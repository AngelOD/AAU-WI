#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Crawler.Helpers;
#endregion

namespace Crawler.Models
{
    [Serializable]
    public class CrawlerRegistry
    {
        protected const string FileIdent = "BSCCR";
        protected const int FileVersion = 2;
        private SortedDictionary<string, List<IndexEntry>> _index;
        private Dictionary<int, double> _pageRanks;
        private bool _isDirty;
        private bool _hasPageRank = false;

        public CrawlerRegistry()
        {
            this.AddressLookup = new Dictionary<string, int>();
            this.Links = new Dictionary<int, CrawlerLink>();
            this.LastId = 0;
        }

        public int LastId { get; protected set; }
        protected Dictionary<int, CrawlerLink> Links { get; set; }
        protected Dictionary<string, int> AddressLookup { get; set; }

        protected Dictionary<int, double> PageRanks
        {
            get
            {
                if (!this._hasPageRank) { this.BuildPageRank(); }

                return this._pageRanks;
            }
        }

        public SortedDictionary<string, List<IndexEntry>> Index
        {
            get
            {
                if (this._isDirty) { this.BuildIndex(); }

                return this._index;
            }
        }

        public CrawlerLink this[int index] => this.Links[index];

        public int Count => this.Links.Count;

        protected void BuildIndex()
        {
            var index = new SortedDictionary<string, List<IndexEntry>>(StringComparer.InvariantCulture);

            foreach (var link in this.Links)
            {
                var keywords = link.Value.Keywords;

                foreach (var keyword in keywords)
                {
                    var entry = new IndexEntry
                                {
                                    Frequency = keyword.Value,
                                    LinkId = link.Key
                                };

                    if (!index.ContainsKey(keyword.Key)) { index[keyword.Key] = new List<IndexEntry>(); }

                    index[keyword.Key].Add(entry);
                }
            }

            this._index = index;
            this._isDirty = false;
        }

        protected void BuildPageRank()
        {
            if (this.Count == 0) return;

            const double alpha = 0.1;
            var invAlpha = 1 - alpha;
            var entryCount = this.Count;
            var teleportProp = 1.0 / (double) entryCount;
            var lookupTable = new int[entryCount];
            var formerPageRank = new double[entryCount];
            var pageRank = new double[entryCount];
            var probMatrix = new double[entryCount, entryCount];

            // TODO Initialise probability matrix
            // TODO Calculate page rank via power method

            // TODO Set page ranks
            this._hasPageRank = true;
        }

        public HashSet<IndexEntry> GetIndexEntries(string key)
        {
            if (this.Index.ContainsKey(key.ToLowerInvariant()))
            {
                return new HashSet<IndexEntry>(this.Index[key.ToLowerInvariant()]);
            }

            return new HashSet<IndexEntry>();
        }

        /**
         * 
         */
        public void CleanRegistry()
        {
            foreach (var crawlerLink in this.Links)
            {
                crawlerLink.Value.CleanTokens();
            }

            this._isDirty = true;
            this._hasPageRank = false;
        }

        public void AddLink(CrawlerLink link)
        {
            if (this.AddressLookup.ContainsKey(link.Address)) return;

            var nextId = this.LastId + 1;
            link.UniqueId = nextId;
            this.LastId = nextId;

            this.Links[nextId] = link;
            this._isDirty = true;
            this._hasPageRank = false;
        }

        public CrawlerLink GetLink(int linkId)
        {
            if (!this.Links.ContainsKey(linkId))
            {
                throw new KeyNotFoundException("No link with the given ID exists in the registry.");
            }

            return this.Links[linkId];
        }

        public Dictionary<int, CrawlerLink>.Enumerator GetEnumerator() { return this.Links.GetEnumerator(); }

        public static void SaveToFile(string fileName, CrawlerRegistry registry)
        {
            var file = File.Create(Path.Combine(Utilities.UserAppDataPath, fileName));
            var bw = new BinaryWriter(file);

            // TODO Make this betterer!
            // Header
            bw.Write(FileIdent);
            bw.Write(FileVersion);

            // Entries
            bw.Write(registry.Links.Count);
            foreach (var registryLink in registry.Links)
            {
                bw.Write(registryLink.Key);
                registryLink.Value.SaveTo(bw);
            }

            bw.Close();
        }

        public static CrawlerRegistry LoadFromFile(string fileName)
        {
            var file = File.OpenRead(Path.Combine(Utilities.UserAppDataPath, fileName));
            var br = new BinaryReader(file);
            var registry = new CrawlerRegistry();

            // TODO Make this betterer too!
            // Header
            var ident = br.ReadString();
            var ver = br.ReadInt32();

            if (!ident.Equals(FileIdent) || ver != FileVersion)
            {
                throw new FileLoadException("Incorrect file format!");
            }

            var maxId = 0;
            var entryCount = br.ReadInt32();
            for (var i = 0; i < entryCount; i++)
            {
                var key = br.ReadInt32();
                var link = new CrawlerLink(br);

                registry.Links.Add(key, link);

                if (key > maxId) { maxId = key; }
            }

            registry.LastId = maxId;
            registry._isDirty = true;
            registry._hasPageRank = false;

            br.Close();

            return registry;
        }

        #region Nested type: IndexEntry
        public struct IndexEntry
        {
            public int LinkId;
            public int Frequency;
        }
        #endregion
    }
}
