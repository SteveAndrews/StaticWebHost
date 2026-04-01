namespace StaticWebHost.Services
{
    public abstract class BaseFileService
    {
        // For static copy — same file type, compare size and date directly
        protected bool OutputNeedsRebuild(string sourcePath, string outputPath)
        {
            if (!File.Exists(outputPath))
            {
                return true;
            }

            var source = new FileInfo(sourcePath);
            var output = new FileInfo(outputPath);

            return source.Length != output.Length
                || NormalizeToSeconds(source.LastWriteTimeUtc) > NormalizeToSeconds(output.LastWriteTimeUtc);
        }

        protected static long NormalizeToSeconds(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day,
                dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc).Ticks;
        }

        protected string ToAbsolute(string root, string relative)
        {
            return Path.Combine(root, relative.TrimStart('/', '\\'));
        }
    }
}
