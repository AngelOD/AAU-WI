using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Crawler.Helpers;

namespace Crawler.Models
{
    [Serializable]
    public class CrawlerRegistry
    {
        public HashSet<CrawlerLink> Links { get; protected set; }

        public CrawlerRegistry()
        {
            this.Links = new HashSet<CrawlerLink>();
        }

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

            var registry = (CrawlerRegistry)deserializer.Deserialize(file);

            file.Close();

            return registry;
        }
    }
}
