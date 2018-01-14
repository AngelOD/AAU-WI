#region Using Directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Crawler.Helpers;
#endregion

namespace Crawler.Models
{
    public class RegularQuery
    {
        public LinkedList<IndexEntry> Execute(string query, CrawlerRegistry registry, int maxResults = 25, bool usePageRank = false)
        {
            var tokens = new List<string>(query.ToLower().Split(' '));
            tokens.RemoveAll(token => token.Length <= 1 || StopWords.StopWordsList.Contains(token));

            var stemmer = new PorterStemmer();
            var stemmedTokens = new HashSet<string>(tokens.Select(token => stemmer.StemWord(token.ToLower())));

            return this.Execute(stemmedTokens.ToList(), registry, maxResults, usePageRank);
        }

        protected LinkedList<IndexEntry> Execute(List<string> tokens, CrawlerRegistry registry, int maxResults, bool usePageRank)
        {
            var queryWeights = this.CalculateQueryWeights(tokens, registry);
            var results = new LinkedList<IndexEntry>();
            var scores = new Dictionary<int, double>();
            var documentWeights = new Dictionary<int, Dictionary<string, double>>();
            var totalWeights = new Dictionary<int, double>();

            foreach (var token in tokens)
            {
                var entries = registry.GetIndexEntries(token);

                foreach (var entry in entries)
                {
                    var weight = 1 + Math.Log10(entry.Frequency);

                    if (!documentWeights.ContainsKey(entry.LinkId))
                    {
                        documentWeights[entry.LinkId] = new Dictionary<string, double>();
                        totalWeights[entry.LinkId] = 0;
                    }

                    documentWeights[entry.LinkId][token] = weight;
                    totalWeights[entry.LinkId] += weight * weight;
                }
            }

            foreach (var doc in documentWeights)
            {
                var totalWeight = Math.Sqrt(totalWeights[doc.Key]);
                scores[doc.Key] = doc.Value.Sum(token => (token.Value / totalWeight) * queryWeights[token.Key]);
            }

            var sortedScores =
                scores.OrderByDescending(score => score.Value)
                .Take(maxResults);

            if (usePageRank) { sortedScores = sortedScores.OrderByDescending(score => registry.PageRanks[score.Key]); }

            foreach (var score in sortedScores)
            {
                results.AddLast(new IndexEntry(score.Key, score.Value));
            }

            return results;
        }

        protected Dictionary<string, double> CalculateQueryWeights(IEnumerable<string> tokens, CrawlerRegistry registry)
        {
            double totalWeightSquared = 0;
            var queryWeights = new Dictionary<string, double>();
            var localQueryWeights = new Dictionary<string, double>();

            queryWeights.Clear();

            foreach (var token in tokens)
            {
                if (!registry.Index.ContainsKey(token)) { queryWeights[token] = -1; }
                else
                {
                    var df = registry.Index[token].Count;
                    var weight = Math.Log10(registry.Index.Count / (double)df);
                    totalWeightSquared += weight * weight;
                    localQueryWeights[token] = weight;
                }
            }

            totalWeightSquared = Math.Sqrt(totalWeightSquared);

            foreach (var weight in localQueryWeights) { queryWeights[weight.Key] = weight.Value / totalWeightSquared; }

            return queryWeights;
        }

        #region Nested type: IndexEntry
        public struct IndexEntry
        {
            public int LinkId;
            public double Score;

            public IndexEntry(int linkId, double score)
            {
                this.LinkId = linkId;
                this.Score = score;
            }
        }
        #endregion
    }
}
