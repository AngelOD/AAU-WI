using System;
using System.Runtime.InteropServices;

namespace Crawler.Modules
{
    public class ConsoleMenu
    {
        private readonly Crawler _crawler;

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
            Console.WriteLine("(L) Load crawler data");
            Console.WriteLine("(R) Print crawler info");
            Console.WriteLine("(C) Crawl");
            Console.WriteLine("(Z) Set default seed");
            Console.WriteLine();
            Console.WriteLine("(Q) Quit");
        }

        private bool HandleMenu()
        {
            var ch = Console.ReadKey(true);

            switch (ch.Key) {
                case ConsoleKey.Q:
                    return true;
                case ConsoleKey.L:
                    Console.WriteLine("Loading data...");
                    return !this._crawler.LoadCrawlerData();
                case ConsoleKey.R:
                    this._crawler.PrintInfo();
                    break;
                case ConsoleKey.C:
                    Console.WriteLine("Crawling...");
                    this._crawler.Crawl();
                    break;
                case ConsoleKey.Z:
                    Console.WriteLine("Seed set!");
                    this._crawler.SetSeedUris(new []{ @"http://www.freeos.com/guides/lsst/" });
                    break;
            }

            return false;
        }
    }
}
