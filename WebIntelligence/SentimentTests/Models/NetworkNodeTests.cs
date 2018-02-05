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
    public class NetworkNodeTests
    {
        [TestMethod()]
        public void NetworkNodeTest()
        {
            var nn = new NetworkNode("abagael", @"	joey	gretel	beitris	nani	junia	");

            Assert.AreEqual("abagael", nn.Name);
            Assert.AreEqual(5, nn.Friends.Count);
            Assert.IsTrue(nn.Friends.Contains("joey"));
            Assert.IsTrue(nn.Friends.Contains("gretel"));
            Assert.IsTrue(nn.Friends.Contains("beitris"));
            Assert.IsTrue(nn.Friends.Contains("nani"));
            Assert.IsTrue(nn.Friends.Contains("junia"));
            Assert.IsFalse(nn.Friends.Contains("Joey"));
        }
    }
}