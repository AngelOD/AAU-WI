using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Crawler.Modules
{
    public class RobotsParser
    {
        private readonly Dictionary<string, Regex> _regexes;

        /// <summary>
        /// 
        /// </summary>
        protected HashSet<string> DisallowedList { get; }

        /// <summary>
        /// 
        /// </summary>
        public long CrawlDelay { get; protected set; } = 1;
        public long CrawlDelayMilliseconds => this.CrawlDelay * 1000;

        /// <summary>
        /// 
        /// </summary>
        protected RobotsParser()
        {
            this.DisallowedList = new HashSet<string>();
            this._regexes = new Dictionary<string, Regex>()
            {
                { "userAgent", new Regex("^[Uu]ser-[Aa]gent: (.*)$") },
                { "disallow", new Regex("^[Dd]isallow: (.*)$") },
                { "crawlDelay", new Regex("^[Cc]rawl-[Dd]elay: (.*)$") }
            };
        }

        /// <inheritdoc />
        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        public RobotsParser(string filePath) : this()
        {
            if (!this.ReadFile(filePath))
            {
                // TODO Do something here if it fails?
                Console.WriteLine("Couldn't read the file for some reason.");
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public RobotsParser(Stream stream) : this()
        {
            if (!this.ReadStream(stream))
            {
                // TODO Do something here if it fails?
                Console.WriteLine("Failed to read the stream.");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entry"></param>
        protected void AddDisallowedEntry(string entry)
        {
            if (string.IsNullOrEmpty(entry)) { return; }

            this.DisallowedList.Add(this.PatternizeString(entry));
        }

        /// <summary>
        /// Converts a robots.txt pattern to one compatible with the System.Text.RegularExpressions
        /// flavour of Regex.
        /// </summary>
        /// <param name="pseudoPattern">A robots.txt compatible pattern</param>
        /// <returns>A Regex-compatible version of the input pattern</returns>
        protected string PatternizeString(string pseudoPattern)
        {
            // Escape special characters
            var newPattern = Regex.Replace(pseudoPattern, @"([?+\[\]\\.])", "\\$1");

            // Then replace * wildcards with the regex version
            newPattern = Regex.Replace(newPattern, @"\*", ".*?");

            return newPattern;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        protected bool ReadFile(string filePath)
        {
            if (!File.Exists(filePath)) { return false; }

            var file = new StreamReader(filePath);

            var result = this.ProcessLines(file);

            file.Close();

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        protected bool ReadStream(Stream stream)
        {
            if (stream == null)
            {
                return false;
            }

            var reader = new StreamReader(stream);

            var result = this.ProcessLines(reader);

            reader.Close();

            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool IsAllowed(string path)
        {
            return this.DisallowedList.All(entry => !Regex.IsMatch(path, entry));
        }

        public void PrintRules()
        {
            foreach (var entry in this.DisallowedList)
            {
                Console.WriteLine("Disallowed: {0}", entry);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="streamReader"></param>
        /// <returns></returns>
        protected bool ProcessLines(StreamReader streamReader)
        {
            bool doRegister = false;
            string line;

            while ((line = streamReader.ReadLine()) != null)
            {
                line = line.Trim();

                var uaTest = this._regexes["userAgent"].Match(line);

                if (uaTest.Success)
                {
                    if (doRegister)
                    {
                        // New entry starting here, so just ignore and proceed
                        break;
                    }

                    var agent = uaTest.Groups[1].Value.Trim();

                    if (agent.Equals("*") || agent.Equals("BlazingskiesCrawler"))
                    {
                        doRegister = true;
                    }

                    continue;
                }

                var dTest = this._regexes["disallow"].Match(line);

                if (dTest.Success)
                {
                    var entry = dTest.Groups[1].Value.Trim();

                    if (!string.IsNullOrEmpty(entry) && doRegister)
                    {
                        this.AddDisallowedEntry(entry);
                    }

                    continue;
                }

                var cdTest = this._regexes["crawlDelay"].Match(line);

                if (cdTest.Success && long.TryParse(cdTest.Groups[1].Value, out long crawlDelay))
                {
                    this.CrawlDelay = crawlDelay;
                }
            }

            return true;
        }
    }
}
