using System;
using System.Collections.Generic;

namespace Crawler.Models
{
    [Serializable]
    public class CrawlerLink
    {
        public string Address { get; protected set; }
        public LinkedList<ulong> ShingleHashes { get; protected set; }

        public CrawlerLink(string address, string contents)
        {
            this.Address = address;

            var jaccard = new Jaccard();
            this.ShingleHashes = new LinkedList<ulong>(jaccard.HashedShinglifyDocument(contents));
        }
    }
}
