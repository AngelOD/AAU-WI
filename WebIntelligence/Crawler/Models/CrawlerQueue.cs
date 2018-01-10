#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Crawler.Helpers;
#endregion

namespace Crawler.Models
{
    [Serializable]
    public class CrawlerQueue
    {
        protected const string FileIdent = "BSCCQ";
        protected const int FileVersion = 1;

        private Queue<string> _backlog;

        public CrawlerQueue() { this._backlog = new Queue<string>(); }

        public int Length => this._backlog.Count;

        public bool HasLink => this._backlog.Count > 0;

        public void AddLink(string link) { this._backlog.Enqueue(link); }

        public string GetLink() { return this._backlog.Dequeue(); }

        public IEnumerable<string> GetLinkCollection(string lastLink)
        {
            if (!this.HasLink) { throw new InvalidOperationException("No links available!"); }

            var loopCount = 0;
            var maxLoopCount = this.Length;
            var links = new Queue<string>();
            var newQueue = new Queue<string>();
            var firstLink = this.GetLink();
            var baseLink = new Uri(Utilities.GetUrlBase(firstLink));
            var lastBaseLink = new Uri(Utilities.GetUrlBase(lastLink));

            while (baseLink.IsBaseOf(lastBaseLink) && loopCount < maxLoopCount)
            {
                this.AddLink(firstLink);
                firstLink = this.GetLink();
                baseLink = new Uri(Utilities.GetUrlBase(firstLink));
                loopCount++;
            }

            links.Enqueue(firstLink);

            while (this._backlog.Count > 0)
            {
                var link = this._backlog.Dequeue();

                if (baseLink.IsBaseOf(new Uri(link)) && links.Count < 100) { links.Enqueue(link); }
                else { newQueue.Enqueue(link); }
            }

            this._backlog = newQueue;

            return links;
        }

        public Queue<string> GetOverflow(int maxCount)
        {
            var links = new Queue<string>();
            var newQueue = new Queue<string>();
            var curCount = 0;

            while (this.HasLink)
            {
                var link = this.GetLink();

                if (curCount < maxCount) { newQueue.Enqueue(link); }
                else { links.Enqueue(link); }

                curCount++;
            }

            this._backlog = newQueue;

            return links;
        }

        public void ReplaceQueue(IEnumerable<string> newQueue) { this._backlog = new Queue<string>(newQueue); }

        public static void SaveToFile(string fileName, CrawlerQueue queue)
        {
            var file = File.Create(Path.Combine(Utilities.UserAppDataPath, fileName));
            var bw = new BinaryWriter(file);

            // TODO Again make this better
            // Header
            bw.Write(FileIdent);
            bw.Write(FileVersion);

            // Entries
            var entries = queue._backlog.ToArray();
            bw.Write(entries.Length);
            foreach (var entry in entries)
            {
                bw.Write(entry);
            }

            bw.Close();
        }

        public static CrawlerQueue LoadFromFile(string fileName)
        {
            var file = File.OpenRead(Path.Combine(Utilities.UserAppDataPath, fileName));
            var br = new BinaryReader(file);
            var queue = new CrawlerQueue();

            // TODO Make LoadFromFile great again!
            // Header
            var ident = br.ReadString();
            var ver = br.ReadInt32();

            if (!ident.Equals(FileIdent) || ver != FileVersion)
            {
                throw new FileLoadException("Incorrect file format!");
            }

            // Entries
            var entryCount = br.ReadInt32();
            for (var i = 0; i < entryCount; i++)
            {
                queue.AddLink(br.ReadString());
            }

            br.Close();

            return queue;
        }
    }
}
