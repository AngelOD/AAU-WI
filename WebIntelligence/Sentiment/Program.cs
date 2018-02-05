using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;

namespace Sentiment
{
    class Program
    {
        static void Main(string[] args)
        {
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

            Console.ReadLine();
        }
    }
}
