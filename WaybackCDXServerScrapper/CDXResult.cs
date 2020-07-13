using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WaybackCDXServerScrapper
{
    public class CDXResult
    {
        //public string Urlkey { get; set; }
        //public string Timestamp { get; set; }
        //public string Original { get; set; }
        ////public string Mimetype { get; set; }
        //public int Statuscode { get; set; }
        //public string Digest { get; set; }
        //public int Length { get; set; }

        public int PageNumber { set; get; }
        public List<string> URLS { get; set; }

    }
}
