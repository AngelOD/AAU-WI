using System;
using System.Collections.Generic;

namespace Crawler.Models
{
    [Serializable]
    public class CrawlerQueue
    {
        private readonly Queue<string> _backlog;

        public CrawlerQueue()
        {
            this._backlog = new Queue<string>();
        }

        public void AddLink(string link) { this._backlog.Enqueue(link); }

        public string GetLink() { return this._backlog.Dequeue(); }
    }
}
