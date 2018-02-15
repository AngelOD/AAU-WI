using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Sentiment.Models
{
    public class HappyFunTokenizer
    {
        private readonly Dictionary<string, Regex> _regexes;
        private readonly bool _preserveCase;

        public HappyFunTokenizer(bool preserveCase = false)
        {
            const string emoticon = "(?:[<>]?[:;=8][-o*']?[)\\](\\[dDpP/:}{@|\\\\]|[)\\](\\[dDpP/" +
                                    ":}{@|\\\\][-o*']?[:;=8][<>]?)";
            const string phone = "(?:(?:\\+?[01](?:[-.]|\\s)*)?(?:\\(?\\d{3}(?:[-.)]|\\s)*)?\\d" +
                                 "{3}(?:[-.]|\\s)*\\d{4})";
            const string htmlTags = "<[^>]+>";
            const string twitterUsers = "(?:@(?:\\w|_)+)";
            const string twitterHashTags = "(?:#+(?:\\w|_)+(?:\\w|['_-])*(?:\\w|_)+)";
            const string otherWordTypes = "(?:[a-z][a-z'_-]+[a-z])|(?:[+-]?\\d+[,/.:-]\\d+[+-]?)|(?:(?:" +
                                          "\\w|_)+)|(?:\\.(?:\\s*\\.){1,})|(?:\\S)";
            const string htmlEntities = "&(?:#\\d+|\\w+);";
            const string amp = "&amp;";

            var regexStrings = $"({phone}|{emoticon}|{htmlTags}|{twitterUsers}|{twitterHashTags}|{otherWordTypes})";

            this._regexes = new Dictionary<string, Regex>
                            {
                                ["words"] =
                                new Regex(regexStrings,
                                          RegexOptions.Compiled |
                                          RegexOptions.CultureInvariant |
                                          RegexOptions.IgnoreCase),
                                ["emoticons"] =
                                new Regex(emoticon,
                                          RegexOptions.Compiled |
                                          RegexOptions.CultureInvariant |
                                          RegexOptions.IgnoreCase),
                                ["entities"] =
                                new Regex(htmlEntities,
                                          RegexOptions.Compiled |
                                          RegexOptions.CultureInvariant |
                                          RegexOptions.IgnoreCase),
                                ["amp"] =
                                new Regex(amp,
                                          RegexOptions.Compiled |
                                          RegexOptions.CultureInvariant |
                                          RegexOptions.IgnoreCase)
                            };

            this._preserveCase = preserveCase;
        }

        public List<string> Tokenize(string text)
        {
            var words = new List<string>();
            var newText = this.DecodeHtmlEntities(text);
            var matches = this._regexes["words"].Matches(newText);

            foreach (Match match in matches)
            {
                if (!this._preserveCase && !this._regexes["emoticons"].IsMatch(match.Value)) { words.Add(match.Value.ToLowerInvariant()); }
                else { words.Add(match.Value); }
            }

            return words;
        }

        protected string DecodeHtmlEntities(string text)
        {
            var retVal = this._regexes["amp"].Replace(text, " and ");
            var entities = this._regexes["entities"].Matches(retVal);
            var entitySet = new HashSet<string>();

            foreach (Match match in entities) { entitySet.Add(match.Value); }
            foreach (var entity in entitySet) { retVal = retVal.Replace(entity, WebUtility.HtmlDecode(entity)); }

            return retVal;
        }
    }
}
