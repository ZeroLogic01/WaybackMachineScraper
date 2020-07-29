using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WaybackCDXServerScrapper
{
    class Program
    {


        //static async Task Main(string[] args)
        //{
        //    string dateText = "20151015";
        //    DateTime dateTime = DateTime.ParseExact(dateText, "yyyyMMdd", null);

        //    Console.WriteLine(dateTime.ToLocalTime());

        //    Console.ReadLine();
        //}

        class Options
        {
            [Option('u', "url", HelpText = "A web URL. It can be a domain/sub-domain or a specific URL.", Required = true)]
            public string WebUrl { get; set; }

            [Option('m', "match-type", HelpText = "Match type filter. " +
               "See more https://github.com/internetarchive/wayback/blob/master/wayback-cdx-server/README.md#url-match-scope", Default = MatchTypeFilter.domain, Required = false)]
            public MatchTypeFilter MatchType { get; set; }

            [Option('c', "concurrent-downloads", HelpText = "Total number of concurrent downloads.", Default = 1)]
            public int ConcurrentDownloadsCount { get; set; }

            [Option('f', "from", HelpText = "The \"from\" range is inclusive and is specified in the same 1 to 14 digit format used for wayback captures: yyyyMMddhhmmss", Required = false)]
            public string From { get; set; }

            [Option('t', "to", HelpText = "The \"To\" range is inclusive and is specified in the same 1 to 14 digit format used for wayback captures: yyyyMMddhhmmss", Required = false)]
            public string To { get; set; }

            [Option('d', "delay", HelpText = "Delay in seconds after each page fetch.", Default = 2)]
            public int DelayInSeconds { get; set; }
        }


        static async Task Main(string[] args)
        {
#if DEBUG
            args = new string[] { "-u", @"katespade.com", "-f", "20180922", "-t", "20181023"/*, "-d", "10", "-c", "10" */};
#endif

            var parsedResult = await Parser.Default.ParseArguments<Options>(args)
                      .WithParsedAsync(async o =>
                      {
                          await ParseOptionsAndStartScraping(o);
                      });

            parsedResult.WithNotParsed(ParsingFailed);

            return;
        }

        private static void ParsingFailed(IEnumerable<Error> errors)
        {
            //Console.WriteLine("*************matchType**************");
            Console.WriteLine("MATCH TYPES:");
            Console.WriteLine(" if given the URL: archive.org/about/ and:\n");
            Console.WriteLine("  matchType=exact will return results matching exactly archive.org/about/\n");
            Console.WriteLine("  matchType=prefix will return results for all results under the path archive.org/about/\n");
            Console.WriteLine("  matchType=host will return results from host archive.org\n");
            Console.WriteLine("  matchType=domain will return results from host archive.org and all subhosts *.archive.org");
            //Console.WriteLine("************************************");
        }

        static async Task ParseOptionsAndStartScraping(Options options)
        {
            CdxScrapper scrapper = new CdxScrapper(options.MatchType, options.From, options.To, options.DelayInSeconds)
            {
                ConcurrentTasksCount = options.ConcurrentDownloadsCount
            };

            try
            {
                scrapper.OutputFilePath = GetOutputFileName(options.WebUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ExceptionHelper.ExtractExceptionMessage(ex));
                return;
            }

            var numberOfPages = await scrapper.GetTotalPagesCount(options.WebUrl);
            if (numberOfPages > 0)
            {
                await scrapper.ScrapeAllPages(numberOfPages, options.WebUrl);
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
