using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        #region Private properties



        #endregion

        #region Public Properties

        /// <summary>
        /// Occurs when the raw JSON response is received
        /// </summary>
        public event Action<byte[]> OnRawResponseReceived;

        /// <summary>
        /// Occurs just before we send a request to VirusTotal.
        /// </summary>
        public event Action<HttpRequestMessage> OnHTTPRequestSending;

        /// <summary>
        /// Occurs right after a response has been received from VirusTotal.
        /// </summary>
        public event Action<HttpResponseMessage> OnHTTPResponseReceived;




        public string BaseRequestUrl { get; set; } = @"http://web.archive.org/cdx/search/cdx?url=";
        public string ArchiveAccessUrl { get; set; } = @"https://web.archive.org/web/";

        public List<CDXResult> CDXDataList { get; set; }


        #endregion

        #region Methods

        public async Task StartScraping(string url)
        {
            var numberOfPages = await GetResponse<long>(BaseRequestUrl + url + "&matchType=domain&showNumPages=true", HttpMethod.Get);
            Console.WriteLine($"{numberOfPages} pages found for this domain ({url})");
            CDXDataList = new List<CDXResult>();
            for (int i = 0; i < numberOfPages; i++)
            {
                await ScrapeAPage(url, i);
            }
        }

        public async Task ScrapeAPage(string url, int pageNumber)
        {
            Console.WriteLine($"Fetching URLs from page {pageNumber}...");
            CDXResult cdxData = new CDXResult
            {
                PageNumber = pageNumber,
                URLS = new List<string>()
            };

            string finalUrl = BaseRequestUrl + url + $"&matchType=domain&fl=timestamp,original&output=json&page={pageNumber}";

            var items = await GetResponse<JArray>(finalUrl, HttpMethod.Get);
            foreach (var item in items.Children().Skip(1).ToList())
            {
                cdxData.URLS.Add($"{ArchiveAccessUrl}{item[0]}/{item[1]}");
            }
            CDXDataList.Add(cdxData);
            Console.WriteLine($"Fetching URLS from {pageNumber} completed.");
        }

        #region Http Client Methods

        private async Task<T> GetResponse<T>(string url, HttpMethod method, HttpContent content = null)
        {
            HttpResponseMessage response = await SendRequest(url, method, content).ConfigureAwait(false);

            using (Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            using (StreamReader sr = new StreamReader(responseStream, Encoding.UTF8))
            using (JsonTextReader jsonTextReader = new JsonTextReader(sr))
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
