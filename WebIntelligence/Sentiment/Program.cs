using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using Sentiment.Models;

namespace Sentiment
{
    class Program
    {
        static void Main(string[] args)
        {
            var hft = new HappyFunTokenizer();
            var words =
                hft.Tokenize(
                             @"RECONSIDER THIS FORMULA!!<br /><br />Martek advertises this formula on their website!<br /><br />""Although Martek told the board that they would discontinue the use of the controversial neurotoxic solvent n-hexane for DHA/ARA processing, they did not disclose what other synthetic solvents would be substituted. Federal organic standards prohibit the use of all synthetic/petrochemical solvents"".<br /><br />Martek Biosciences was able to dodge the ban on hexane-extraction by claiming USDA does not consider omega-3 and omega-6 fats to be ""agricultural ingredients."" Therefore, they argue, the ban against hexane extraction does not apply. The USDA helped them out by classifying those oils as ""necessary vitamins and minerals,"" which are exempt from the hexane ban. But hexane-extraction is just the tip of the iceberg. Other questionable manufacturing practices and misleading statements by Martek included:<br /><br />Undisclosed synthetic ingredients, prohibited for use in organics (including the sugar alcohol mannitol, modified starch, glucose syrup solids, and ""other"" undisclosed ingredients)<br />Microencapsulation of the powder and nanotechnology, which are prohibited under organic laws<br />Use of volatile synthetic solvents, besides hexane (such as isopropyl alcohol)<br />Recombinant DNA techniques and other forms of genetic modification of organisms; mutagenesis; use of GMO corn as a fermentation medium<br />Heavily processed ingredients that are far from ""natural""<br /><br />quote from: Why is this Organic Food Stuffed With Toxic Solvents? by Dr. Mercola - GOOGLE GMOs found in Martek.<br /><br />This is the latest I have found on DHA in organic and non organic baby food/ formula:<br />AT LEAST READ THIS ONE*** GOOGLE- False Claims That DHA in Organic and Non-Organic Infant Formula Is Safe. AND OrganicconsumersDOTorg<br /><br />Martek's patents for Life'sDHA states: ""includes mutant organisms"" and ""recombinant organisms"", (a.k.a. GMOs!) The patents explain that the oil is ""extracted readily with an effective amount of solvent ... a preferred solvent is hexane.""<br /><br />The patent for Life'sARA states: ""genetically-engineering microorganisms to produce increased amounts of arachidonic acid"" and ""extraction with solvents such as ... hexane."" Martek has many other patents for DHA and ARA. All of them include GMOs. GMOs and volatile synthetic solvents like hexane aren't allowed in USDA Organic products and ingredients.<br /><br />Tragically, Martek's Life'sDHA is already in hundreds of products, many of them certified USDA Organic. Please demand that the National Organic Standards Board reject Martek's petition, and that the USDA National Organic Program inform the company that the illegal 2006 approval is rescinded and that their GMO, hexane-extracted Life'sDHA and Life'sARA are no longer allowed in organic products.<br /><br />BUT I went to the lifesdha website and THEY DO NOT DISCLOSE HOW THEY MAKE THEIR LifesDHA!!! I have contacted the company to see what they say.<br /><br />Also these are the corporate practices of Martek which are damaging to the environment as well written just last Dec 2011 at NaturalnewsDOTcom<br /><br />The best bet is to just avoid the lifeDHA at this time in my opinion b/c corporate america cares more about the almighty $ than your health.");
            words.ForEach(Console.WriteLine);
            Console.ReadLine();

            var networks = new List<Network>();
            var newNetworks = new Queue<Network>();

            Console.WriteLine("Press enter to start...");
            Console.ReadLine();

            Console.WriteLine("Loading file...");
            var network = new Network(@"D:\_Temp\__WI_TestData\friendships.txt");
            Console.WriteLine("Network has {0} entries.", network.NetworkNodes.Count);

            newNetworks.Enqueue(network);

            while (newNetworks.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Trying to split next network...");

                var n = newNetworks.Dequeue();

                Console.WriteLine("Splitting network with {0} entries...", n.NetworkNodes.Count);
                Console.WriteLine("Performing spectral clustering split...");
                n.DoSpectralClusteringSplit(out var n1, out var n2);

                var n1Count = n1.NetworkNodes.Count;
                var n2Count = n2.NetworkNodes.Count;
                var ratio = Math.Min((double)n1Count, n2Count) / Math.Max(n1Count, n2Count);
                Console.WriteLine("Networks have {0} and {1} entries. (Ratio: {2})", n1Count, n2Count, ratio);

                if (ratio <= 0.1)
                {
                    Console.WriteLine("Split rejected. Adding original network to finals.");
                    networks.Add(n);
                }
                else
                {
                    Console.WriteLine("Split accepted. Adding new networks to queue.");
                    newNetworks.Enqueue(n1);
                    newNetworks.Enqueue(n2);
                }
            }

            Console.WriteLine("Found {0} communities.", networks.Count);

            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
        }
    }
}
