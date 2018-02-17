using System;
using System.Collections.Generic;

namespace Sentiment.Models
{
    public class NetworkNode
    {
        public string Name { get; set; }
        public HashSet<string> Friends { get; protected set; }
        public string Summary { get; set; }
        public string Review { get; set; }
        public int InDegree { get; set; }
        public int OutDegree => this.Friends.Count;
        public int Degree => this.InDegree + this.OutDegree;
        public bool HasReview => !this.Review.Equals("*");

        public NetworkNode(NetworkNode node) : this(node.Name, node.Friends, node.Summary, node.Review)
        { }

        public NetworkNode(string name, string friends, string summary, string review) : this(name, friends.Trim().Split('\t'), summary, review)
        { }

        public NetworkNode(string name, IEnumerable<string> friends, string summary, string review)
        {
            this.InDegree = 0;
            this.Name = name;
            this.Friends = new HashSet<string>(friends);
            this.Summary = summary;
            this.Review = review;
        }

        public int ClassifyReview(Classifier classifier)
        {
            if (classifier == null || !this.HasReview) { return 0; }

            var score = classifier.Classify(this.Review);

            return score;
        }
    }
}
