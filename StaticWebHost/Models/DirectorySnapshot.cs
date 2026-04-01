using System.Diagnostics;

namespace StaticWebHost.Models
{
    [DebuggerDisplay("RelativePath = {RelativePath}")]
    public class DirectorySnapshot
    {
        public string RelativePath { get; set; } = string.Empty;
        public string Root { get; set; } = string.Empty;

        public int FileCount { get; set; }
        public long TotalSize { get; set; }
        public DateTime LatestModifiedUtc { get; set; }

        public string FullPath => Path.Combine(this.Root, this.RelativePath);
    }
}
