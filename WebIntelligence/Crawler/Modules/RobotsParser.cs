using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Crawler.Modules
{
    class RobotsParser
    {
        private HashSet<string> disallowedList;
        private Int64 crawlDelay = 1;
        private Dictionary<string, Regex> regexes;

        /// <summary>
        /// 
        /// </summary>
        protected HashSet<string> DisallowedList { get => disallowedList; }

        /// <summary>
        /// 
        /// </summary>
        public Int64 CrawlDelay { get => crawlDelay; protected set => crawlDelay = value; }

        /// <summary>
        /// 
        /// </summary>
        protected RobotsParser()
        {
            disallowedList = new HashSet<string>();
            regexes = new Dictionary<string, Regex>()
            {
                { "userAgent", new Regex("^[Uu]ser-[Aa]gent: (.*)$") },
                { "disallow", new Regex("^[Dd]isallow: (.*)$") },
                { "crawlDelay", new Regex("^[Cc]rawl-[Dd]elay: (.*)$") }
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        public RobotsParser(string filePath) : this()
        {
            if (!ReadFile(filePath))
            {
                // TODO Do something here if it fails?
                Console.WriteLine("Couldn't read the file for some reason.");
            }
        }

        public RobotsParser(Stream stream) : this()
        {
            if (!ReadStream(stream))
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
            if (String.IsNullOrEmpty(entry)) { return; }

            DisallowedList.Add(PatternizeString(entry));
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
            string newPattern = Regex.Replace(pseudoPattern, @"([?+\[\]\\.])", "\\$1");

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

            var result = ProcessLines(file);

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

            var result = ProcessLines(reader);

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
            foreach (var entry in DisallowedList)
            {
                if (Regex.IsMatch(path, entry)) { return false; }
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        protected bool ProcessLines(StreamReader streamReader)
        {
            bool doRegister = false;
            string line;

            while ((line = streamReader.ReadLine()) != null)
            {
                line = line.Trim();

                if (String.IsNullOrEmpty(line))
                {
                    if (doRegister)
                    {
                        // We've already parsed rules pertaining to us, so break out of the loop here
                        break;
                    }

                    continue;
                }

                var uaTest = regexes["userAgent"].Match(line);

                if (uaTest.Success)
                {
                    var agent = uaTest.Groups[1].Value.Trim();

                    if (agent.Equals("*") || agent.Equals("BlazingskiesCrawler"))
                    {
                        doRegister = true;
                    }

                    continue;
                }

                var dTest = regexes["disallow"].Match(line);

                if (dTest.Success)
                {
                    var entry = dTest.Groups[1].Value.Trim();

                    if (!String.IsNullOrEmpty(entry) && doRegister)
                    {
                        AddDisallowedEntry(entry);
                    }

                    continue;
                }

                var cdTest = regexes["crawlDelay"].Match(line);

                if (cdTest.Success)
                {
                    if (Int64.TryParse(cdTest.Groups[1].Value, out long crawlDelay))
                    {
                        CrawlDelay = crawlDelay;
                    }

                    continue;
                }
            }

            return true;
        }
    }
}
