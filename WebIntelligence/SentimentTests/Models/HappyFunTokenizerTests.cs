using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sentiment.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sentiment.Models.Tests
{
    [TestClass()]
    public class HappyFunTokenizerTests
    {
        [TestMethod()]
        public void TokenizeTest()
        {
            var hft = new HappyFunTokenizer(false);
        }
    }
}