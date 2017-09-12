using System;
using Crawler.Modules;
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
            /*
            var string1 = "Do not worry about your difficulties in mathematics";
            var string2 = "I would not worry about your difficulties, you can easily learn what is needed.";

            var jaccard = new Jaccard();

            var result = jaccard.CompareDocuments(string1, string2, 3);
            var result2 = jaccard.CompareDocumentsTrickOne(string1, string2, 3);

            Console.WriteLine("Result: {0}", result);
            Console.WriteLine("Result (Trick 1): {0}", result2);
            */

            var parser = new RobotsParser(@"D:\___Projects\_UniStuff\WI\WebIntelligence\_TestFiles\Robots\kaffeteriet.txt");

            string[] urlsToTest = {
                @"https://kaffeteriet.dk/collections/filterkaffe-bonner",
                @"https://kaffeteriet.dk/blogs/news",
                @"https://kaffeteriet.dk/collections/kaffe-og-kaffeudstyr-pa-tilbud",
                @"https://kaffeteriet.dk/collections/iskaffebryggere",
                @"https://kaffeteriet.dk/cart",
                @"https://kaffeteriet.dk/collections/iskaffebryggere+woot",
                @"https://kaffeteriet.dk/collections/iskaffebryggere%2Bwoot",
                @"https://kaffeteriet.dk/collections/iskaffebryggere%2bwoot",
            };

            foreach (var url in urlsToTest)
            {
                Console.WriteLine("Testing: {0} - {1}", url, (parser.IsAllowed(url) ? "YES" : "NO"));
            }

            Console.ReadLine();
        }
    }
}
