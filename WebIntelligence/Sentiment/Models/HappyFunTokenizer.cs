using System.Runtime.InteropServices;

namespace Sentiment.Models
{
    public class HappyFunTokenizer
    {
        public HappyFunTokenizer(bool preserveCase = false)
        {
            var phone = "(?:(?:\\+?[01](?:[-.]|\\s)*)?(?:\\(?\\d{3}(?:[-.)]|\\s)*)?\\d" +
                        "{3}(?:[-.]|\\s)*\\d{4})";
        }
    }
}
