#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using Crawler.Helpers;
using Crawler.Models;
#endregion

namespace Crawler.Modules
{
    public class Crawler
    {
        protected const string UserAgent = "BlazingskiesCrawler/v0.1 (by tristan@blazingskies.dk)";
        protected const string FileIdent = "BSCCP";
        protected const int FileVersion = 1;
        private int _crawlCount = 1000;
        private long _lastSaved;

        private WebClient _webClient;

        public Crawler()
        {
            this.CrawledPages = new HashSet<string>();
            this.Queue = new CrawlerQueue();
            this.LocalQueue = new CrawlerQueue();
            this.PageRegistry = new CrawlerRegistry();
            this.RobotsParsers = new Dictionary<string, RobotsParser>();
            this.Regexes = new Dictionary<string, Regex>
                           {
                               {
                                   "links",
                                   new Regex("<a.*?href=(?:['\"])?(?<link>[^'\"> ]*)(?:['\"])?.*?",
                                             RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline)
                               },
                               {
                                   "body",
                                   new Regex("<body(?:[ ][^>]*)?>(?<contents>.*?)</body>",
                                             RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline)
                               },
                               {
                                   "scripts",
                                   new Regex("<script[^>]*>.*?</script>",
                                             RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline)
                               },
                               {
                                   "styles",
                                   new Regex("<style[^>]*>.*?</style>",
                                             RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)
                               },
                               {
                                   "tags",
                                   new Regex("<[^>]+>",
                                             RegexOptions.Compiled | RegexOptions.Singleline)
                               },
                               {
                                   "lineBreaks",
                                   new Regex(@"[\u000A\u000B\u000C\u000D\u2028\u2029\u0085]+",
                                             RegexOptions.Compiled)
                               },
                               {
                                   "multiSpaces",
                                   new Regex("[ ]{2,}",
                                             RegexOptions.Compiled)
                               },
                               {
                                   "nonLetters",
                                   new Regex("[^A-Za-z]",
                                             RegexOptions.Compiled)
                               }
                           };
        }

        protected Dictionary<string, Regex> Regexes { get; }

        protected WebClient WebClient
        {
            get
            {
                if (this._webClient != null) return this._webClient;
                this._webClient = new WebClient();
                this._webClient.Headers.Set(HttpRequestHeader.UserAgent, UserAgent);

                return this._webClient;
            }
        }

        protected CrawlerQueue Queue { get; set; }
        protected CrawlerQueue LocalQueue { get; set; }
        protected CrawlerRegistry PageRegistry { get; set; }
        protected HashSet<string> CrawledPages { get; set; }
        protected Dictionary<string, RobotsParser> RobotsParsers { get; set; }

        public int CrawlCount
        {
            get => this._crawlCount;
            set
            {
                if (value > 0) { this._crawlCount = value; }
            }
        }

        public void OutputIndex()
        {
            var index = this.PageRegistry.Index;
            var file = File.Create(Path.Combine(Utilities.UserAppDataPath, "index.txt"));
            var sw = new StreamWriter(file);

            foreach (var entry in index)
            {
                Console.Write("{0} -> ", entry.Key);
                sw.Write("{0} -> ", entry.Key);

                foreach (var value in entry.Value)
                {
                    Console.Write("{0}:{1} ", value.LinkId, value.Frequency);
                    sw.Write("{0}:{1} ", value.LinkId, value.Frequency);
                }

                Console.WriteLine();
                sw.WriteLine();
            }

            sw.Close();
        }

        public void OutputPage(int pageNum)
        {
            this.PageRegistry[pageNum].Output();
        }

        public void ExecuteBooleanQuery(string query)
        {
            var bq = new BooleanQuery();
            var result = bq.ParseQuery(query);

            result.Output();

            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            var results = result.Execute(this.PageRegistry);

            foreach (var entry in results)
            {
                Console.Write("{0}, ", entry.LinkId);
            }

            Console.WriteLine();
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();
        }

        public string NormalizeUri(string baseUri, string checkUri)
        {
            return this.NormalizeUri(new Uri(baseUri), checkUri);
        }

        public string NormalizeUri(Uri baseUri, string checkUri)
        {
            // Check if it's a relative Uri
            var outputUri = Uri.IsWellFormedUriString(checkUri, UriKind.Absolute)
                                ? new Uri(checkUri)
                                : new Uri(baseUri, checkUri);

            // Check scheme
            if (!(outputUri.Scheme.Equals("http") || outputUri.Scheme.Equals("https")))
            {
                throw new ArgumentException("Invalid scheme. Only http and https permitted.");
            }

            return outputUri.AbsoluteUri;
        }

        public void PrintInfo()
        {
            Console.WriteLine("{0} pages crawled:", this.CrawledPages.Count);
            Console.WriteLine();
            Console.WriteLine("{0} pages left in local queue, {1} in global.", this.LocalQueue.Length,
                              this.Queue.Length);
        }

        public void PrintDomainInfo()
        {
            var domains = new Dictionary<string, int>();

            foreach (var crawledPage in this.CrawledPages)
            {
                var baseUrl = Utilities.GetUrlBase(crawledPage);

                if (domains.ContainsKey(baseUrl)) { domains[baseUrl]++; }
                else { domains[baseUrl] = 1; }
            }

            var sortedDomains =
                from d in domains
                orderby d.Key
                select d;

            foreach (var domain in sortedDomains)
            {
                Console.WriteLine("{0} [{1}]", domain.Key, domain.Value);
            }

            Console.WriteLine();
            Console.WriteLine("A total of {0} pages spanning {1} domains.", this.CrawledPages.Count, domains.Count);
        }

        public CrawlerLink ParsePage(Uri pageUri)
        {
            Console.Write("Downloading page... ");
            var wc = this.WebClient;
            var pageSource = wc.DownloadStringAwareOfEncoding(pageUri);
            var baseAddress = pageUri.GetLeftPart(UriPartial.Path);
            var baseUri = new Uri(baseAddress);
            Console.WriteLine("Length: {0}", pageSource.Length);

            if (pageSource.Length == 0)
            {
                Console.WriteLine("Empty page. Skipping!");
                return null;
            }

            // Extract links
            Console.Write("Extracting links... ");
            var matches = this.Regexes["links"].Matches(pageSource);
            var links = new List<string>();
            var localCount = 0;
            var externCount = 0;

            foreach (Match match in matches)
            {
                try
                {
                    var link = match.Groups["link"].Value;

                    // Check for presence of an anchor tag
                    var pos = link.IndexOf('#');

                    if (pos > -1) { link = link.Substring(0, pos); }

                    if (link.Length == 0) { continue; }

                    var newUri = this.NormalizeUri(baseAddress, link);

                    links.Add(newUri);

                    if (baseUri.IsBaseOf(new Uri(newUri)))
                    {
                        this.LocalQueue.AddLink(newUri);
                        localCount++;
                    }
                    else
                    {
                        this.Queue.AddLink(newUri);
                        externCount++;
                    }
                }
                catch (ArgumentException)
                {
                    // Ignore exception
                    Console.WriteLine("Ignoring link: {0}", match.Groups["link"].Value);
                }
            }

            Console.WriteLine("Found {0} ({1} local, {2} extern)", matches.Count, localCount, externCount);

            Console.Write("Removing script areas... ");
            var noScriptText = this.Regexes["scripts"].Replace(pageSource, " ");
            Console.WriteLine("Length: {0}", noScriptText.Length);

            // Extract text from body
            Console.Write("Extracting body... ");
            var bodyMatch = this.Regexes["body"].Match(noScriptText);
            string bodyText;

            if (bodyMatch.Groups["contents"].Success)
            {
                Console.WriteLine("Length: {0}", bodyMatch.Groups["contents"].Length);
                bodyText = bodyMatch.Groups["contents"].Value;
            }
            else
            {
                Console.WriteLine("Not found! Using whole document instead!");
                bodyText = noScriptText;
            }


            // Clean up the body text
            Console.Write("Cleaning up document, resulting in lengths: ");
            bodyText = this.Regexes["styles"].Replace(bodyText, " ");
            Console.Write("{0}, ", bodyText.Length);
            bodyText = this.Regexes["tags"].Replace(bodyText, " ");
            Console.WriteLine("{0}", bodyText.Length);

            // Decode HTML entities
            Console.Write("Decoding HTML entities... ");
            bodyText = WebUtility.HtmlDecode(bodyText);
            Console.WriteLine("New length: {0}", bodyText.Length);

            // Removing line breaks
            Console.Write("Removing line breaks... ");
            bodyText = this.Regexes["lineBreaks"].Replace(bodyText, " ");
            Console.WriteLine("New length: {0}", bodyText.Length);

            // Remove anything that aren't letters
            Console.Write("Cleaning non-letters from document... ");
            bodyText = this.Regexes["nonLetters"].Replace(bodyText.ToLower(), " ");
            Console.WriteLine("New length: {0}", bodyText.Length);

            // Removing multi-spaces
            Console.Write("Condensing multiple spaces... ");
            bodyText = this.Regexes["multiSpaces"].Replace(bodyText, " ");
            Console.WriteLine("New length: {0}", bodyText.Length);

            return new CrawlerLink(pageUri.AbsoluteUri, bodyText, links);
        }

        public void Crawl(IEnumerable<string> seedUris)
        {
            this.SetSeedUris(seedUris);
            this.Crawl();
        }

        public void Crawl()
        {
            var finished = false;
            long lastCrawl = 0;
            var curLink = "";
            var pagesCrawledTotal = this.CrawledPages.Count;
            var pagesCrawledLocal = 0;
            var pagesSinceMainQueueFetch = 0;

            if (this.LocalQueue.Length > 100)
            {
                Console.Write("Limiting local queue to 100 entries... ");
                var links = this.LocalQueue.GetOverflow(100);

                while (links.Count > 0)
                {
                    this.Queue.AddLink(links.Dequeue());
                }

                Console.WriteLine("Done!");
            }

            while (!finished)
            {
                if (!this.LocalQueue.HasLink)
                {
                    Console.WriteLine("No more links in local queue. Need to fetch new ones!");
                    if (this.Queue.HasLink)
                    {
                        this.LocalQueue.ReplaceQueue(this.Queue.GetLinkCollection(curLink));
                        pagesSinceMainQueueFetch = 0;
                    }
                    else { finished = true; }
                    continue;
                }

                curLink = this.LocalQueue.GetLink();
                var curUri = new Uri(curLink);

                try
                {
                    if (this.CrawledPages.Contains(curLink)) { continue; }

                    var baseUri = Utilities.GetUrlBase(curLink);

                    if (!this.RobotsParsers.TryGetValue(baseUri, out var robotsParser))
                    {
                        var robotsStream = this.WebClient.OpenRead(new Uri(new Uri(baseUri), "robots.txt"));
                        robotsParser = new RobotsParser(robotsStream);
                    }

                    var timeElapsed = DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastCrawl;
                    if (timeElapsed < robotsParser.CrawlDelayMilliseconds)
                    {
                        var delay = (int) (robotsParser.CrawlDelayMilliseconds - timeElapsed) + 100;
                        Console.WriteLine("Sleeping for {0} ms.", delay);
                        Thread.Sleep(delay);
                    }

                    if (!robotsParser.IsAllowed(curUri.PathAndQuery))
                    {
                        Console.WriteLine("Ignoring page due to robots.txt limitation: {0}", curLink);
                        continue;
                    }

                    Console.WriteLine("Crawling page #{0} (Local: #{1}/{2}) (SLF: {3})", pagesCrawledTotal + 1,
                                      pagesCrawledLocal + 1, this.CrawlCount, pagesSinceMainQueueFetch + 1);
                    Console.WriteLine("[URL] {0}", curLink);
                    var parsedPage = this.ParsePage(curLink);

                    if (parsedPage == null) { continue; }

                    this.PageRegistry.AddLink(parsedPage);
                    this.CrawledPages.Add(curLink);

                    pagesCrawledTotal++;
                    pagesCrawledLocal++;
                    pagesSinceMainQueueFetch++;

                    lastCrawl = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (pagesCrawledLocal % 10 == 0) { this.SaveCrawlerData(); }
                    if (pagesCrawledLocal >= this.CrawlCount) { finished = true; }

                    if (pagesSinceMainQueueFetch > 100)
                    {
                        Console.Write("Clearing local queue to give other domains a shot... ");
                        var links = this.LocalQueue.GetOverflow(0);

                        while (links.Count > 0)
                        {
                            this.Queue.AddLink(links.Dequeue());
                        }

                        Console.WriteLine("Done!");
                    }
                }
                catch (Exception e) { Console.WriteLine(e); }
            }

            this.SaveCrawlerData();
        }

        public CrawlerLink ParsePage(string url) { return this.ParsePage(new Uri(url)); }

        public void SetSeedUris(IEnumerable<string> seedUris)
        {
            Uri baseUri = null;

            foreach (var seedUri in seedUris)
            {
                if (baseUri == null)
                {
                    var tmpUri = new Uri(seedUri);
                    baseUri = new Uri(tmpUri.GetLeftPart(UriPartial.Path));

                    this.LocalQueue.AddLink(seedUri);
                }
                else
                {
                    if (baseUri.IsBaseOf(new Uri(seedUri))) { this.LocalQueue.AddLink(seedUri); }
                    else { this.Queue.AddLink(seedUri); }
                }
            }
        }

        private HashSet<string> LoadCrawledPages(string fileName)
        {
            var file = File.OpenRead(Path.Combine(Utilities.UserAppDataPath, fileName));
            var br = new BinaryReader(file);

            // TODO Make this better along the same lines as SaveCrawledPages
            // Header
            var ident = br.ReadString();
            var ver = br.ReadInt32();

            if (!ident.Equals(FileIdent) || ver != FileVersion)
            {
                throw new FileLoadException("Incorrect format!");
            }

            var pageCount = br.ReadInt32();
            var crawledPages = new HashSet<string>();

            for (var i = 0; i < pageCount; i++) { crawledPages.Add(br.ReadString()); }

            br.Close();

            return crawledPages;
        }

        private void SaveCrawledPages(string fileName)
        {
            var file = File.Create(Path.Combine(Utilities.UserAppDataPath, fileName));
            var bw = new BinaryWriter(file);

            // TODO Make this better with some generic function somewhere
            // Header
            bw.Write(FileIdent);
            bw.Write(FileVersion);

            // Entries
            bw.Write(this.CrawledPages.Count);
            foreach (var crawledPage in this.CrawledPages)
            {
                bw.Write(crawledPage);
            }

            bw.Close();
        }

        public bool LoadCrawlerData()
        {
            this.PageRegistry = CrawlerRegistry.LoadFromFile("registry.dat");
            this.LocalQueue = CrawlerQueue.LoadFromFile("local_queue.dat");
            this.Queue = CrawlerQueue.LoadFromFile("queue.dat");
            this.CrawledPages = this.LoadCrawledPages("crawled_pages.dat");

            return true;
        }

        public bool SaveCrawlerData()
        {
            var curTime = DateTimeOffset.Now.ToUnixTimeSeconds();

            if ((curTime - this._lastSaved) > 300)
            {
                this.BackupCrawlerData();
                this._lastSaved = curTime;
            }

            CrawlerRegistry.SaveToFile("registry.dat", this.PageRegistry);
            CrawlerQueue.SaveToFile("local_queue.dat", this.LocalQueue);
            CrawlerQueue.SaveToFile("queue.dat", this.Queue);
            this.SaveCrawledPages("crawled_pages.dat");

            GC.Collect();

            return true;
        }

        public void BackupCrawlerData()
        {
            var sourcePath = Utilities.UserAppDataPath;
            var destPath = Path.Combine(Utilities.UserAppDataPath, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            var fileNames = new[]
                            {
                                "registry.dat",
                                "local_queue.dat",
                                "queue.dat",
                                "crawled_pages.dat"
                            };

            Directory.CreateDirectory(destPath);

            foreach (var fileName in fileNames)
            {
                var fromFile = Path.Combine(sourcePath, fileName);
                var toFile = Path.Combine(destPath, fileName);

                if (File.Exists(fromFile)) { File.Copy(fromFile, toFile); }
            }
        }
    }
}
