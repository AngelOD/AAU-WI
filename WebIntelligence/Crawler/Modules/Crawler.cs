using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading;
using Crawler.Helpers;
using Crawler.Models;

namespace Crawler.Modules
{
    public class Crawler
    {
        protected const string UserAgent = "BlazingskiesCrawler/v0.1 (by tristan@blazingskies.dk)";

        private WebClient _webClient;
        private long _lastSaved = 0;
        private int _crawlCount = 1000;

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
                                    new Regex("\\P{L}",
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
        

        public string NormalizeUri(string baseUri, string checkUri) { return this.NormalizeUri(new Uri(baseUri), checkUri); }

        public string NormalizeUri(Uri baseUri, string checkUri)
        {
            // Check if it's a relative Uri
            var outputUri = Uri.IsWellFormedUriString(checkUri, UriKind.Absolute) ? new Uri(checkUri) : new Uri(baseUri, checkUri);

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

            Console.WriteLine("{0} pages left in local queue, {1} in global.", this.LocalQueue.Length, this.Queue.Length);
        }

        public CrawlerLink ParsePage(Uri pageUri)
        {
            Console.Write("Downloading page... ");
            var wc = this.WebClient;
            var pageSource = wc.DownloadStringAwareOfEncoding(pageUri);
            var baseAddress = pageUri.GetLeftPart(UriPartial.Path);
            var baseUri = new Uri(baseAddress);
            Console.WriteLine("Length: {0}", pageSource.Length);

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
            var pagesCrawledTotal = this.CrawledPages.Count;
            var pagesCrawledLocal = 0;

            while (!finished)
            {
                if (!this.LocalQueue.HasLink)
                {
                    // TODO Fetch from other queue
                    finished = true;
                    continue;
                }

                var curLink = this.LocalQueue.GetLink();

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

                    Console.WriteLine("Crawling page #{0} (Local: #{1}/{2})", pagesCrawledTotal + 1, pagesCrawledLocal + 1, this.CrawlCount);
                    var parsedPage = this.ParsePage(curLink);

                    this.PageRegistry.Links.Add(parsedPage);
                    this.CrawledPages.Add(curLink);

                    pagesCrawledTotal++;
                    pagesCrawledLocal++;

                    lastCrawl = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (pagesCrawledLocal % 10 == 0) { this.SaveCrawlerData(); }
                    if (pagesCrawledLocal >= this.CrawlCount) { finished = true; }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
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
            var deserializer = new BinaryFormatter();

            var crawledPages = (HashSet<string>)deserializer.Deserialize(file);

            file.Close();

            return crawledPages;
        }

        private void SaveCrawledPages(string fileName)
        {
            var file = File.Create(Path.Combine(Utilities.UserAppDataPath, fileName));
            var serializer = new BinaryFormatter();

            serializer.Serialize(file, this.CrawledPages);

            file.Close();
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

            if ((curTime - this._lastSaved) > 900)
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

                if (File.Exists(fromFile))
                {
                    File.Copy(fromFile, toFile);
                }
            }
        }
    }
}
