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
            Console.WriteLine("Press enter to start crawling...");
            Console.ReadLine();

            var crawler = new Modules.Crawler();

            crawler.Crawl(new List<string>(){ @"http://www.freeos.com/guides/lsst/" });
            crawler.PrintInfo();

            var test = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            Console.WriteLine();
            Console.WriteLine("Path: {0}", test);

            Console.ReadLine();
        }
    }
}
