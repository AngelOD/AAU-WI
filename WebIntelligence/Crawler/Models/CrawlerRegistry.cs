#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Crawler.Helpers;
#endregion

namespace Crawler.Models
{
    [Serializable]
    public class CrawlerRegistry
    {
        private SortedDictionary<string, List<IndexEntry>> _index;
        private bool _isDirty;

        public CrawlerRegistry()
        {
            this.Links = new Dictionary<int, CrawlerLink>();
            this.LastId = 0;
        }

        protected int LastId { get; set; }
        protected Dictionary<int, CrawlerLink> Links { get; set; }

        public SortedDictionary<string, List<IndexEntry>> Index
        {
            get
            {
                if (this._isDirty) { this.BuildIndex(); }

                return this._index;
            }
        }

        public CrawlerLink this[int index] => this.Links[index];

        protected void BuildIndex()
        {
            var index = new SortedDictionary<string, List<IndexEntry>>();

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

        public void AddLink(CrawlerLink link)
        {
            var nextId = this.LastId + 1;
            link.UniqueId = nextId;
            this.LastId = nextId;

            this.Links[nextId] = link;
            this._isDirty = true;
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
            var serializer = new BinaryFormatter();

            serializer.Serialize(file, registry);

            file.Close();
        }

        public static CrawlerRegistry LoadFromFile(string fileName)
        {
            var file = File.OpenRead(Path.Combine(Utilities.UserAppDataPath, fileName));
            var deserializer = new BinaryFormatter();

            var registry = (CrawlerRegistry) deserializer.Deserialize(file);

            file.Close();

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
