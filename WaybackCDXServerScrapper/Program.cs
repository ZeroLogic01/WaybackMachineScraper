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
            [Option('d', "domain-name", HelpText = "A web URL. It can be a domain/sub-domain or a specific URL.", Required = true)]
            public string DomainName { get; set; }

            [Option('m', "match-type", HelpText = "Match type filter." +
               "See more https://github.com/internetarchive/wayback/blob/master/wayback-cdx-server/README.md#url-match-scope", /*Default = MatchType.exact,*/ Required = false)]
            public MatchTypeFilter MatchType { get; set; }

            [Option('c', "concurrent-downloads", HelpText = "Total number of concurrent downloads.", Default = 1)]
            public int ConcurrentDownloadsCount { get; set; }

            [Option('f', "from", HelpText = "The \"from\" range is inclusive and is specified in the same 1 to 14 digit format used for wayback captures: yyyyMMddhhmmss", Required = false)]
            public string From { get; set; }

            [Option('t', "to", HelpText = "The \"To\" range is inclusive and is specified in the same 1 to 14 digit format used for wayback captures: yyyyMMddhhmmss", Required = false)]
            public string To { get; set; }
        }


        static async Task Main(string[] args)
        {
#if DEBUG
            args = new string[] { "-d", @"amazon.com/Amazon-Prime-Air/b?ie=UTF8&node=8037720011", "-m", "prefix", "-f", "2013", "-t", "2014" };
#endif

            var parsedResult = await Parser.Default.ParseArguments<Options>(args)
                      .WithParsedAsync(async o =>
                      {
                          await StartScraping(o.DomainName, o.MatchType, o.From, o.To, o.ConcurrentDownloadsCount);
                      });

            parsedResult.WithNotParsed(ParsingFailed);

            return;

            if (args.Length != 4 && args.Length != 5)
            {
                Console.WriteLine("WaybackCDXServerScrapper \"domain-name\" \"matchType\" \"{Optional}total-number-of-concurrent-downloads{default is 1}\" Expected.");


                return;
            }

            string domainName = args[0];
            string matchType = args[1];

            if (!matchType.Equals("exact") && !matchType.Equals("prefix") && !matchType.Equals("host") && !matchType.Equals("domain"))
            {
                Console.WriteLine("Only \"exact\", \"prefix\", \"host\" & \"domain\" allowed as matchType");
                return;
            }
        }

        private static void ParsingFailed(IEnumerable<Error> errors)
        {
            Console.WriteLine("*************matchType**************");
            Console.WriteLine("if given the URL: archive.org/about/ and:\n");
            Console.WriteLine("matchType=exact will return results matching exactly archive.org/about/\n");
            Console.WriteLine("matchType=prefix will return results for all results under the path archive.org/about/\n");
            Console.WriteLine("matchType=host will return results from host archive.org\n");
            Console.WriteLine("matchType=domain will return results from host archive.org and all subhosts *.archive.org");
            Console.WriteLine("************************************");
        }

        public static async Task StartScraping(string domainName, MatchTypeFilter matchType, string from, string to, int concurrentTasksCount)
        {
            CdxScrapper scrapper = new CdxScrapper(matchType, from, to)
            {
                ConcurrentTasksCount = concurrentTasksCount
            };

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
