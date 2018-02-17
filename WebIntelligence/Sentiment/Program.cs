using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Win32;
using Sentiment.Helpers;
using Sentiment.Models;

namespace Sentiment
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Press enter to start classifier...");
            Console.ReadLine();

            var c = new Classifier();

            for (var i = 1; i < 6; i++)
            {
                Console.WriteLine("{0}.0: {1:F4}\t{2}", i, c.GetSentimentProbability(i), c.GetEmptyScoreForSentiment(i));
            }

            c.RunOnTestingData();

            var networks = new List<Network>();
            var newNetworks = new Queue<Network>();

            Console.WriteLine("Press enter to start analysis...");
            Console.ReadLine();

            Console.WriteLine("Loading file...");
            var network = new Network(@"D:\_Temp\__WI_TestData\friendships.reviews.txt");
            Console.WriteLine("Network has {0} entries.", network.NetworkNodes.Count);

            newNetworks.Enqueue(network);

            while (newNetworks.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Trying to split next network...");

                var n = newNetworks.Dequeue();

                Console.WriteLine("Splitting network with {0} entries...", n.NetworkNodes.Count);
                Console.WriteLine("Performing spectral clustering split...");
                n.DoSpectralClusteringSplit(out var n1, out var n2);

                var n1Count = n1.NetworkNodes.Count;
                var n2Count = n2.NetworkNodes.Count;
                var ratio = Math.Min((double)n1Count, n2Count) / Math.Max(n1Count, n2Count);
                Console.WriteLine("Networks have {0} and {1} entries. (Ratio: {2})", n1Count, n2Count, ratio);

                if (ratio <= 0.1)
                {
                    Console.WriteLine("Split rejected. Adding original network to finals.");
                    networks.Add(n);
                }
                else
                {
                    Console.WriteLine("Split accepted. Adding new networks to queue.");
                    newNetworks.Enqueue(n1);
                    newNetworks.Enqueue(n2);
                }
            }

            Console.WriteLine("Found {0} communities.", networks.Count);
            Console.WriteLine();
            Console.WriteLine("Running classifier on them...");

            foreach (var nw in networks)
            {
                nw.ClassifyNetwork(c);
            }

            Console.WriteLine("Done!");
            Console.WriteLine();

            Console.WriteLine("Press enter to calculate purchase predictions...");
            Console.ReadLine();

            CalculateAverageFriendScoresForAll(c, network, networks);

            Console.WriteLine("Done!");

            Console.WriteLine();
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
        }

        private static void CalculateAverageFriendScoresForAll(Classifier classifier, Network globalNetwork,
                                                               IReadOnlyList<Network> networks)
        {
            var resultFile = Path.Combine(Utilities.UserAppDataPath, "results.txt");
            var sw = new StreamWriter(resultFile) { AutoFlush = true };

            foreach (var user in globalNetwork.NetworkNodes)
            {
                var userNetwork = -1;

                for (var i = 0; i < networks.Count; i++)
                {
                    if (!networks[i].NetworkNodes.ContainsKey(user.Key)) continue;
                    userNetwork = i;
                    break;
                }

                if (userNetwork == -1) { continue; }

                var purchase = false;
                var score = 0;

                if (user.Value.HasReview) { score = classifier.Classify(user.Value.Review); }
                else
                {
                    var avgScore =
                        (int) Math.Round(CalculateAverageFriendScoresForUser(classifier, globalNetwork,
                                                                             networks[userNetwork], user.Value));

                    if (avgScore > 3) { purchase = true; }
                }

                OutputResult(sw, user.Value, userNetwork, score, purchase);
            }

            sw.Close();
        }

        private static void OutputResult(TextWriter sw, NetworkNode user, int cluster, int score, bool purchase)
        {
            sw.WriteLine("user: {0}", user.Name);
            sw.WriteLine("cluster: {0}", cluster);
            sw.WriteLine("score: {0}", score);
            sw.WriteLine("purchase: {0}", purchase ? "yes" : "no");
            sw.Write("friends:");

            foreach (var friend in user.Friends)
            {
                sw.Write("\t{0}", friend);
            }

            sw.WriteLine();
            sw.WriteLine("summary: {0}", user.Summary);
            sw.WriteLine("review: {0}", user.Review);
            sw.WriteLine();
        }

        private static double CalculateAverageFriendScoresForUser(Classifier classifier, Network globalNetwork,
                                                                  Network userNetwork, NetworkNode user)
        {
            var totalScore = 0;
            var reviewCount = 0;

            foreach (var friend in user.Friends)
            {
                if (!globalNetwork.NetworkNodes.ContainsKey(friend)) { continue; }

                var friendNode = globalNetwork.NetworkNodes[friend];
                var score = friendNode.ClassifyReview(classifier);

                if (score == 0) { continue; }

                var multiplier = 1;

                if (!userNetwork.NetworkNodes.ContainsKey(friend)) { multiplier = 10; }

                if (friend.Equals("kyle")) { multiplier *= 10; }

                totalScore += score * multiplier;
                reviewCount += multiplier;
            }

            return (double) totalScore / reviewCount;
        }
    }
}
