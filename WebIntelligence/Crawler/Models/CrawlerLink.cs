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

        public CrawlerLink(string address, string contents)
        {
            this.Address = address;

            // Tokenize
            var tokens = new List<string>(contents.Split(' '));
            tokens.RemoveAll(token => token.Length <= 1 || StopWords.StopWordsList.Contains(token));

            // Generate shingle hashes
            var jaccard = new Jaccard();
            this.ShingleHashes = new LinkedList<ulong>(jaccard.HashedShinglifyDocument(tokens.ToArray()));

            // Apply stemming
            var stemmer = new PorterStemmer();
            this.Tokens = new HashSet<string>(tokens.Select(token => stemmer.StemWord(token)));
        }
    }
}
