using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Crawler.Models
{
    class RobotsNode
    {
        private RobotsNode parent;
        private HashSet<RobotsNode> children;
        private string path;
        private string regexPattern;
        private bool isDisallowed = false;

        public string Path { get => path; protected set => path = value; }
        public RobotsNode Parent { get => parent; set => parent = value; }
        public HashSet<RobotsNode> Children { get => children; }
        private string RegexPattern {
            get
            {
                if (String.IsNullOrEmpty(regexPattern))
                {
                    regexPattern = String.Format("^{0}/?");
                }

                return regexPattern;
            }
        }

        public bool IsAllowed(string fullPath)
        {
            string checkPath = NormalisePath(fullPath);

            // Check for remaining path
            if (Regex.IsMatch(checkPath, RegexPattern + "$"))
            {
                return isDisallowed;
            }

            // Check for "starts with" path
            if (Regex.IsMatch(checkPath, RegexPattern))
            {
                foreach (var node in Children)
                {
                    if (!node.IsAllowed(Regex.Replace(checkPath, RegexPattern, ""))) { return false; }
                }
            }

            return true;
        }

        public bool AddPath(string pathToAdd, bool allowed)
        {
            string normPath = NormalisePath(pathToAdd);

            if (Regex.IsMatch(normPath, RegexPattern))
            {
                normPath = Regex.Replace(normPath, RegexPattern, "");

                if (String.IsNullOrEmpty(normPath)) { return false; }

                foreach (var node in Children)
                {
                    if (node.AddPath(normPath, allowed)) { return true; }
                }


            }

            return false;
        }

        protected string NormalisePath(string path)
        {
            string retPath = path.ToLower();

            return retPath;
        }
    }
}
