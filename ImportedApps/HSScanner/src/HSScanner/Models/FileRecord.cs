using System;

namespace HSScanner.Models
{
    public sealed class FileRecord
    {
        public long Sequence { get; set; }
        public long FolderId { get; set; }
        public int FolderLevel { get; set; }
        public string FullPath { get; set; }
        public string RelativePath { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public long SizeBytes { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime LastAccessTime { get; set; }
        public DateTime ReferenceDate { get; set; }
        public bool OlderThanCutoff { get; set; }
        public string Attributes { get; set; }
    }
}
