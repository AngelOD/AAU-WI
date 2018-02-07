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
            const int k = 5;

            var networks = new List<Network>();

            Console.WriteLine("Press enter to start...");
            Console.ReadLine();

            Console.WriteLine("Loading file...");
            var network = new Network(@"D:\_Temp\__WI_TestData\friendships.txt");
            Console.WriteLine("Network has {0} entries.", network.NetworkNodes.Count);

            networks.Add(network);

            while (networks.Count < k)
            {
                Console.WriteLine("Finding biggest cluster...");

                Network n = null;

                foreach (var nw in networks)
                {
                    if (n == null || nw.NetworkNodes.Count > n.NetworkNodes.Count) { n = nw; }
                }

                if (n == null) { break; }

                networks.Remove(n);

                Console.WriteLine("Splitting network with {0} entries...", n.NetworkNodes.Count);
                Console.WriteLine("Performing spectral clustering split...");
                n.DoSpectralClusteringSplit(out var n1, out var n2);

                Console.WriteLine("Network 1 has {0} entries.", n1.NetworkNodes.Count);
                Console.WriteLine("Network 2 has {0} entries.", n2.NetworkNodes.Count);

                if (n1.NetworkNodes.Count * 100 < n2.NetworkNodes.Count ||
                    n2.NetworkNodes.Count * 100 < n1.NetworkNodes.Count)
                {
                    networks.Add(n);
                    break;
                }

                networks.Add(n1);
                networks.Add(n2);
            }

            Console.WriteLine("Found {0} communities out of the {1} that was desired.");

            /*
            double[,] ar =
            {
                {0,1,1,0,0,0,0,0,0},
                {1,0,1,0,0,0,0,0,0},
                {1,1,0,1,1,0,0,0,0},
                {0,0,1,0,1,1,1,0,0},
                {0,0,1,1,0,1,1,0,0},
                {0,0,0,1,1,0,1,1,0},
                {0,0,0,1,1,1,0,1,0},
                {0,0,0,0,0,1,1,0,1},
                {0,0,0,0,0,0,0,1,0}
            };
            var a = Matrix<double>.Build.DenseOfArray(ar);
            var d = Matrix<double>.Build.Diagonal(new double[]
                                                  {
                                                      2,
                                                      2,
                                                      4,
                                                      4,
                                                      4,
                                                      4,
                                                      4,
                                                      3,
                                                      1
                                                  });
            var m = d.Subtract(a);

            var se = m.Evd().EigenVectors.Column(1);
            var ti = 0;
            var tse =
                (from td in se
                select new KeyValuePair<int, double>(ti++, td))
                .ToList();
            var ose = tse.OrderBy(v => v.Value).ToArray();
            var max = 0.0;
            var maxI = -1;

            for (var i = 1; i < ose.Length; i++)
            {
                var diff = Math.Abs(ose[i - 1].Value - ose[i].Value);

                if (diff > max)
                {
                    maxI = i;
                    max = diff;
                }
            }

            var idxa = ose[maxI].Key;
            var idxb = ose[maxI - 1].Key;

            Console.WriteLine(m.Evd().EigenVectors.ToMatrixString());
            Console.WriteLine();
            Console.WriteLine("Largest difference between {2}:{0} and {3}:{1}", se[idxb], se[idxa], idxb, idxa);

            var ika = a.Row(idxa);
            var ikb = a.Row(idxb);

            for (var i = 0; i < ika.Count; i++)
            {
                if (ika[i] > 0 && ikb[i] > 0)
                {
                    Console.WriteLine("Found shared node {0}", i);

                    var diffa = Math.Abs(se[idxa] - se[i]);
                    var diffb = Math.Abs(se[idxb] - se[i]);

                    Console.WriteLine("Difference to {0} is {1}", idxa, diffa);
                    Console.WriteLine("Difference to {0} is {1}", idxb, diffb);
                    Console.WriteLine("Thus {0} belongs with {1}", i, (diffa < diffb ? idxa : idxb));
                }
            }
            */

            Console.ReadLine();
        }
    }
}
