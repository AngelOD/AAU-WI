using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MathNet.Numerics.LinearAlgebra;

namespace Sentiment.Models
{
    public class Network
    {
        private readonly Dictionary<string, Regex> _regexes;
        private readonly Dictionary<string, int> _fromNameIndex;
        private readonly Dictionary<int, string> _toNameIndex;

        public Dictionary<string, NetworkNode> NetworkNodes { get; protected set; }

        protected Network()
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
        }

        public Network(string filePath) : this()
        {
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

        protected void AddNode(NetworkNode node)
        {
            this.NetworkNodes.Add(node.Name, new NetworkNode(node));
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

        public Matrix<double> ToAdjacencyMatrix() { return this.ToAdjacencyMatrix(false); }

        public Matrix<double> ToAdjacencyMatrix(bool forSpectral)
        {
            var lst = this.ToAdjacencyList();
            var retVal = Matrix<double>.Build.Dense(lst.Count, lst.Count);
            var setValue = (forSpectral ? -1 : 1);

            foreach (var entry in lst)
            {
                var rowEntries = 0;

                foreach (var friend in entry.Value)
                {
                    retVal[entry.Key, friend] = setValue;
                    rowEntries++;
                }

                if (forSpectral) { retVal[entry.Key, entry.Key] = rowEntries; }
            }

            return retVal;
        }

        public bool DoSpectralClusteringSplit(out Network n1, out Network n2)
        {
            n1 = new Network();
            n2 = new Network();

            // Setup matrix, eigenvector and sort it
            Console.WriteLine("Calculating adjacency matrix...");
            var am = this.ToAdjacencyMatrix(true);
            Console.WriteLine("Calculating eigenvectors...");
            var eigenVector = am.Evd().EigenVectors.Column(1);
            Console.WriteLine("Augmenting eigenvectors...");
            var augmentIndex = 0;
            var augmentedEigenVector =
                (from entry in eigenVector
                 select new KeyValuePair<int, double>(augmentIndex++, entry))
                .ToList();
            Console.WriteLine("Sorting augmented eigenvectors...");
            var sortedEigenVector = augmentedEigenVector.OrderBy(v => v.Value).ToArray();

            Console.WriteLine("Locating edge to split on...");

            // Find largest difference
            var maxVal = 0d;
            var maxIndex = -1;

            for (var i = 1; i < sortedEigenVector.Length; i++)
            {
                var diff = Math.Abs(sortedEigenVector[i - 1].Value - sortedEigenVector[i].Value);

                if (!(diff > maxVal)) continue;

                maxIndex = i;
                maxVal = diff;
            }

            Console.WriteLine("Adding first nodes to networks...");

            var cluster1 = sortedEigenVector[maxIndex];
            var cluster2 = sortedEigenVector[maxIndex - 1];

            n1.AddNode(this.NetworkNodes[this._toNameIndex[cluster1.Key]]);
            n2.AddNode(this.NetworkNodes[this._toNameIndex[cluster2.Key]]);

            Console.WriteLine("Sort nodes into networks...");

            foreach (var entry in augmentedEigenVector)
            {
                if (entry.Key == cluster1.Key || entry.Key == cluster2.Key) { continue; }

                var diff1 = Math.Abs(cluster1.Value - entry.Value);
                var diff2 = Math.Abs(cluster2.Value - entry.Value);

                if (diff1 < diff2)
                {
                    n1.AddNode(this.NetworkNodes[this._toNameIndex[entry.Key]]);
                }
                else
                {
                    n2.AddNode(this.NetworkNodes[this._toNameIndex[entry.Key]]);
                }
            }

            // Generate indexes and clean it up
            Console.WriteLine("Generate indexes for network1...");
            n1.SetupIndexes();
            Console.WriteLine("Clean network1...");
            n1.CleanNetwork();
            Console.WriteLine("Generate indexes for network2...");
            n2.SetupIndexes();
            Console.WriteLine("Clean network2...");
            n2.CleanNetwork();

            return true;
        }

        protected void CleanNetwork()
        {
            foreach (var node in this.NetworkNodes)
            {
                node.Value.Friends.RemoveWhere(friend => !this._fromNameIndex.ContainsKey(friend));
            }
        }
    }
}
