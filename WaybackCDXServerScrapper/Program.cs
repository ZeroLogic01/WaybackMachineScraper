using System;
using System.IO;
using System.Threading.Tasks;

namespace WaybackCDXServerScrapper
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if DEBUG
            args = new string[] { @"http://www.katespade.com", @"F:\katespace.csv" };
#endif
            if (args.Length != 2)
            {
                Console.WriteLine("WaybackCDXServerScrapper \"domain-name\" \"csv-output-file-name\" Expected.");
                return;
            }

            string domainName = args[0];
            string outputFile = args[1];

            if (!Path.GetExtension(outputFile).Equals(".csv"))
            {
                Console.WriteLine($"A CSV output file name is required.");
                return;
            }

            CdxScrapper scrapper = new CdxScrapper();
            await scrapper.StartScraping(domainName);
        }


    }
}
