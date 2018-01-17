#region Using Directives
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
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

        public Dictionary<int, double> PageRanks
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
            Console.Write("Generating index... ");

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

            Console.WriteLine("Done!");
        }

        protected void BuildPageRank()
        {
            if (this.Count == 0) return;

            Console.WriteLine("Building page rank...");

            const double alpha = 0.1;
            const double invAlpha = 1 - alpha;
            var entryCount = this.Count;
            var teleportProp = 1.0 / (double) entryCount;
            var lookupTable = new Dictionary<int, int>();
            var invLookupTable = new Dictionary<int, int>();
            var formerPageRank = new double[entryCount];
            var pageRank = new double[entryCount];
            var probMatrix = new double[entryCount, entryCount];

            // Randomise starting page
            pageRank[new Random().Next(0, entryCount - 1)] = 1.0;

            // Initialise lookup table
            Console.Write("Initialising lookup tables... ");
            var curIndex = 0;
            foreach (var crawlerLink in this.Links)
            {
                lookupTable[crawlerLink.Key] = curIndex;
                invLookupTable[curIndex] = crawlerLink.Key;
                curIndex++;
            }
            Console.WriteLine("Done!");

            // Initialise probability matrix
            Console.Write("Initiating probability matrix... ");
            foreach (var crawlerLink in this.Links)
            {
                var localLinks = new HashSet<string>(crawlerLink.Value.Links);
                localLinks.RemoveWhere(link => !this.AddressLookup.ContainsKey(link));
                var linkCount = localLinks.Count;
                curIndex = lookupTable[crawlerLink.Key];

                if (linkCount == 0)
                {
                    // Set all columns to be 1 / entryCount
                    for (var i = 0; i < entryCount; i++) { probMatrix[curIndex, i] = teleportProp; }
                }
                else
                {
                    foreach (var link in localLinks)
                    {
                        var index = lookupTable[this.AddressLookup[link]];
                        if (curIndex == index) continue;
                        probMatrix[curIndex, index] = (1.0 / linkCount) * invAlpha;
                    }

                    // Loop through all columns and add (1 / entryCount) * alpha
                    for (var i = 0; i < entryCount; i++) { probMatrix[curIndex, i] += teleportProp * alpha; }
                }
            }
            Console.WriteLine("Done!");

            // Calculate page rank via power method
            Console.Write("Calculating approximated page rank... ");
            var totalDiff = 10.0;
            var iter = 0;
            var file = File.Create(Path.Combine(Utilities.UserAppDataPath, "pagerank.txt"));
            var sw = new StreamWriter(file);
            while (totalDiff >= 0.05)
            {
                totalDiff = 0.0;
                iter++;

                sw.WriteLine("Iteration: {0}", iter);
                Console.Write("{0}.", iter);
                for (var i = 0; i < entryCount; i++)
                {
                    formerPageRank[i] = pageRank[i];
                    pageRank[i] = 0;

                    for (var j = 0; j < entryCount; j++)
                    {
                        pageRank[i] += (j == i ? formerPageRank[j] : pageRank[j]) * probMatrix[j, i];
                    }

                    totalDiff += Math.Abs(formerPageRank[i] - pageRank[i]);
                }
                sw.WriteLine();
                sw.WriteLine("Difference: {0}", totalDiff);
                sw.WriteLine();
                Console.Write(". ");
            }
            sw.Close();
            Console.WriteLine("Done!");

            // Set page ranks
            this._pageRanks = new Dictionary<int, double>();
            for (var i = 0; i < entryCount; i++) { this._pageRanks[invLookupTable[i]] = pageRank[i]; }
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
            this.AddressLookup[link.Address] = nextId;

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
            var bw = Utilities.GetWriterForFile(fileName);

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
            var br = Utilities.GetReaderForFile(fileName);
            var registry = new CrawlerRegistry();

            if (br == null) { return registry; }

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
                registry.AddressLookup[link.Address] = key;

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
