using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;

namespace Sentiment.Models
{
    public class Classifier
    {
        private readonly Dictionary<string, Regex> _regexes;

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
        }

        public List<string> AddNegationAugments(List<string> words)
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

        public void TestDataTable()
        {
            var table = new DataTable();
        }

        public bool LoadTrainingData(string filePath)
        {
            if (!File.Exists(filePath)) { return false; }

            var file = new StreamReader(filePath);
            var result = this.ProcessTrainingFile(file);

            file.Close();

            return result;
        }

        protected bool ProcessTrainingFile(StreamReader sr)
        {
            var tokenizer = new HappyFunTokenizer(true);
            var wordList = new Dictionary<string, int>();
            var sentimentCounts = new int[] { 0, 0, 0, 0, 0, 0 };
            var sentimentWordCounts = new Dictionary<int, int>[6];
            var indexes = new List<int>();
            var entries = new List<TrainingEntry>();
            var score = 0;
            var count = 0;
            var maxCount = 0;
            var totalCount = 0;
            string line;

            /*
             * Read training data, tokenize and store feature vectors in a sparse index to
             * limit the memory usage.
             */

            for (var i = 0; i < 6; i++) { sentimentWordCounts[i] = new Dictionary<int, int>(); }

            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                {
                    if (indexes != null && (indexes.Count > 0 && score > 0))
                    {
                        sentimentCounts[score]++;
                        //entries.Add(new TrainingEntry { Score = score, Indexes = indexes });

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
                    if (wordList.ContainsKey(token))
                    {
                        var index = wordList[token];
                        indexes.Add(index);
                        sentimentWordCounts[score][index]++;
                    }
                    else
                    {
                        var index = wordList.Count;
                        wordList[token] = index;
                        indexes.Add(index);

                        for (var i = 0; i < 6; i++)
                        {
                            sentimentWordCounts[i][index] = 0;
                        }

                        sentimentWordCounts[score][index]++;
                    }
                }
            }

            Console.WriteLine("Bytes: {0} (Average: {1})", maxCount * sizeof(int), ((decimal)totalCount / count) * sizeof(int));
            Console.WriteLine("Found {0} words and {1} entries.", wordList.Count, count);

            return true;
        }

        protected struct TrainingEntry
        {
            public int Score;
            public List<int> Indexes;
        }
    }
}
