using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Crawler.Modules
{
    class RobotsParser
    {
        private HashSet<string> disallowedList;

        protected HashSet<string> DisallowedList { get => disallowedList; }

        public RobotsParser()
        {
            disallowedList = new HashSet<string>();
        }

        public RobotsParser(string filePath) : this()
        {
            if (ReadFile(filePath))
            {
                DebugPrintAll();
            }
        }

        public void AddDisallowedEntry(string entry)
        {
            if (String.IsNullOrEmpty(entry)) { return; }

            DisallowedList.Add(PatternizeString(entry));
        }

        public void DebugPrintAll()
        {
            foreach (var entry in DisallowedList)
            {
                Console.WriteLine(entry);
            }
        }

        protected string PatternizeString(string pseudoPattern)
        {
            // Escape special characters
            string newPattern = Regex.Replace(pseudoPattern, @"([?+\[\]\\.])", "\\$1");

            // Then replace * wildcards with the regex version
            newPattern = Regex.Replace(newPattern, @"\*", ".*?");

            return newPattern;
        }

        protected bool ReadFile(string filePath)
        {
            int counter = 0;
            string line;
            bool doRegister = false;

            if (!File.Exists(filePath)) { return false; }

            var userAgentRegex = new Regex("^User-agent: (.*)$");
            var disallowRegex = new Regex("^Disallow: (.*)$");
            var file = new StreamReader(filePath);

            while ((line = file.ReadLine()) != null)
            {
                counter++;
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

                var uaTest = userAgentRegex.Match(line);

                if (uaTest.Success)
                {
                    var agent = uaTest.Groups[1].Value.Trim();

                    if (agent.Equals("*") || agent.Equals("BlazingskiesCrawler"))
                    {
                        doRegister = true;
                    }

                    continue;
                }

                var dTest = disallowRegex.Match(line);

                if (dTest.Success)
                {
                    var entry = dTest.Groups[1].Value.Trim();

                    if (!String.IsNullOrEmpty(entry) && doRegister)
                    {
                        AddDisallowedEntry(entry);
                    }
                }
            }

            file.Close();

            return true;
        }

        public bool IsAllowed(string path)
        {
            foreach (var entry in DisallowedList)
            {
                if (Regex.IsMatch(path, entry)) { return false; }
            }

            return true;
        }
    }
}
