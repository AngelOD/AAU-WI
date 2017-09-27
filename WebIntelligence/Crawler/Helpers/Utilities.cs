#region Using Directives
using System;
using System.IO;
#endregion

namespace Crawler.Helpers
{
    public class Utilities
    {
        private static string _path;

        public static string UserAppDataPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_path)) return _path;

                _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                        "Blazingskies",
                                        "Crawler");

                if (!Directory.Exists(_path)) { Directory.CreateDirectory(_path); }

                return _path;
            }
        }

        public static string GetUrlBase(string url)
        {
            var uri = new Uri(url);

            return uri.GetLeftPart(UriPartial.Authority);
        }
    }
}
