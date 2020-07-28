using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace WaybackCDXServerScrapper
{
    public class CDXResults
    {
        public long PageNumber { set; get; }
        public List<CDXResult> URLS { get; set; }
    }
}
