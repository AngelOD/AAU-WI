using System;
using System.Net;
using System.Text;

namespace Crawler.Helpers
{
    public static class WebClientExtensions
    {
        public static string DownloadStringAwareOfEncoding(this WebClient webClient, Uri uri)
        {
            var rawData = webClient.DownloadData(uri);
            var encoding = WebUtils.GetEncodingFrom(webClient.ResponseHeaders, Encoding.UTF8);

            return WebUtils.ContentType != WebUtils.ContentTypes.Other ? encoding.GetString(rawData) : "";
        }
    }
}
