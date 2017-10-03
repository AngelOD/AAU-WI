using System;
using System.Collections.Generic;
using System.Linq;
using Crawler.Helpers;

namespace Crawler.Models
{
    [Serializable]
    public class CrawlerLink
    {
        public string Address { get; protected set; }
        public LinkedList<ulong> ShingleHashes { get; protected set; }
        public HashSet<string> Tokens { get; protected set; }
        public Dictionary<string, int> Keywords { get; protected set; }
        public HashSet<string> Links { get; protected set; }

        public CrawlerLink(string address, string contents, IEnumerable<string> links)
        {
            this.Address = address;

            // Store links
            this.Links = new HashSet<string>(links);

            // Tokenize
            var tokens = new List<string>(contents.Split(' '));
            tokens.RemoveAll(token => token.Length <= 1 || StopWords.StopWordsList.Contains(token));

            // Generate shingle hashes
            var jaccard = new Jaccard();
            this.ShingleHashes = new LinkedList<ulong>(jaccard.HashedShinglifyDocument(tokens.ToArray()));

            // Apply stemming
            var stemmer = new PorterStemmer();
            var stemmedTokens = new List<string>(tokens.Select(token => stemmer.StemWord(token)));
            this.Tokens = new HashSet<string>(stemmedTokens);

            // Sort elements
            stemmedTokens.Sort();

            // Get keyword count
            var lastKeyword = "";
            var keywords = new Dictionary<string, int>();

            foreach (var stemmedToken in stemmedTokens)
            {
                if (!stemmedToken.Equals(lastKeyword))
                {
                    lastKeyword = stemmedToken;
                    keywords[stemmedToken] = 1;
                }
                else { keywords[stemmedToken] += 1; }
            }

            this.Keywords = keywords;
        }
    }
}
