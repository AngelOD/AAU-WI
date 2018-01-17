using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Crawler.Models;

namespace Crawler.Modules
{
    public class ConsoleMenu
    {
        private enum MenuTypes
        {
            MainMenu, InfoMenu, SearchMenu, BlacklistMenu
        }

        private Dictionary<int, string> _localBlacklist;
        private MenuTypes _currentMenu = MenuTypes.MainMenu;
        private readonly Crawler _crawler;
        private int _pageCrawlCount = 200;
        private bool _dataLoaded = false;

        public ConsoleMenu()
        {
            this._crawler = new Crawler();
            this._localBlacklist = new Dictionary<int, string>();
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
            if (!this._dataLoaded)
            {
                Console.WriteLine();
                Console.WriteLine("No data loaded!");
            }

            Console.WriteLine();

            switch (this._currentMenu)
            {
                case MenuTypes.MainMenu:
                    Console.WriteLine("(L)   Load Crawler Data");
                    Console.WriteLine("(A)   Save Crawler Data");
                    Console.WriteLine("(S)   Set Seed");
                    Console.WriteLine("(Z)   Set Default Seed");
                    Console.WriteLine("(C)   Crawl (Count = {0})", this._pageCrawlCount);
                    Console.WriteLine("(+)   Increase Crawl Count by 100");
                    Console.WriteLine("(-)   Decrease Crawl Count by 25");
                    Console.WriteLine("(End) Set Crawl Count");
                    Console.WriteLine();
                    Console.WriteLine("(B)   Blacklist");
                    Console.WriteLine("(I)   Information");
                    Console.WriteLine("(F)   Search");
                    Console.WriteLine();
                    Console.WriteLine("(Q)   Quit");
                    break;
                case MenuTypes.InfoMenu:
                    Console.WriteLine("(I)   Print Crawler Info");
                    Console.WriteLine("(V)   View Page");
                    Console.WriteLine("(G)   Generate and Output Index");
                    Console.WriteLine("(T)   Test Element");
                    Console.WriteLine();
                    Console.WriteLine("(Q)   Go Back");
                    break;
                case MenuTypes.SearchMenu:
                    Console.WriteLine("(B)   Boolean Search");
                    Console.WriteLine("(R)   Regular Search");
                    Console.WriteLine();
                    Console.WriteLine("(Q)   Go Back");
                    break;
                case MenuTypes.BlacklistMenu:
                    var i = 0;
                    this._localBlacklist.Clear();

                    foreach (var entry in this._crawler.Blacklist)
                    {
                        i++;
                        this._localBlacklist[i] = entry;
                        Console.WriteLine("({0}) {1}", i, entry);
                    }

                    Console.WriteLine();
                    Console.WriteLine("(A)   Add New Entry");
                    Console.WriteLine("(D)   Delete Entry");
                    Console.WriteLine();
                    Console.WriteLine("(Q)   Go Back");
                    break;
            }
        }

        private bool HandleMenu()
        {
            var ch = Console.ReadKey(true);

            Console.Clear();
            switch (this._currentMenu)
            {
                case MenuTypes.MainMenu:
                    switch (ch.Key)
                    {
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
                        case ConsoleKey.A:
                            this._crawler.SaveCrawlerData();
                            break;
                        case ConsoleKey.B:
                            this._currentMenu = MenuTypes.BlacklistMenu;
                            break;
                        case ConsoleKey.C:
                            Console.WriteLine("Crawling...");
                            this._crawler.CrawlCount = this._pageCrawlCount;
                            this._crawler.Crawl();
                            this._dataLoaded = true;
                            break;
                        case ConsoleKey.F:
                            this._currentMenu = MenuTypes.SearchMenu;
                            break;
                        case ConsoleKey.I:
                            this._currentMenu = MenuTypes.InfoMenu;
                            break;
                        case ConsoleKey.L:
                            Console.WriteLine("Loading data...");
                            this._dataLoaded = true;
                            return !this._crawler.LoadCrawlerData();
                        case ConsoleKey.Q:
                            return true;
                        case ConsoleKey.S:
                            Console.Write("Enter seed URL: ");
                            var newSeed = Console.ReadLine();
                            this._crawler.SetSeedUris(new[]
                                                      {
                                                          newSeed
                                                      });
                            Console.WriteLine("Seed set!");
                            break;
                        case ConsoleKey.Z:
                            Console.WriteLine("Seed set!");
                            //this._crawler.SetSeedUris(new []{ @"https://www.indigoag.com" });
                            this._crawler.SetSeedUris(new[]
                                                      {
                                                          @"https://farmsunday.org/"
                                                      });
                            break;
                    }
                    break;

                case MenuTypes.InfoMenu:
                    switch (ch.Key)
                    {
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
                        case ConsoleKey.Q:
                            this._currentMenu = MenuTypes.MainMenu;
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
                    }
                    break;

                case MenuTypes.SearchMenu:
                    switch (ch.Key)
                    {
                        case ConsoleKey.B:
                            Console.Write("Enter boolean query: ");
                            var booleanQuery = Console.ReadLine();
                            this._crawler.ExecuteBooleanQuery(booleanQuery);
                            break;
                        case ConsoleKey.Q:
                            this._currentMenu = MenuTypes.MainMenu;
                            break;
                        case ConsoleKey.R:
                            Console.Write("Enter query: ");
                            var regularQuery = Console.ReadLine();
                            this._crawler.ExecuteRegularQuery(regularQuery);
                            break;
                    }
                    break;

                case MenuTypes.BlacklistMenu:
                    switch (ch.Key)
                    {
                        case ConsoleKey.A:
                            Console.Write("Enter string to blacklist (eg. twitter.com): ");
                            var blacklistString = Console.ReadLine();
                            if (blacklistString != null && blacklistString.Length > 3) { this._crawler.Blacklist.Add(blacklistString); }
                            break;
                        case ConsoleKey.D:
                            Console.Write("Enter number of entry to delete: ");

                            if (int.TryParse(Console.ReadLine(), out var entryIndex))
                            {
                                if (this._localBlacklist.ContainsKey(entryIndex))
                                {
                                    this._crawler.Blacklist.Remove(this._localBlacklist[entryIndex]);
                                }
                            }
                            break;
                        case ConsoleKey.Q:
                            this._currentMenu = MenuTypes.MainMenu;
                            break;
                    }
                    break;
            }

            return false;
        }
    }
}
