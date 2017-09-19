using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Crawler.Models;

namespace Crawler.Modules
{
    public class Crawler
    {
        protected const string UserAgent = "BlazingskiesCrawler/v0.1 (by tristan@blazingskies.dk)";

        private readonly Dictionary<string, Regex> _regexes;
        private WebClient _webClient;

        public Crawler()
        {
            this.Queue = new CrawlerQueue();
            this._regexes = new Dictionary<string, Regex>
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
                                    new Regex("[<]script.*?[>].*?[<]/script[>]",
                                              RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Singleline)
                                },
                                {
                                    "styles",
                                    new Regex("[<]style.*?[>].*?[<]/script[>]",
                                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)
                                },
                                {
                                    "tags",
                                    new Regex("[<].+?[>]",
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
                                }
                            };

        }

        protected Dictionary<string, Regex> Regexes { get => this._regexes; }
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
            var pageSource = wc.DownloadString(pageUri);
            var baseAddress = pageUri.GetLeftPart(UriPartial.Path);
            Console.WriteLine("Length: {0}", pageSource.Length);

            // Extract links
            Console.Write("Extracting links... ");

            var linkRegex = this.Regexes["links"];
            var matches = linkRegex.Matches(pageSource);

            foreach (Match match in matches)
            {
                var link = match.Groups["link"];
                var newUri = this.NormalizeUri(baseAddress, link.Value);

                this.Queue.AddLink(newUri);
            }

            Console.WriteLine("Found {0}", matches.Count);

            // Extract text from body
            Console.Write("Extracting body...");

            var bodyRegex = this.Regexes["body"];
            var bodyMatch = bodyRegex.Match(pageSource);

            if (bodyMatch.Groups.Count == 0)
            {
                throw new FormatException("Empty body");
            }

            Console.WriteLine("Length: {0}", bodyMatch.Groups[0].Length);

            // Clean up the body text
            Console.Write("Cleaning up document, resulting in lengths: ");
            var scriptRegex = this.Regexes["scripts"];
            var styleRegex = this.Regexes["styles"];
            var tagRegex = this.Regexes["tags"];
            var lineBreakRegex = this.Regexes["lineBreaks"];
            var multiSpaceRegex = this.Regexes["multiSpaces"];

            var bodyText = scriptRegex.Replace(bodyMatch.Groups["contents"].Value, "");
            Console.Write("{0}, ", bodyText.Length);
            bodyText = styleRegex.Replace(bodyText, "");
            Console.Write("{0}, ", bodyText.Length);
            bodyText = tagRegex.Replace(bodyText, "");
            Console.WriteLine("{0}", bodyText.Length);

            // Decode HTML entities
            Console.Write("Decoding HTML entities... ");
            bodyText = WebUtility.HtmlDecode(bodyText);
            Console.WriteLine("New length: {0}", bodyText.Length);

            // Removing line breaks
            Console.Write("Removing line breaks... ");
            bodyText = lineBreakRegex.Replace(bodyText, " ");
            Console.WriteLine("New length: {0}", bodyText.Length);

            // Removing multi-spaces
            Console.Write("Condensing multiple spaces... ");
            bodyText = multiSpaceRegex.Replace(bodyText, " ");
            Console.WriteLine("New length: {0}", bodyText.Length);

            return new CrawlerLink(pageUri.AbsoluteUri, bodyText);
        }
    }
}
