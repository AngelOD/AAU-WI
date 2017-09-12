using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Crawler.Modules
{
    class RobotsParser
    {
        protected string PatternizeString(string pseudoPattern)
        {
            // Start by replacing dots with their escaped counterparts
            string newPattern = Regex.Replace(pseudoPattern, @"\.", @"\.");

            // Then replace * wildcards with the regex version
            newPattern = Regex.Replace(newPattern, @"\*(.)", "[^$1]*$1");

            return newPattern;
        }
    }
}
