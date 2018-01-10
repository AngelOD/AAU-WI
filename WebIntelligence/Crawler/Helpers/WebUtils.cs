using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Mime;
using System.Text;

namespace Crawler.Helpers
{
    public static class WebUtils
    {
        public static ContentTypes ContentType = ContentTypes.Other;

        public enum ContentTypes
        {
            Html, Text, Other
        }

        public static Encoding GetEncodingFrom(
            NameValueCollection responseHeaders,
            Encoding defaultEncoding = null)
        {
            ContentType = ContentTypes.Other;

            if (responseHeaders == null) { throw new ArgumentNullException(nameof(responseHeaders)); }

            //Note that key lookup is case-insensitive
            var contentType = responseHeaders["Content-Type"];
            if (contentType == null)
                return defaultEncoding;

            var contentTypeParts = contentType.Split(';');
            if (contentTypeParts.Length <= 1)
                return defaultEncoding;

            var contentTypePart = contentTypeParts[0].Trim();
            if (contentTypePart.StartsWith("text/html", StringComparison.InvariantCultureIgnoreCase))
            {
                ContentType = ContentTypes.Html;
            }
            else if (contentTypePart.StartsWith("text/plain", StringComparison.InvariantCultureIgnoreCase))
            {
                ContentType = ContentTypes.Text;
            }

            var charsetPart =
                contentTypeParts.Skip(1).FirstOrDefault(p => p.TrimStart().StartsWith("charset", StringComparison.InvariantCultureIgnoreCase));
            if (charsetPart == null)
                return defaultEncoding;

            var charsetPartParts = charsetPart.Split('=');
            if (charsetPartParts.Length != 2)
                return defaultEncoding;

            var charsetName = charsetPartParts[1].Trim();
            if (charsetName == "")
                return defaultEncoding;

            try
            {
                return Encoding.GetEncoding(charsetName);
            }
            catch (ArgumentException ex)
            {
                throw new InvalidOperationException("The server returned data in an unknown encoding: " + charsetName, ex);
            }
        }
    }
}
