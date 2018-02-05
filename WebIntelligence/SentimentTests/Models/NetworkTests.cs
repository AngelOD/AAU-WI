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
    public class NetworkTests
    {
        [TestMethod()]
        public void NetworkTest()
        {
            var nr = new Network(@"D:\_Temp\__WI_TestData\friendships.txt");

            Assert.AreEqual(4219, nr.NetworkNodes.Count);
            Assert.IsTrue(nr.NetworkNodes.ContainsKey("abagael"));
            Assert.AreEqual(38, nr.NetworkNodes["abagael"].Friends.Count);
            Assert.AreEqual(38, nr.NetworkNodes["abagael"].InDegree);
        }
    }
}