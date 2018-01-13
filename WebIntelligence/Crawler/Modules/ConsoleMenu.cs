using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Crawler.Models;

namespace Crawler.Modules
{
    public class ConsoleMenu
    {
        private readonly Crawler _crawler;
        private int _pageCrawlCount = 200;

        public ConsoleMenu()
        {
            this._crawler = new Crawler();
        }
        public void Run()
        {
            var finished = false;

            while (!finished)
            {
                this.WriteMenu();
                finished = this.HandleMenu();
            }
        }

        private void WriteMenu()
        {
            Console.WriteLine();
            Console.WriteLine("(L)   Load crawler data");
            Console.WriteLine("(I)   Print crawler info");
            Console.WriteLine("(G)   Generate and output index");
            Console.WriteLine("(S)   Set seed");
            Console.WriteLine("(Z)   Set default seed");
            Console.WriteLine("(C)   Crawl (Count = {0})", this._pageCrawlCount);
            Console.WriteLine("(+)   Increase crawl count by 100");
            Console.WriteLine("(-)   Decrease crawl count by 25");
            Console.WriteLine("(End) Set crawl count");
            Console.WriteLine("(V)   View page");
            Console.WriteLine("(B)   Boolean Search");
            Console.WriteLine("(R)   Regular Search");
            Console.WriteLine("(T)   Test element");
            Console.WriteLine();
            Console.WriteLine("(Q)   Quit");
        }

        private bool HandleMenu()
        {
            var ch = Console.ReadKey(true);

            Console.Clear();

            switch (ch.Key) {
                case ConsoleKey.Add:
                case ConsoleKey.OemPlus:
                    this._pageCrawlCount += 100;
                    if (this._pageCrawlCount > 10000) { this._pageCrawlCount = 10000; }
                    break;
                case ConsoleKey.OemMinus:
                case ConsoleKey.Subtract:
                    this._pageCrawlCount -= 25;
                    if (this._pageCrawlCount < 5) { this._pageCrawlCount = 5; }
                    break;
                case ConsoleKey.End:
                    Console.Write("Enter crawl count: ");

                    if (int.TryParse(Console.ReadLine(), out var crawlCount))
                    {
                        this._pageCrawlCount = crawlCount;
                        if (this._pageCrawlCount < 5) { this._pageCrawlCount = 5; }
                        if (this._pageCrawlCount > 10000) { this._pageCrawlCount = 10000; }
                    }

                    break;
                case ConsoleKey.B:
                    Console.Write("Enter boolean query: ");
                    var booleanQuery = Console.ReadLine();
                    this._crawler.ExecuteBooleanQuery(booleanQuery);
                    break;
                case ConsoleKey.C:
                    Console.WriteLine("Crawling...");
                    this._crawler.CrawlCount = this._pageCrawlCount;
                    this._crawler.Crawl();
                    break;
                case ConsoleKey.G:
                    this._crawler.OutputIndex();
                    Console.WriteLine();
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadLine();
                    break;
                case ConsoleKey.I:
                    this._crawler.PrintInfo();
                    Console.WriteLine();
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadLine();
                    this._crawler.PrintDomainInfo();
                    Console.WriteLine();
                    Console.WriteLine("Press enter to continue...");
                    Console.ReadLine();
                    Console.Clear();
                    break;
                case ConsoleKey.L:
                    Console.WriteLine("Loading data...");
                    return !this._crawler.LoadCrawlerData();
                case ConsoleKey.Q:
                    return true;
                case ConsoleKey.R:
                    Console.Write("Enter query: ");
                    var regularQuery = Console.ReadLine();
                    this._crawler.ExecuteRegularQuery(regularQuery);
                    break;
                case ConsoleKey.S:
                    Console.Write("Enter seed URL: ");
                    var newSeed = Console.ReadLine();
                    this._crawler.SetSeedUris(new []{ newSeed });
                    Console.WriteLine("Seed set!");
                    break;
                case ConsoleKey.T:
                    this._crawler.ExecuteBooleanQuery("general or impact and test not batteries");
                    break;
                case ConsoleKey.V:
                    Console.Write("Enter page number: ");

                    if (int.TryParse(Console.ReadLine(), out var pageNum))
                    {
                        this._crawler.OutputPage(pageNum);
                        Console.WriteLine();
                        Console.WriteLine("Press enter to continue...");
                        Console.ReadLine();
                        Console.Clear();
                    }

                    break;
                case ConsoleKey.Z:
                    Console.WriteLine("Seed set!");
                    //this._crawler.SetSeedUris(new []{ @"https://www.indigoag.com" });
                    this._crawler.SetSeedUris(new []{ @"https://farmsunday.org/" });
                    break;
            }

            return false;
        }
    }
}
