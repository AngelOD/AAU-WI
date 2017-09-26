using System;
using System.Collections.Generic;
using System.Net;
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
                                    new Regex("<a.*?href=(['\"])(?<link>.*?)\\1.*?",
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

        protected CrawlerQueue Queue { get; }
        protected CrawlerQueue LocalQueue { get; }
        protected CrawlerRegistry PageRegistry { get; }
        protected HashSet<string> CrawledPages { get; }
        protected Dictionary<string, RobotsParser> RobotsParsers { get; }
        

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
            var localCount = 0;
            var externCount = 0;

            foreach (Match match in matches)
            {
                try
                {
                    var link = match.Groups["link"];
                    var newUri = this.NormalizeUri(baseAddress, link.Value);

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
                }
            }

            Console.WriteLine("Found {0} ({1} local, {2} extern)", matches.Count, localCount, externCount);

            Console.Write("Removing script areas... ");
            var noScriptText = this.Regexes["scripts"].Replace(pageSource, " ");
            Console.WriteLine("Length: {0}", noScriptText.Length);

            // Extract text from body
            Console.Write("Extracting body...");
            var bodyMatch = this.Regexes["body"].Match(noScriptText);

            if (bodyMatch.Groups.Count == 0)
            {
                throw new FormatException("Empty body");
            }

            Console.WriteLine("Length: {0}", bodyMatch.Groups["contents"].Length);
            var bodyText = bodyMatch.Groups["contents"].Value;

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

            return new CrawlerLink(pageUri.AbsoluteUri, bodyText);
        }

        public void Crawl(IEnumerable<string> seedUris)
        {
            this.SetSeedUris(seedUris);
            this.Crawl();
        }

        public void Crawl()
        {
            var finished = false;
            var lastCrawl = 0;

            while (!finished)
            {
                if (!this.LocalQueue.HasLink())
                {
                    // TODO Fetch from other queue
                    finished = true;
                    continue;
                }

                var curLink = this.LocalQueue.GetLink();

                try
                {
                    var baseUri = this.GetUrlBase(curLink);

                    if (!this.RobotsParsers.TryGetValue(baseUri, out var robotsParser))
                    {
                        var robotsStream = this.WebClient.OpenRead(new Uri(new Uri(baseUri), "robots.txt"));
                        robotsParser = new RobotsParser(robotsStream);
                    }

                    var timeElapsed = DateTimeOffset.Now.ToUnixTimeSeconds() - lastCrawl;
                    if (timeElapsed < robotsParser.CrawlDelay)
                    {
                        var delay = (int) (robotsParser.CrawlDelay - timeElapsed) + 1;
                        Thread.Sleep(delay * 1000);
                    }

                    var parsedPage = this.ParsePage(curLink);

                    this.PageRegistry.Links.Add(parsedPage);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
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

        protected string GetUrlBase(string url)
        {
            var uri = new Uri(url);

            return uri.GetLeftPart(UriPartial.Authority);
        }
    }
}
