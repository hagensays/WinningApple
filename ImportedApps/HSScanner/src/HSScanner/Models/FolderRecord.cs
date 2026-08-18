using System;

namespace HSScanner.Models
{
    public sealed class FolderRecord
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public int Level { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string RelativePath { get; set; }
        public long DirectFileCount { get; set; }
        public long RecursiveFileCount { get; set; }
        public long DirectSizeBytes { get; set; }
        public long RecursiveSizeBytes { get; set; }
        public long OlderThanCutoffFileCount { get; set; }
        public long OlderThanCutoffSizeBytes { get; set; }
        public DateTime? OldestReferenceDate { get; set; }
        public DateTime? NewestReferenceDate { get; set; }
    }
}
