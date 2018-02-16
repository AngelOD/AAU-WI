using System;
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

            sentimentProbabilities[0] = 0;
            for (var i = 1; i < 6; i++) { sentimentProbabilities[i] = this.GetSentimentProbability(i); }

            foreach (var word in this._wordList)
            {
                for (var i = 1; i < 6; i++)
                {
                    this._emptyScores[i] *= (1 - this.GetProbabilityOfWordGivenSentimentFast(word.Value, i)) * sentimentProbabilities[i];
                }
            }

            Console.WriteLine("Done!");
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
            var indexes = new List<int>();
            var score = 0;
            var count = 0;
            var maxCount = 0;
            var totalCount = 0;
            string line;

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
                    if (indexes != null && (indexes.Count > 0 && score > 0))
                    {
                        this._sentimentCounts[score]++;

                        count++;
                        totalCount += indexes.Count;

                        if (indexes.Count > maxCount) { maxCount = indexes.Count; }

                        score = 0;
                        indexes = null;
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
                var tokens = this.AddNegationAugments(tokenizer.Tokenize(rTest.Groups["review"].Value));
                indexes = new List<int>(tokens.Count);

                foreach (var token in tokens)
                {
                    if (this._wordList.ContainsKey(token))
                    {
                        var index = this._wordList[token];
                        indexes.Add(index);
                        this._sentimentWordCounts[score][index]++;
                    }
                    else
                    {
                        var index = this._wordList.Count;
                        this._wordList[token] = index;
                        indexes.Add(index);

                        for (var i = 0; i < 6; i++)
                        {
                            this._sentimentWordCounts[i][index] = 0;
                        }

                        this._sentimentWordCounts[score][index]++;
                    }
                }
            }

            if (indexes != null && (indexes.Count > 0 && score > 0))
            {
                this._sentimentCounts[score]++;
                count++;
                totalCount += indexes.Count;

                if (indexes.Count > maxCount) { maxCount = indexes.Count; }
            }

            this._entryCount = count;

            Console.WriteLine("Bytes: {0} (Average: {1})", maxCount * sizeof(int), ((decimal)totalCount / count) * sizeof(int));
            Console.WriteLine("Found {0} words and {1} entries.", this._wordList.Count, count);

            return true;
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
            return (decimal)this._sentimentWordCounts[sentiment][wordIndex] / this._sentimentCounts[sentiment];
        }

        public decimal GetEmptyScoreForSentiment(int sentiment)
        {
            if (sentiment < 1 || sentiment > 5) { return -1; }

            return this._emptyScores[sentiment];
        }
    }
}
