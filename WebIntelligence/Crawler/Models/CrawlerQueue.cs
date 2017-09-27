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
    public class CrawlerQueue
    {
        private Queue<string> _backlog;

        public CrawlerQueue() { this._backlog = new Queue<string>(); }

        public int Length => this._backlog.Count;

        public bool HasLink => this._backlog.Count > 0;

        public void AddLink(string link) { this._backlog.Enqueue(link); }

        public string GetLink() { return this._backlog.Dequeue(); }

        public IEnumerable<string> GetLinkCollection()
        {
            if (!this.HasLink) { throw new InvalidOperationException("No links available!"); }

            var links = new HashSet<string>();
            var newQueue = new Queue<string>();
            var firstLink = this.GetLink();
            var baseLink = new Uri(Utilities.GetUrlBase(firstLink));

            newQueue.Enqueue(firstLink);

            while (this._backlog.Count > 0)
            {
                var link = this._backlog.Dequeue();

                if (baseLink.IsBaseOf(new Uri(link))) { links.Add(link); }
                else { newQueue.Enqueue(link); }
            }

            this._backlog = newQueue;

            return links;
        }

        public static void SaveToFile(string fileName, CrawlerQueue queue)
        {
            var file = File.Create(Path.Combine(Utilities.UserAppDataPath, fileName));
            var serializer = new BinaryFormatter();

            serializer.Serialize(file, queue);

            file.Close();
        }

        public static CrawlerQueue LoadFromFile(string fileName)
        {
            var file = File.OpenRead(Path.Combine(Utilities.UserAppDataPath, fileName));
            var deserializer = new BinaryFormatter();

            var queue = (CrawlerQueue)deserializer.Deserialize(file);

            file.Close();

            return queue;
        }
    }
}
