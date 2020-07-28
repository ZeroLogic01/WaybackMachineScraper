using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WaybackCDXServerScrapper
{
    public class CdxScrapper
    {

        #region Public Properties

        /// <summary>
        /// Occurs when the raw response is received
        /// </summary>
        public event Action<byte[]> OnRawResponseReceived;

        /// <summary>
        /// Occurs just before we send a request to the CDX.
        /// </summary>
        public event Action<HttpRequestMessage> OnHTTPRequestSending;

        /// <summary>
        /// Occurs right after a response has been received from the CDX.
        /// </summary>
        public event Action<HttpResponseMessage> OnHTTPResponseReceived;

        public string BaseRequestUrl { get; set; } = @"http://web.archive.org/cdx/search/cdx?url=";
        public string ArchiveAccessUrl { get; set; } = @"https://web.archive.org/web/";

        public List<CDXResults> CDXDataList { get; set; }
        public Queue<long> FailedFetchPages { get; set; }

        #endregion

        public int ConcurrentTasksCount { get; set; } = 1;
        public List<Task> ConcurrentTasks { get; set; } = new List<Task>();
        public MatchTypeFilter MatchType { get; }
        public string From { get; }
        public string To { get; }

        #region Constructor

        public CdxScrapper(MatchTypeFilter matchType, string from, string to)
        {
            MatchType = matchType;
            From = from;
            To = to;
        }

        #endregion

        #region Methods

        public async Task<long> GetTotalPagesCount(string url, string filePath)
        {
            string requestUrl = BaseRequestUrl + url + $"&matchType={MatchType}&showNumPages=true";
            Console.WriteLine("************************************");
            Console.WriteLine($"Scanning {url}");
           
            if (!string.IsNullOrWhiteSpace(From) && !string.IsNullOrWhiteSpace(To))
            {
                requestUrl = $"{requestUrl}&from={From}&to={To}";
                Console.WriteLine($"From {From} to {To}");
            }

            Console.WriteLine($"Concurrent tasks count: {ConcurrentTasksCount}");
            Console.WriteLine($"Output file: {Path.GetFileName(filePath)}");

            var numberOfPages = await GetResponse<long>(requestUrl, HttpMethod.Get);

            Console.WriteLine($"Total number of pages found: {numberOfPages}");
            Console.WriteLine("************************************");

            return numberOfPages;
        }


        public async Task ScrapeAllPages(long totalNumberOfPages, string domainName, string outputFilePath)
        {
            Queue<long> pages = new Queue<long>();
            for (long i = 0; i < totalNumberOfPages; i++)
            {
                pages.Enqueue(i);
            }
            await ScrapeAllPages(pages, domainName, outputFilePath);
        }

        public async Task ScrapeAllPages(Queue<long> pages, string url, string filePath)
        {
            CDXDataList = new List<CDXResults>();
            FailedFetchPages = new Queue<long>();

            bool isSomePagesRemaining = pages.Count > 0;

            while (isSomePagesRemaining)
            {
                if (ConcurrentTasks.Count < ConcurrentTasksCount)
                {
                    int remainingSpace = ConcurrentTasksCount - ConcurrentTasks.Count;
                    for (long i = 0; i < remainingSpace; i++)
                    {
                        if (pages.Any())
                        {
                            long page = pages.Dequeue();
                            ConcurrentTasks.Add(ScrapeAPageAndSave(url, page, filePath));
                        }
                        else
                        {
                            isSomePagesRemaining = false;
                            break;
                        }
                    }
                    if (ConcurrentTasks.Any())
                    {
                        var completedTask = await Task.WhenAny(ConcurrentTasks);
                        ConcurrentTasks.Remove(completedTask);
                    }

                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            if (FailedFetchPages.Count > 0)
            {
                Console.WriteLine("************************************");
                Console.WriteLine("Re-fetching failed pages");
                Console.WriteLine($"Total number of pages failed: {FailedFetchPages.Count}");
                Console.WriteLine("************************************");
                await ScrapeAllPages(FailedFetchPages, url, filePath);
            }
        }
        public async Task ScrapeAPageAndSave(string url, long pageNumber, string filePath)
        {
            try
            {
                var data = await ScrapeAPage(url, pageNumber);
                if (data != null && data.URLS.Count > 0)
                {
                    Console.WriteLine($"Saving page {pageNumber + 1} URLs");
                    FileWriterExtension.WriteToFile(data.URLS, filePath);
                    Console.WriteLine($"Saving page {pageNumber + 1} URLs completed");
                }
                else
                {
                    Console.WriteLine($"No URLs found for this domain on page {pageNumber + 1}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while fetching/saving page {pageNumber + 1} URLS.\r\n{ExceptionHelper.ExtractExceptionMessage(ex)}");
                FailedFetchPages.Enqueue(pageNumber);
                await Task.Delay(TimeSpan.FromSeconds(10));
            }
        }

        public async Task<CDXResults> ScrapeAPage(string url, long pageNumber)
        {
            CDXResults cdxData = new CDXResults
            {
                PageNumber = pageNumber,
                URLS = new List<CDXResult>()
            };

            Console.WriteLine($"Fetching URLs from page {pageNumber + 1}...");

            string finalUrl = BaseRequestUrl + url + $"&matchType={MatchType}&fl=original,timestamp,mimetype&output=json&page={pageNumber}";


            if (!string.IsNullOrWhiteSpace(From) && !string.IsNullOrWhiteSpace(To))
            {
                finalUrl = $"{finalUrl}&from={From}&to={To}";
            }

            var items = await GetResponse<JArray>(finalUrl, HttpMethod.Get);

            if (items == null)
            {
                return null;
            }
            foreach (var item in items?.Children().Skip(1).ToList())
            {
                var result = new CDXResult()
                {
                    URL = $"{ArchiveAccessUrl}{item[1]}/{item[0]}",
                    Mimetype = item[2].ToString(),
                    Timestamp = DateTime.ParseExact(item[1].ToString(), "yyyyMMddHHmmss", null)
                };
                cdxData.URLS.Add(result);
            }

            Console.WriteLine($"Fetching URLS from page {pageNumber + 1} completed.");

            return cdxData;
        }

        #region HTTP Client Methods

        private async Task<T> GetResponse<T>(string url, HttpMethod method, HttpContent content = null)
        {
            try
            {
                HttpResponseMessage response = await SendRequest(url, method, content).ConfigureAwait(false);

                using Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using StreamReader sr = new StreamReader(responseStream, Encoding.UTF8);
                using JsonTextReader jsonTextReader = new JsonTextReader(sr);
                {
                    jsonTextReader.CloseInput = false;

                    SaveResponse(responseStream);
                    JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.Indented
                    };

                    var serializer = JsonSerializer.Create(jsonSettings);
                    return serializer.Deserialize<T>(jsonTextReader);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<HttpResponseMessage> SendRequest(string url, HttpMethod method, HttpContent content)
        {
            HttpRequestMessage request = new HttpRequestMessage(method, url)
            {
                Content = content
            };

            OnHTTPRequestSending?.Invoke(request);

            HttpClient client = new HttpClient(handler:
                 new HttpClientHandler
                 {
                     AllowAutoRedirect = true
                 }
            )
            {
                Timeout = Timeout.InfiniteTimeSpan
            };

            HttpResponseMessage response = await client.SendAsync(request, CancellationToken.None).ConfigureAwait(false);

            OnHTTPResponseReceived?.Invoke(response);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                if (string.IsNullOrWhiteSpace(response.Content.ToString()))
                    throw new Exception("There were no content in the response.");
                else
                {
                    return response;
                }
            }

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("API gave error code " + response.StatusCode);

            if (string.IsNullOrWhiteSpace(response.Content.ToString()))
                throw new Exception("There were no content in the response.");

            return response;
        }

        private void SaveResponse(Stream stream)
        {
            if (OnRawResponseReceived == null)
                return;

            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                OnRawResponseReceived(ms.ToArray());
            }

            stream.Position = 0;
        }

        #endregion

        #endregion


    }
}
