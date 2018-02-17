﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Sentiment.Helpers;

namespace Sentiment.Models
{
    public class Classifier
    {
        private readonly Dictionary<string, Regex> _regexes;
        private Dictionary<string, int> _wordList;
        private Dictionary<int, int> _sentimentCounts;
        private Dictionary<int, Dictionary<int, int>> _sentimentWordCounts;
        private int _entryCount = 0;
        private decimal[] _emptyScores = { 1, 1, 1, 1, 1, 1 };

        public Classifier()
        {
            const string negations = "(?:^(?:never|no|nothing|nowhere|noone|none|not|havent|hasnt|" +
                                     "hadnt|cant|couldnt|shouldnt|wont|wouldnt|dont|doesnt|didnt|" +
                                     "isnt|arent|aint)$)|n't";
            const string punctuation = "^[.:;!?]+$";

            this._regexes = new Dictionary<string, Regex>()
                            {
                                ["negations"] =
                                new Regex(negations,
                                          RegexOptions.Compiled |
                                          RegexOptions.CultureInvariant |
                                          RegexOptions.IgnoreCase),
                                ["punctuation"] =
                                new Regex(punctuation,
                                          RegexOptions.Compiled |
                                          RegexOptions.CultureInvariant |
                                          RegexOptions.IgnoreCase),
                                ["score"] =
                                new Regex("^review/score: (?<score>\\d)\\.\\d$",
                                          RegexOptions.Compiled |
                                          RegexOptions.CultureInvariant |
                                          RegexOptions.IgnoreCase),
                                ["review"] =
                                new Regex("^review/text: (?<review>.*)$",
                                          RegexOptions.Compiled |
                                          RegexOptions.CultureInvariant |
                                          RegexOptions.IgnoreCase)
                            };

            Console.WriteLine("Reading training information...");
            if (!this.LoadTrainingData(@"D:\_Temp\__WI_TestData\SentimentTrainingData.txt"))
            {
                Console.WriteLine("No data loaded.");
                return;
            }

            Console.WriteLine("Data loaded!");
            Console.WriteLine("Calculating score for the empty review...");

            var sentimentProbabilities = new decimal[6];
            var zeroes = 0;
            var ones = 0;

            sentimentProbabilities[0] = 0;
            for (var i = 1; i < 6; i++)
            {
                sentimentProbabilities[i] = this.GetSentimentProbability(i);
                Console.WriteLine(sentimentProbabilities[i]);
                this._emptyScores[i] = 1;
            }

            foreach (var word in this._wordList)
            {
                for (var i = 1; i < 6; i++)
                {
                    var wordProb = this.GetProbabilityOfWordGivenSentimentFast(word.Value, i);

                    if (wordProb == 0) zeroes++;
                    else if (wordProb >= 1) { ones++; }

                    this._emptyScores[i] *= (1M - wordProb);
                }
            }

            Console.WriteLine("Done!");
            Console.WriteLine("{0} Zeroes (results in 1) and {1} ones (results in 0).", zeroes, ones);
        }

        protected List<string> AddNegationAugments(List<string> words)
        {
            var newWords = new List<string>();
            var enumerator = words.GetEnumerator();
            var negated = false;

            while (enumerator.MoveNext())
            {
                if (enumerator.Current == null) { continue; }

                var append = "";

                if (negated && this._regexes["punctuation"].IsMatch(enumerator.Current)) { negated = false; }
                else if (negated) { append = "_NEG"; }
                else if (this._regexes["negations"].IsMatch(enumerator.Current)) { negated = true; }

                newWords.Add(enumerator.Current + append);
            }

            enumerator.Dispose();

            return newWords;
        }

        protected bool LoadTrainingData(string filePath)
        {
            if (this.LoadCacheTrainingData())
            {
                Console.WriteLine("Read {0} words and {1} entries.", this._wordList.Count, this._entryCount);
                foreach (var entry in this._sentimentWordCounts)
                {

                }
                return true;
            }

            if (!File.Exists(filePath)) { return false; }

            var file = new StreamReader(filePath);
            var result = this.ProcessTrainingFile(file);

            file.Close();

            if (result) { this.CacheTrainingData(); }

            return result;
        }

        protected void CacheTrainingData()
        {
            var bw = Utilities.GetWriterForFile("training.dat");

            // Word list
            bw.Write(this._wordList.Count);
            foreach (var word in this._wordList)
            {
                var plainTextBytes = Encoding.UTF8.GetBytes(word.Key);
                bw.Write(Convert.ToBase64String(plainTextBytes));
                bw.Write(word.Value);
            }

            // Sentiment counts
            for (var i = 0; i < 6; i++) { bw.Write(this._sentimentCounts[i]); }

            // Sentiment word counts
            for (var i = 0; i < 6; i++)
            {
                bw.Write(this._sentimentWordCounts[i].Count);
                foreach (var entry in this._sentimentWordCounts[i])
                {
                    bw.Write(entry.Key);
                    bw.Write(entry.Value);
                }
            }

            bw.Close();


            // Output debug file if needed!
            var debugFile = Path.Combine(Utilities.UserAppDataPath, "debug.txt");
            if (File.Exists(debugFile)) return;

            var sw = new StreamWriter(debugFile)
                     {
                         AutoFlush = true
                     };

            // Word list
            sw.WriteLine("Word list");
            foreach (var word in this._wordList)
            {
                try
                {
                    var toWrite = $"{word.Value:D}\t{word.Key}";
                    sw.WriteLine(toWrite);
                }
                catch (EncoderFallbackException)
                {
                }
            }
            sw.WriteLine();

            // Sentiment counts
            sw.WriteLine("Sentiment counts");
            for (var i = 0; i < 6; i++)
            {
                sw.WriteLine("{0}\t{1}", i, this._sentimentCounts[i]);
            }
            sw.WriteLine();

            // Sentiment word counts
            sw.WriteLine("Sentiment word counts");
            for (var i = 0; i < 6; i++)
            {
                sw.WriteLine(".:: {0} ({1})", i, this._sentimentWordCounts[i].Count);

                foreach (var entry in this._sentimentWordCounts[i])
                {
                    sw.WriteLine("{0}\t{1}", entry.Key, entry.Value);
                }
            }
            sw.WriteLine();

            sw.Close();
        }

        protected bool LoadCacheTrainingData()
        {
            var br = Utilities.GetReaderForFile("training.dat");
            if (br == null) { return false; }

            this._entryCount = 0;

            // Word list
            this._wordList = new Dictionary<string, int>();
            var wordCount = br.ReadInt32();

            for (var i = 0; i < wordCount; i++)
            {
                var wordBytes = Convert.FromBase64String(br.ReadString());
                var word = Encoding.UTF8.GetString(wordBytes);
                var index = br.ReadInt32();

                this._wordList[word] = index;
            }

            // Sentiment counts
            this._sentimentCounts = new Dictionary<int, int>();
            for (var i = 0; i < 6; i++)
            {
                this._sentimentCounts[i] = br.ReadInt32();
                this._entryCount += this._sentimentCounts[i];
            }

            // Sentiment word counts
            this._sentimentWordCounts = new Dictionary<int, Dictionary<int, int>>();
            for (var i = 0; i < 6; i++)
            {
                this._sentimentWordCounts[i] = new Dictionary<int, int>();
                wordCount = br.ReadInt32();

                for (var j = 0; j < wordCount; j++)
                {
                    var index = br.ReadInt32();
                    var count = br.ReadInt32();

                    this._sentimentWordCounts[i][index] = count;
                }
            }

            br.Close();

            return true;
        }

        protected bool ProcessTrainingFile(StreamReader sr)
        {
            var tokenizer = new HappyFunTokenizer(true);
            var score = 0;
            var count = 0;
            var maxCount = 0;
            var totalCount = 0;
            string line;
            string review = null;

            /*
             * Read training data, tokenize and store feature vectors in a sparse index to
             * limit the memory usage.
             */
            this._wordList = new Dictionary<string, int>();
            this._sentimentCounts = new Dictionary<int, int>();
            this._sentimentWordCounts = new Dictionary<int, Dictionary<int, int>>();

            for (var i = 0; i < 6; i++)
            {
                this._sentimentCounts[i] = 0;
                this._sentimentWordCounts[i] = new Dictionary<int, int>();
            }

            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                {
                    if (!string.IsNullOrEmpty(review) && score > 0)
                    {
                        var tokenCount = this.AddTrainingEntry(tokenizer, score, review);
                        this._sentimentCounts[score]++;

                        count++;
                        totalCount += tokenCount;

                        if (tokenCount > maxCount) { maxCount = tokenCount; }

                        score = 0;
                        review = null;
                    }

                    continue;
                }

                var sTest = this._regexes["score"].Match(line);
                if (sTest.Success)
                {
                    score = int.Parse(sTest.Groups["score"].Value);
                    continue;
                }

                var rTest = this._regexes["review"].Match(line);
                if (!rTest.Success) continue;
                review = rTest.Groups["review"].Value;
            }

            if (!string.IsNullOrEmpty(review) && score > 0)
            {
                var tokenCount = this.AddTrainingEntry(tokenizer, score, review);
                this._sentimentCounts[score]++;

                count++;
                totalCount += tokenCount;

                if (tokenCount > maxCount) { maxCount = tokenCount; }
            }

            this._entryCount = count;

            Console.WriteLine("Bytes: {0} (Average: {1:F2})", maxCount * sizeof(int), ((decimal)totalCount / count) * sizeof(int));
            Console.WriteLine("Found {0} words and {1} entries.", this._wordList.Count, count);

            return true;
        }

        protected int AddTrainingEntry(HappyFunTokenizer tokenizer, int score, string review)
        {
            var addedEntries = new Dictionary<int, bool>();
            var tokens = this.AddNegationAugments(tokenizer.Tokenize(review));
            var count = 0;

            foreach (var token in tokens)
            {
                if (!Utilities.CheckUtf8(token)) { continue; }

                if (this._wordList.ContainsKey(token))
                {
                    var index = this._wordList[token];

                    if (addedEntries.ContainsKey(index)) { continue; }

                    count++;
                    this._sentimentWordCounts[score][index]++;
                    addedEntries[index] = true;
                }
                else
                {
                    var index = this._wordList.Count;
                    this._wordList[token] = index;
                    count++;

                    for (var i = 0; i < 6; i++)
                    {
                        this._sentimentWordCounts[i][index] = 0;
                    }

                    this._sentimentWordCounts[score][index]++;
                    addedEntries[index] = true;
                }
            }

            return count;
        }

        public decimal GetSentimentProbability(int sentiment)
        {
            if (sentiment < 1 || sentiment > 5 || this._entryCount == 0) { return -1; }

            return (decimal) this._sentimentCounts[sentiment] / this._entryCount;
        }

        public decimal GetProbabilityOfWordGivenSentiment(int wordIndex, int sentiment)
        {
            if (!this._wordList.ContainsValue(wordIndex) || this._sentimentCounts[sentiment] == 0) { return -1; }

            return (decimal)this._sentimentWordCounts[sentiment][wordIndex] / this._sentimentCounts[sentiment];
        }

        protected decimal GetProbabilityOfWordGivenSentimentFast(int wordIndex, int sentiment)
        {
            var result = (decimal) this._sentimentWordCounts[sentiment][wordIndex] / this._sentimentCounts[sentiment];
            if (result > 1)
            {
                Console.WriteLine("Huh? c = {0}, xi = {1}, N(xi,c) = {2}, N(c) = {3}, p(xi|c) = {4}",
                                  sentiment,
                                  wordIndex,
                                  this._sentimentWordCounts[sentiment][wordIndex],
                                  this._sentimentCounts[sentiment],
                                  result);
            }
            return result;
        }

        public decimal GetEmptyScoreForSentiment(int sentiment)
        {
            if (sentiment < 1 || sentiment > 5) { return -1; }

            return this._emptyScores[sentiment];
        }
    }
}
