#region Using Directives
using System;
using Crawler.Modules;
#endregion

namespace Crawler
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var menu = new ConsoleMenu();
            menu.Run();
        }
    }
}
