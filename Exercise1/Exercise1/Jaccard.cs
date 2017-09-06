using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Exercise1
{
    class Jaccard
    {
        protected int[] permutations = { 4802, 13141, 14983, 30154, 32405, 38072, 45076, 49468, 66908, 67640, 91805, 92348, 101340, 117576, 136386, 158413, 177209, 188886, 191622, 198389, 210814, 212454, 229998, 237159, 241705, 249384, 259169, 268630, 288216, 300560, 308363, 310212, 313209, 347849, 349290, 351820, 364728, 395416, 406279, 415501, 420265, 435041, 438955, 439908, 445912, 447049, 456450, 456566, 487710, 522429, 533080, 541473, 601154, 627797, 628013, 629235, 635953, 638823, 652239, 666039, 672211, 675746, 713862, 719511, 724720, 761905, 771157, 775618, 784789, 795263, 818805, 821227, 835269, 877328, 881412, 881813, 908477, 921111, 930163, 951340, 974627, 984774, 987052, 995589 };
        protected List<string> docOneShingles;
        protected List<string> docTwoShingles;

        public decimal CompareDocuments(string doc1, string doc2, int shingleSize = 4)
        {
            docOneShingles = ShinglifyDocument(doc1, shingleSize);
            docTwoShingles = ShinglifyDocument(doc2, shingleSize);

            var overlap = docOneShingles.Intersect(docTwoShingles).Count();
            var union = docOneShingles.Concat(docTwoShingles).Distinct().Count();

            if (union == 0) return 0M;

            return (decimal) overlap / union;
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

        protected List<int> HashedShinglifyDocument(string document, int n = 4)
        {
            var returnList = new List<int>();
            var tempList = new List<int>();
        }
    }
}
