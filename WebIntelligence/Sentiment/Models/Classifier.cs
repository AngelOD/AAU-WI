using System;
using System.Collections;
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
            var entries = new List<ClassifierEntry>();
            var score = 0;
            BitArray bits = null;
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                {
                    if (bits != null && (bits.Count > 0 && score > 0))
                    {
                        /*
                        entries.Add(new ClassifierEntry
                                    {
                                        score = score,
                                        featureVector = bits
                                    });
                                    */

                        score = 0;
                        bits = null;
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
                bits = new BitArray(wordList.Count + tokens.Count);
                bits.SetAll(false);

                foreach (var token in tokens)
                {
                    if (wordList.ContainsKey(token))
                    {
                        bits.Set(wordList[token], true);
                    }
                    else
                    {
                        var index = wordList.Count;
                        wordList[token] = index;
                        bits.Set(index, true);
                    }
                }
            }

            Console.WriteLine("Found {0} entries.", wordList.Count);

            return true;
        }

        protected class ClassifierEntry
        {
            public BitArray featureVector;
            public int score;
        }
    }
}
