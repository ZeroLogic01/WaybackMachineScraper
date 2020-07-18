using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace WaybackCDXServerScrapper
{
    public static class FileWriterExtension
    {
        static readonly ReaderWriterLock locker = new ReaderWriterLock();
        public static void WriteToFile(this List<string> records, string filePath)
        {
            try
            {
                locker.AcquireWriterLock(int.MaxValue); //You might wanna change timeout value 
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(fileStream, Encoding.UTF8))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        foreach (var record in records)
                        {
                            csv.WriteField(record);
                            csv.NextRecord();
                        }
                        writer.Flush();
                    }
                }
            }
            finally
            {
                locker.ReleaseWriterLock();
            }
        }
    }
}
