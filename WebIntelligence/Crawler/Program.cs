using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Crawler.Modules;

namespace Crawler
{
    internal class Program
    {
        private static void Main(string[] args)
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

            //var parser = new RobotsParser(@"..\..\..\_TestFiles\Robots\kaffeteriet.txt");
            var webClient = new WebClient();
            webClient.Headers.Set(HttpRequestHeader.UserAgent, "BlazingskiesCrawler/0.1");

            var stream = webClient.OpenRead(@"http://www.freeos.com/robots.txt");
            var parser = new RobotsParser(stream);

            stream?.Close();

            var crawler = new Modules.Crawler();
            var page = crawler.ParsePage(new Uri(@"http://www.freeos.com/guides/lsst/"));

            crawler.SetSeedUris(new List<string>(){ "http://www.freeos.com/guides/lsst/" });

            var tokens = page.Tokens;

            foreach (var token in tokens)
            {
                Console.Write("{0}, ", token);
            }
            Console.WriteLine();
            Console.WriteLine();

            var keywords = page.Keywords;

            foreach (var keyword in keywords.OrderByDescending(i => i.Value))
            {
                Console.Write("{0} ({1}), ", keyword.Key, keyword.Value);
            }
            Console.WriteLine();
            Console.WriteLine();

            Console.ReadLine();
        }
    }
}
