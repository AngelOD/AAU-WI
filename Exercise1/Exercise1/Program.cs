using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crawler
{
    class Program
    {
        static void Main(string[] args)
        {
            var string1 = "Do not worry about your difficulties in mathematics";
            var string2 = "I would not worry about your difficulties, you can easily learn what is needed.";

            var jaccard = new Jaccard();

            var result = jaccard.CompareDocuments(string1, string2, 3);
            var result2 = jaccard.CompareDocumentsTrickOne(string1, string2, 3);

            Console.WriteLine("Result: {0}", result);
            Console.WriteLine("Result (Trick 1): {0}", result2);
        }
    }
}
