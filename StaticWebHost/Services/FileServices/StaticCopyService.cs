using StaticWebHost.Models;

namespace StaticWebHost.Services.FileServices
{
    public class StaticCopyService(StaticWebHostOptions options, BuildStatusService buildStatus, IWebHostEnvironment env, ILogger<StaticCopyService> logger) : BaseFileService
    {
        private const string DevWwwRoot = "dev/wwwroot";
        private const string WwwRoot = "wwwroot";

        public string DevWwwRootPath => Path.Combine(env.ContentRootPath, DevWwwRoot);
        public string WwwRootPath => Path.Combine(env.ContentRootPath, WwwRoot);

        public bool DevWwwRootExists => Directory.Exists(this.DevWwwRootPath);

        internal async Task<FileServiceResult> Process()
        {
            var hasError = false;
            var anyChanged = false;

            if (this.DevWwwRootExists)
            {
                anyChanged = this.RunCopyPass();

                if (anyChanged)
                {
                    await buildStatus.PublishAsync(BuildStatus.Built);
                }
            }

            return new(hasError, anyChanged);
        }

        public bool RunCopyPass()
        {
            if (!this.DevWwwRootExists)
            {
                return false;
            }

            var excludedPaths = options.StaticCopyExcludePaths
                .Select(p => Path.Combine(env.ContentRootPath, p.TrimStart('/', '\\')))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var anyChanged = false;

            foreach (var sourceFile in Directory.EnumerateFiles(this.DevWwwRootPath, "*.*", SearchOption.AllDirectories))
            {
                if (excludedPaths.Any(e =>
                    sourceFile.StartsWith(e + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    sourceFile.StartsWith(e + "/", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var relativeFile = Path.GetRelativePath(this.DevWwwRootPath, sourceFile);
                var targetFile = Path.Combine(this.WwwRootPath, relativeFile);

                if (this.OutputNeedsRebuild(sourceFile, targetFile))
                {
                    Utils.EnsureDirectory(targetFile);
                    File.Copy(sourceFile, targetFile, overwrite: true);
                    anyChanged = true;

                    logger.LogInformation("Copied: {Source} -> {Target}", sourceFile, targetFile);
                }
            }

            return anyChanged;
        }
    }
}
