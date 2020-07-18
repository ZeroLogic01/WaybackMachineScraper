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
#if DEBUG
            args = new string[] { @"katespade.com/handbags/" };
#endif
            if (args.Length != 1 && args.Length != 2)
            {
                Console.WriteLine("WaybackCDXServerScrapper \"domain-name\" \"{Optional}total-number-of-concurrent-downloads{default is 1}\" Expected.");
                return;
            }

            string domainName = args[0];

            CdxScrapper scrapper = new CdxScrapper();
            try
            {
                int.TryParse(args[1], out int concurrentTasksCount);
                scrapper.ConcurrentTasksCount = concurrentTasksCount > 0 ? concurrentTasksCount : 1;
            }
            catch { }

            string outputFile = string.Empty;
            try
            {
                outputFile = GetOutputFileName(domainName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ExceptionHelper.ExtractExceptionMessage(ex));
                return;
            }

            var numberOfPages = await scrapper.GetTotalPagesCount(domainName, outputFile);
            if (numberOfPages > 0)
            {
                await scrapper.ScrapeAllPages(numberOfPages, domainName, outputFile);
            }

        }

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
