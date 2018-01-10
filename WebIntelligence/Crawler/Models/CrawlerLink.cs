#region Using Directives
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Crawler.Helpers;
#endregion

namespace Crawler.Models
{
    [Serializable]
    public class CrawlerLink
    {
        private int _uniqueId;

        public CrawlerLink(BinaryReader br)
        {
            this.ShingleHashes = new LinkedList<ulong>();
            this.Tokens = new HashSet<string>();
            this.Keywords = new Dictionary<string, int>();
            this.Links = new HashSet<string>();
            this.LoadFrom(br);
        }

        public CrawlerLink(string address, string contents, IEnumerable<string> links)
        {
            Console.WriteLine("Processing contents...");

            this.Address = address;

            // Store links
            this.Links = new HashSet<string>(links);

            // Tokenize
            Console.Write("Tokenizing... ");
            var tokens = new List<string>(contents.Split(' '));
            Console.WriteLine("Done!");
            Console.Write("Removing short and stop word tokens... ");
            tokens.RemoveAll(token => token.Length <= 1 || StopWords.StopWordsList.Contains(token));
            Console.WriteLine("Done!");

            // Generate shingle hashes
            Console.Write("Generating shingle hashes... ");
            var jaccard = new Jaccard();
            this.ShingleHashes = new LinkedList<ulong>(jaccard.HashedShinglifyDocument(tokens.ToArray()));
            Console.WriteLine("Done!");

            // Apply stemming
            Console.Write("Stemming tokens... ");
            var stemmer = new PorterStemmer();
            var stemmedTokens = new List<string>(tokens.Select(token => stemmer.StemWord(token)));
            this.Tokens = new HashSet<string>(stemmedTokens);
            Console.WriteLine("Done!");

            // Sort elements
            Console.Write("Sorting stemmed tokens... ");
            stemmedTokens.Sort();
            Console.WriteLine("Done!");

            // Get keyword count
            Console.Write("Adding stemmed tokens to dictionary... ");
            var lastKeyword = "";
            var keywords = new Dictionary<string, int>();

            foreach (var stemmedToken in stemmedTokens)
            {
                if (!stemmedToken.Equals(lastKeyword))
                {
                    lastKeyword = stemmedToken;
                    keywords[stemmedToken] = 1;
                }
                else { keywords[stemmedToken] += 1; }
            }

            this.Keywords = keywords;
            Console.WriteLine("Done!");
        }

        public int UniqueId
        {
            get => this._uniqueId;
            set
            {
                if (this._uniqueId == 0) { this._uniqueId = value; }
            }
        }

        public string Address { get; protected set; }
        public LinkedList<ulong> ShingleHashes { get; protected set; }
        public HashSet<string> Tokens { get; protected set; }
        public Dictionary<string, int> Keywords { get; protected set; }
        public HashSet<string> Links { get; protected set; }

        public void Output()
        {
            Console.WriteLine(this.Address);
            Console.WriteLine();
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();

            Console.WriteLine("[Keywords]");
            foreach (var keyword in this.Keywords)
            {
                Console.Write("{0}[{1}] ", keyword.Key, keyword.Value);
            }

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("[Links]");
            foreach (var link in this.Links)
            {
                Console.WriteLine(link);
            }
        }

        public void CleanTokens()
        {
            var cleaner = new Regex("[^A-Za-z]", RegexOptions.Compiled);
            var tokens = this.Tokens.ToList();
            var newTokens = tokens.Select(token => cleaner.Replace(token, "")).ToList();

            newTokens.RemoveAll(token => token.Length <= 1 || StopWords.StopWordsList.Contains(token));

            var jaccard = new Jaccard();
            this.ShingleHashes = new LinkedList<ulong>(jaccard.HashedShinglifyDocument(newTokens.ToArray()));

            var stemmer = new PorterStemmer();
            var stemmedTokens = new List<string>(newTokens.Select(token => stemmer.StemWord(token)));
            this.Tokens = new HashSet<string>(stemmedTokens);

            stemmedTokens.Sort();

            var lastKeyword = "";
            var keywords = new Dictionary<string, int>();

            foreach (var stemmedToken in stemmedTokens)
            {
                if (!stemmedToken.Equals(lastKeyword))
                {
                    lastKeyword = stemmedToken;
                    keywords[stemmedToken] = 1;
                }
                else { keywords[stemmedToken] += 1; }
            }

            this.Keywords = keywords;
        }

        public bool SaveTo(BinaryWriter bw)
        {
            bw.Write(this.UniqueId);
            bw.Write(this.Address);

            bw.Write(this.ShingleHashes.Count);
            foreach (var shingle in this.ShingleHashes)
            {
                bw.Write(shingle);
            }

            bw.Write(this.Tokens.Count);
            foreach (var token in this.Tokens)
            {
                bw.Write(token);
            }

            bw.Write(this.Keywords.Count);
            foreach (var keyword in Keywords)
            {
                bw.Write(keyword.Key);
                bw.Write(keyword.Value);
            }

            bw.Write(this.Links.Count);
            foreach (var link in this.Links)
            {
                bw.Write(link);
            }

            return true;
        }

        public bool LoadFrom(BinaryReader br)
        {
            this.UniqueId = br.ReadInt32();
            this.Address = br.ReadString();

            this.ShingleHashes.Clear();
            var shingleCount = br.ReadInt32();
            for (var i = 0; i < shingleCount; i++) { this.ShingleHashes.AddLast(br.ReadUInt64()); }

            this.Tokens.Clear();
            var tokenCount = br.ReadInt32();
            for (var i = 0; i < tokenCount; i++) { this.Tokens.Add(br.ReadString()); }

            this.Keywords.Clear();
            var keywordCount = br.ReadInt32();
            for (var i = 0; i < keywordCount; i++)
            {
                var key = br.ReadString();
                var value = br.ReadInt32();
                this.Keywords.Add(key, value);
            }

            this.Links.Clear();
            var linkCount = br.ReadInt32();
            for (var i = 0; i < linkCount; i++) { this.Links.Add(br.ReadString()); }

            return true;
        }
    }
}
