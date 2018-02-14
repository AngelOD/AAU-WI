using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using Sentiment.Models;

namespace Sentiment
{
    class Program
    {
        static void Main(string[] args)
        {
            var networks = new List<Network>();
            var newNetworks = new Queue<Network>();

            Console.WriteLine("Press enter to start...");
            Console.ReadLine();

            Console.WriteLine("Loading file...");
            var network = new Network(@"D:\_Temp\__WI_TestData\friendships.txt");
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

            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
        }
    }
}
