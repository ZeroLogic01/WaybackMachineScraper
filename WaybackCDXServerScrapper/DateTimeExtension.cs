using System;

namespace WaybackCDXServerScrapper
{
    public static class DateTimeExtension
    {
        public static DateTime ToFormat12h(this DateTime dt)
        {
            return Convert.ToDateTime(dt.ToString("yyyy/MM/dd, hh:mm:ss tt"));
        }

        public static DateTime ToFormat24h(this DateTime dt)
        {
            return Convert.ToDateTime(dt.ToString("yyyy/MM/dd, HH:mm:ss"));
        }

        public static string ToFormat12hString(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd, hh:mm:ss tt");
        }

        public static string ToFormat24hString(this DateTime dt)
        {
            return dt.ToString("yyyy/MM/dd, HH:mm:ss");
        }
    }
}
