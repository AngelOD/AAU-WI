#region Using Directives
using System;
using System.IO;
using System.Text;
#endregion

namespace Sentiment.Helpers
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
                                        "Sentiment");

                if (!Directory.Exists(_path)) { Directory.CreateDirectory(_path); }

                return _path;
            }
        }

        public static string GetUrlBase(string url)
        {
            var uri = new Uri(url);

            return uri.GetLeftPart(UriPartial.Authority);
        }

        public static BinaryWriter GetWriterForFile(string fileName)
        {
            var file = File.Create(Path.Combine(Utilities.UserAppDataPath, fileName));
            var bw = new BinaryWriter(file);

            return bw;
        }

        public static BinaryReader GetReaderForFile(string fileName)
        {
            try
            {
                var file = File.OpenRead(Path.Combine(Utilities.UserAppDataPath, fileName));
                var br = new BinaryReader(file);

                return br;
            }
            catch (FileNotFoundException) {}

            return null;
        }

        public static bool CheckUtf8(string data)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(data);
            var encodedWord = Convert.ToBase64String(plainTextBytes);
            var wordBytes = Convert.FromBase64String(encodedWord);
            var decodedWord = Encoding.UTF8.GetString(wordBytes);

            return decodedWord.Equals(data);
        }
    }
}
