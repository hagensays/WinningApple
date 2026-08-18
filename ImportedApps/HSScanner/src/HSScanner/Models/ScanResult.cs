using System;
using System.Collections.Generic;

namespace HSScanner.Models
{
    public sealed class ScanResult
    {
        public string RootPath { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime FinishedAt { get; set; }
        public DateTime CutoffDate { get; set; }
        public int CutoffYears { get; set; }
        public bool IncludeHidden { get; set; }
        public bool IncludeSystem { get; set; }
        public bool SkipReparsePoints { get; set; }
        public bool WasCancelled { get; set; }
        public List<FolderRecord> Folders { get; private set; }
        public List<FileRecord> Files { get; private set; }
        public List<ScanError> Errors { get; private set; }

        public ScanResult()
        {
            Folders = new List<FolderRecord>();
            Files = new List<FileRecord>();
            Errors = new List<ScanError>();
        }

        public long TotalBytes
        {
            get
            {
                long total = 0;
                foreach (var file in Files) total += file.SizeBytes;
                return total;
            }
        }

        public long OlderThanCutoffFiles
        {
            get
            {
                long total = 0;
                foreach (var file in Files) if (file.OlderThanCutoff) total++;
                return total;
            }
        }

        public long OlderThanCutoffBytes
        {
            get
            {
                long total = 0;
                foreach (var file in Files) if (file.OlderThanCutoff) total += file.SizeBytes;
                return total;
            }
        }
    }
}
