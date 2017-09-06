using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exercise1
{
    class HashedShingle
    {
        private string codeText;
        private int[] permutations = { 4802, 13141, 14983, 30154, 32405, 38072, 45076, 49468, 66908, 67640, 91805, 92348, 101340, 117576, 136386, 158413, 177209, 188886, 191622, 198389, 210814, 212454, 229998, 237159, 241705, 249384, 259169, 268630, 288216, 300560, 308363, 310212, 313209, 347849, 349290, 351820, 364728, 395416, 406279, 415501, 420265, 435041, 438955, 439908, 445912, 447049, 456450, 456566, 487710, 522429, 533080, 541473, 601154, 627797, 628013, 629235, 635953, 638823, 652239, 666039, 672211, 675746, 713862, 719511, 724720, 761905, 771157, 775618, 784789, 795263, 818805, 821227, 835269, 877328, 881412, 881813, 908477, 921111, 930163, 951340, 974627, 984774, 987052, 995589 };
        private bool isDirty;
        private int[] hashes;

        public HashedShingle(string codeText)
        {
            this.codeText = codeText;
            this.isDirty = true;
        }

        private void HashEntry()
        {
            int numPermutations = permutations.Length;
            var tmpList = new LinkedList<int>();

            for (int i = 0; i < numPermutations; i++)
            {

            }
        }
    }
}
