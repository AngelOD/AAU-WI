using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Crawler
{
    class Jaccard
    {
        protected ulong[] permutations = { 0, 4802, 13141, 14983, 30154, 32405, 38072, 45076, 49468, 66908, 67640, 91805, 92348, 101340, 117576, 136386, 158413, 177209, 188886, 191622, 198389, 210814, 212454, 229998, 237159, 241705, 249384, 259169, 268630, 288216, 300560, 308363, 310212, 313209, 347849, 349290, 351820, 364728, 395416, 406279, 415501, 420265, 435041, 438955, 439908, 445912, 447049, 456450, 456566, 487710, 522429, 533080, 541473, 601154, 627797, 628013, 629235, 635953, 638823, 652239, 666039, 672211, 675746, 713862, 719511, 724720, 761905, 771157, 775618, 784789, 795263, 818805, 821227, 835269, 877328, 881412, 881813, 908477, 921111, 930163, 951340, 974627, 984774, 987052, 995589 };
        protected List<string> docOneShingles;
        protected List<string> docTwoShingles;

        public decimal CompareDocuments(string doc1, string doc2, int shingleSize = 4)
        {
            docOneShingles = ShinglifyDocument(doc1, shingleSize);
            docTwoShingles = ShinglifyDocument(doc2, shingleSize);

            var overlap = docOneShingles.Intersect(docTwoShingles).Count();
            var union = docOneShingles.Concat(docTwoShingles).Distinct().Count();

            if (union == 0) return 0M;

            return (decimal)overlap / union;
        }

        public decimal CompareDocumentsTrickOne(string doc1, string doc2, int shingleSize = 4)
        {
            var docOneHashes = HashedShinglifyDocument(doc1, shingleSize);
            var docTwoHashes = HashedShinglifyDocument(doc2, shingleSize);

            int overlap = 0;
            int permCount = permutations.Length;

            for (int i = 0; i < permCount; i++)
            {
                if (docOneHashes[i] == docTwoHashes[i])
                {
                    overlap++;
                }
            }

            return (decimal)overlap / permutations.Length;
        }

        protected List<string> ShinglifyDocument(string document, int n = 4)
        {
            var list = new List<string>();
            var newDoc = Regex.Replace(document.ToLower(), "[^a-z0-9 ]", "");
            var words = newDoc.ToLower().Split(' ');
            var shingleCount = words.Length - n;

            for (var i = 0; i <= shingleCount; i++)
            {
                list.Add(String.Join(" ", words, i, n));
            }

            return list;
        }

        protected ulong[] HashedShinglifyDocument(string document, int n = 4)
        {
            var returnList = new LinkedList<ulong>();
            var shingles = ShinglifyDocument(document, n);
            ulong[] baseHashes = (from shingle in shingles
                                select (ulong)shingle.GetHashCode()).ToArray();
            var permCount = permutations.Length;

            for (int i = 0; i < permCount; i++)
            {
                ulong permutation = permutations[i];
                ulong minHash = ulong.MaxValue;

                foreach (var baseHash in baseHashes)
                {
                    ulong tmpHash = baseHash;

                    if (permutation > 0)
                    {
                        tmpHash ^= permutation;
                    }

                    if (tmpHash < minHash)
                    {
                        minHash = tmpHash;
                    }
                }

                returnList.AddLast(minHash);
            }

            return returnList.ToArray();
        }
    }
}
