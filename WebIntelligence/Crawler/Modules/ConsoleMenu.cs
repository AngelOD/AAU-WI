﻿using System;
using System.Runtime.InteropServices;

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
            Console.WriteLine("(L) Load crawler data");
            Console.WriteLine("(R) Print crawler info");
            Console.WriteLine("(Z) Set default seed");
            Console.WriteLine("(C) Crawl (Count = {0})", this._pageCrawlCount);
            Console.WriteLine("(I) Increase crawl count by 100");
            Console.WriteLine("(D) Decrease crawl count by 25");
            Console.WriteLine();
            Console.WriteLine("(Q) Quit");
        }

        private bool HandleMenu()
        {
            var ch = Console.ReadKey(true);

            Console.Clear();

            switch (ch.Key) {
                case ConsoleKey.C:
                    Console.WriteLine("Crawling...");
                    this._crawler.CrawlCount = this._pageCrawlCount;
                    this._crawler.Crawl();
                    break;
                case ConsoleKey.D:
                    this._pageCrawlCount -= 25;
                    if (this._pageCrawlCount < 5) { this._pageCrawlCount = 5; }
                    break;
                case ConsoleKey.I:
                    this._pageCrawlCount += 100;
                    if (this._pageCrawlCount > 1000) { this._pageCrawlCount = 1000; }
                    break;
                case ConsoleKey.L:
                    Console.WriteLine("Loading data...");
                    return !this._crawler.LoadCrawlerData();
                case ConsoleKey.Q:
                    return true;
                case ConsoleKey.R:
                    this._crawler.PrintInfo();
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
