using System;

namespace HSScanner.Models
{
    public sealed class ScanError
    {
        public DateTime Time { get; set; }
        public string Path { get; set; }
        public string Operation { get; set; }
        public string Message { get; set; }
    }
}
