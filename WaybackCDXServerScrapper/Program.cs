using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WaybackCDXServerScrapper
{
    class Program
    {


        static async Task Main(string[] args)
        {
            string dateText = "20151015";
            DateTime dateTime = DateTime.ParseExact(dateText, "yyyyMMdd", null);

            Console.WriteLine(dateTime.ToLocalTime());

            Console.ReadLine();
        }

        //        static async Task Main(string[] args)
        //        {
        //#if DEBUG
        //            args = new string[] { @"amazon.com/Amazon-Prime-Air/b?ie=UTF8&node=8037720011", "prefix" };
        //#endif
        //            if (args.Length != 2 && args.Length != 3)
        //            {
        //                Console.WriteLine("WaybackCDXServerScrapper \"domain-name\" \"matchType\" \"{Optional}total-number-of-concurrent-downloads{default is 1}\" Expected.");

        //                Console.WriteLine("*************matchType**************");
        //                Console.WriteLine("if given the URL: archive.org/about/ and:\n");
        //                Console.WriteLine("matchType=exact will return results matching exactly archive.org/about/\n");
        //                Console.WriteLine("matchType=prefix will return results for all results under the path archive.org/about/\n");
        //                Console.WriteLine("matchType=host will return results from host archive.org\n");
        //                Console.WriteLine("matchType=domain will return results from host archive.org and all subhosts *.archive.org");
        //                Console.WriteLine("************************************");
        //                return;
        //            }

        //            string domainName = args[0];
        //            string matchType = args[1];

        //            if (!matchType.Equals("exact") && !matchType.Equals("prefix") && !matchType.Equals("host") && !matchType.Equals("domain"))
        //            {
        //                Console.WriteLine("Only \"exact\", \"prefix\", \"host\" & \"domain\" allowed as matchType");
        //                return;
        //            }

        //            CdxScrapper scrapper = new CdxScrapper(matchType);
        //            try
        //            {
        //                int.TryParse(args[2], out int concurrentTasksCount);
        //                scrapper.ConcurrentTasksCount = concurrentTasksCount > 0 ? concurrentTasksCount : 1;
        //            }
        //            catch { }

        //            string outputFile = string.Empty;
        //            try
        //            {
        //                outputFile = GetOutputFileName(domainName);
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine(ExceptionHelper.ExtractExceptionMessage(ex));
        //                return;
        //            }

        //            var numberOfPages = await scrapper.GetTotalPagesCount(domainName, outputFile);
        //            if (numberOfPages > 0)
        //            {
        //                await scrapper.ScrapeAllPages(numberOfPages, domainName, outputFile);
        //            }

        //        }

        /// <returns>A CSV output file path</returns>
        public static string GetOutputFileName(string domainName)
        {
            if (!domainName.StartsWith("http", StringComparison.OrdinalIgnoreCase) &&
                    !domainName.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                domainName = $"http://{domainName}";
            }

            Uri myUri = new Uri(domainName);
            string host = myUri.Host;

            char[] invalidFileNameChars = Path.GetInvalidFileNameChars();

            // Builds a string out of valid chars and an _ for invalid ones
            string validFileName = new string(host.Select(ch => invalidFileNameChars.Contains(ch) ? '_' : ch).ToArray());

            return Path.Combine(Directory.GetCurrentDirectory(), $"{validFileName} {DateTime.Now:yyyy-d-MM-HHmmss}.csv");
        }

    }
}
