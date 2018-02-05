using System;
using System.Collections.Generic;

namespace Sentiment.Models
{
    public class NetworkNode
    {
        public string Name { get; set; }
        public HashSet<string> Friends { get; protected set; }
        public int InDegree { get; set; }
        public int OutDegree => this.Friends.Count;
        public int Degree => this.InDegree + this.OutDegree;

        public NetworkNode(string name, string friends) : this(name, friends.Trim().Split('\t'))
        { }

        public NetworkNode(string name, IEnumerable<string> friends)
        {
            this.InDegree = 0;
            this.Name = name;
            this.Friends = new HashSet<string>(friends);
        }
    }
}
