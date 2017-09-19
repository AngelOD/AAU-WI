﻿using System;
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

        private WebClient _webClient;

        public Crawler()
        {
            this.Queue = new CrawlerQueue();
        }

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
            var wc = this.WebClient;
            var pageSource = wc.DownloadString(pageUri);
            var baseAddress = pageUri.GetLeftPart(UriPartial.Path);

            // Extract links
            var linkRegex = new Regex("<a.*?href=(['\"])(?<Link>.*?)\\1.*?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var matches = linkRegex.Matches(pageSource);

            foreach (Match match in matches)
            {
                var link = match.Groups["Link"];
                var newUri = this.NormalizeUri(baseAddress, link.Value);

                this.Queue.AddLink(newUri);
            }

            // Extract text
            var bodyRegex = new Regex("^.?[<]body[>](?<contents>.*?)[<]/body[>].*$");
            var scriptRegex = new Regex("[<]script.*?[>].*?[<]/script[>]");
        }
    }
}
