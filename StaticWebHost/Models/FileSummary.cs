using System.Diagnostics;

namespace StaticWebHost.Models
{
    [DebuggerDisplay("FullPath = {FullPath}")]
    public class FileSummary
    {
        public string Identifier { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public long ByteSize { get; set; }
        public DateTime LastModifiedUtc { get; set; }
    }
}
