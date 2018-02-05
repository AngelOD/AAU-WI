using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Complex;

namespace Sentiment.Models
{
    public class Network
    {
        private readonly Dictionary<string, Regex> _regexes;
        private readonly Dictionary<string, int> _fromNameIndex;
        private readonly Dictionary<int, string> _toNameIndex;

        public Dictionary<string, NetworkNode> NetworkNodes { get; protected set; }

        public Network(string filePath)
        {
            this.NetworkNodes = new Dictionary<string, NetworkNode>();
            this._fromNameIndex = new Dictionary<string, int>();
            this._toNameIndex = new Dictionary<int, string>();

            this._regexes = new Dictionary<string, Regex>()
                            {
                                {
                                    "user",
                                    new Regex("^user: (?<name>.*)$",
                                              RegexOptions.IgnoreCase | RegexOptions.Compiled)
                                },
                                {
                                    "friends",
                                    new Regex("^friends:(?<friends>.*)$",
                                              RegexOptions.IgnoreCase | RegexOptions.Compiled)
                                },
                                {
                                    "summary",
                                    new Regex("^summary: (?<summary>.*)$",
                                              RegexOptions.IgnoreCase | RegexOptions.Compiled)
                                },
                                {
                                    "review",
                                    new Regex("^review: (?<review>.*)$",
                                              RegexOptions.IgnoreCase | RegexOptions.Compiled)
                                }
                            };

            this.LoadFromFile(filePath);
            this.SetupIndexes();
        }

        protected bool LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) { return false; }

            var file = new StreamReader(filePath);
            var result = this.ProcessFile(file);

            // Probably not necessary as the graph appears to be undirected, but just in case...
            if (result) { this.CalculateInDegrees(); }

            file.Close();

            return result;
        }

        protected bool ProcessFile(StreamReader sr)
        {
            var friends = "";
            var name = "";
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();

                if (line.Length == 0)
                {
                    if (name.Length > 0 && friends.Length > 0)
                    {
                        this.AddNode(name, friends);
                        name = "";
                        friends = "";
                    }

                    continue;
                }

                var uTest = this._regexes["user"].Match(line);
                if (uTest.Success)
                {
                    name = uTest.Groups["name"].Value.Trim();
                    continue;
                }

                var fTest = this._regexes["friends"].Match(line);
                if (fTest.Success)
                {
                    friends = fTest.Groups["friends"].Value.Trim();
                    continue;
                }

                var sTest = this._regexes["summary"].Match(line);
                if (sTest.Success)
                {
                    //
                    continue;
                }

                var rTest = this._regexes["review"].Match(line);
                if (rTest.Success)
                {
                    //
                    continue;
                }
            }

            if (name.Length > 0 && friends.Length > 0)
            {
                this.AddNode(name, friends);
            }

            return true;
        }

        protected void AddNode(string name, string friends)
        {
            this.NetworkNodes.Add(name, new NetworkNode(name, friends));
        }

        protected void CalculateInDegrees()
        {
            foreach (var node in this.NetworkNodes)
            {
                foreach (var name in node.Value.Friends) { this.NetworkNodes[name].InDegree++; }
            }
        }

        protected void SetupIndexes()
        {
            var i = 0;

            foreach (var node in this.NetworkNodes)
            {
                this._fromNameIndex[node.Key] = i;
                this._toNameIndex[i] = node.Key;
                i++;
            }
        }

        public Dictionary<int, HashSet<int>> ToAdjacencyList()
        {
            var retVal = new Dictionary<int, HashSet<int>>();

            foreach (var node in this._fromNameIndex)
            {
                retVal[node.Value] = new HashSet<int>();

                foreach (var friend in this.NetworkNodes[node.Key].Friends)
                {
                    retVal[node.Value].Add(this._fromNameIndex[friend]);
                }
            }

            return retVal;
        }

        public Matrix<int> ToAdjacencyMatrix()
        {
            var lst = this.ToAdjacencyList();
            var retVal = Matrix<int>.Build.Dense(lst.Count, lst.Count);

            //

            return retVal;
        }
    }
}
