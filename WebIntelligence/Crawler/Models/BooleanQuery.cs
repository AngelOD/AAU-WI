#region Using Directives
using System;
using System.Collections.Generic;
using System.Linq;
using Crawler.Helpers;
#endregion

namespace Crawler.Models
{
    public class BooleanQuery
    {
        #region QueryType enum
        public enum QueryType
        {
            Word = 1,
            Not = 2,
            And = 3,
            Or = 4
        }
        #endregion

        public QueryPart ParseQuery(string input)
        {
            var tokens = new List<string>(input.ToLower().Split(' '));
            tokens.RemoveAll(token => token.Length <= 1 || StopWords.BooleanStopWordsList.Contains(token));

            var stemmer = new PorterStemmer();
            var stemmedTokens = new List<string>(tokens.Select(token => StopWords.BooleanWords.Contains(token.ToLower()) ? token.ToUpper() : stemmer.StemWord(token.ToLower())));

            return this.ParseQuery(stemmedTokens);
        }

        public QueryPart ParseQuery(List<string> parts)
        {
            var newParts = parts.Select(part => new QueryPart
                                                {
                                                    QueryType = QueryType.Word,
                                                    Word = part
                                                })
                                .ToList();

            return this.ParseQuery(newParts);
        }

        private QueryPart ParseQuery(List<QueryPart> parts)
        {
            while (true)
            {
                // Check for NOT clauses
                bool runAgain;

                do
                {
                    runAgain = false;

                    var notPos = parts.FindIndex(p => p.QueryType == QueryType.Word &&
                                                      p.Word.Equals("NOT",
                                                                    StringComparison.InvariantCultureIgnoreCase));

                    if (notPos <= -1) continue;

                    parts[notPos + 1].QueryType = QueryType.Not;
                    parts.RemoveAt(notPos);

                    runAgain = true;
                } while (runAgain);

                // Check for AND clauses
                var andPos = parts.FindIndex(p => p.QueryType == QueryType.Word &&
                                                  p.Word.Equals("AND", StringComparison.InvariantCultureIgnoreCase));

                if (andPos > -1)
                {
                    var andPart = new QueryPart
                                  {
                                      QueryType = QueryType.And,
                                      LeftPart = this.ParseQuery(parts.GetRange(0, andPos)),
                                      RightPart =
                                          this.ParseQuery(parts.GetRange(andPos + 1, parts.Count - (andPos + 1)))
                                  };

                    return andPart;
                }

                // Check for OR clauses
                var orPos = parts.FindIndex(p => p.QueryType == QueryType.Word &&
                                                 p.Word.Equals("OR", StringComparison.InvariantCultureIgnoreCase));

                if (orPos > -1)
                {
                    var orPart = new QueryPart
                                 {
                                     QueryType = QueryType.Or,
                                     LeftPart = this.ParseQuery(parts.GetRange(0, orPos)),
                                     RightPart = this.ParseQuery(parts.GetRange(orPos + 1, parts.Count - (orPos + 1)))
                                 };

                    return orPart;
                }

                // Check if there's only one entry
                if (parts.Count <= 1) { return parts[0]; }

                // Run through the remaining parts
                var newParts = new List<QueryPart>();
                QueryPart lastPart = null;

                foreach (var part in parts)
                {
                    if (lastPart != null)
                    {
                        newParts.Add(new QueryPart
                                     {
                                         QueryType = QueryType.Word,
                                         Word = "AND"
                                     });
                    }

                    lastPart = part;
                    newParts.Add(part);
                }

                parts = newParts;
            }
        }

        #region Nested type: QueryPart
        public class QueryPart
        {
            public QueryPart LeftPart { get; set; }
            public QueryType QueryType { get; set; }
            public QueryPart RightPart { get; set; }
            public string Word { get; set; }

            public void Output(int level)
            {
                switch (this.QueryType)
                {
                    case QueryType.Word:
                        this.Output(level, $"[WORD] {this.Word}");
                        break;

                    case QueryType.And:
                    case QueryType.Or:
                        this.Output(level, $"[{(this.QueryType == QueryType.And ? "AND" : "OR")}]");
                        this.Output(level, "Left side:");
                        this.LeftPart.Output(level + 1);
                        this.Output(level, "Right side:");
                        this.RightPart.Output(level + 1);
                        break;

                    case QueryType.Not:
                        this.Output(level, "[NOT]");
                        this.Output(level + 1, $"[WORD] {this.Word}");
                        break;
                }
            }

            public void Output()
            {
                this.Output(0);
            }

            public HashSet<CrawlerRegistry.IndexEntry> Execute(CrawlerRegistry registry)
            {
                var removals = new HashSet<CrawlerRegistry.IndexEntry>();
                var entries = this.Execute(registry, removals);

                Console.WriteLine("[TopLevel] {0} entries, {1} removals.", entries.Count, removals.Count);

                // TODO Is this even necessary?
                entries.ExceptWith(removals);

                return entries;
            }

            protected HashSet<CrawlerRegistry.IndexEntry> Execute(CrawlerRegistry registry, HashSet<CrawlerRegistry.IndexEntry> removals)
            {
                var entries = new HashSet<CrawlerRegistry.IndexEntry>();

                switch (this.QueryType)
                {
                    case QueryType.Word:
                        entries = registry.GetIndexEntries(this.Word);
                        break;

                    case QueryType.And:
                        entries = this.LeftPart.Execute(registry, removals);

                        if (this.RightPart.QueryType == QueryType.Not)
                        {
                            var tempRemovals = new HashSet<CrawlerRegistry.IndexEntry>();

                            this.RightPart.Execute(registry, tempRemovals);
                            entries.ExceptWith(tempRemovals);
                        }
                        else { entries.IntersectWith(this.RightPart.Execute(registry, removals)); }
                        break;

                    case QueryType.Or:
                        entries = this.LeftPart.Execute(registry, removals);
                        entries.UnionWith(this.RightPart.Execute(registry, removals));
                        break;

                    case QueryType.Not:
                        removals.UnionWith(registry.GetIndexEntries(this.Word));
                        break;
                }

                Console.WriteLine("[SubLevel] {0} entries, {1} removals.", entries.Count, removals.Count);

                return entries;
            }

            private void Output(int level, string message)
            {
                for (var i = 0; i < level; i++)
                {
                    Console.Write("-");
                }

                Console.WriteLine(message);
            }
        }
        #endregion
    }
}
