using StaticWebHost.Models;

namespace StaticWebHost.Services.FileServices
{
    public class TypeScriptCompilerService(
        StaticWebHostOptions options,
        StateService state,
        BuildStatusService buildStatus,
        IWebHostEnvironment env,
        ILogger<TypeScriptCompilerService> logger) : BaseFileService
    {
        private readonly string _esbuildPath = Path.Combine(env.ContentRootPath, options.TypeScriptBuild.EsbuildPath.TrimStart('/', '\\'));

        private readonly string _tsCompileArgs = options.TypeScriptBuild.EsbuildCompileArgs;

        internal async Task<FileServiceResult> Process(StaticWebHostOptions options, IWebHostEnvironment env)
        {
            var root = env.ContentRootPath;

            if (!File.Exists(this._esbuildPath))
            {
                logger.LogWarning("esbuild not found at {Path}. TypeScript compilation will be skipped.", this._esbuildPath);

                return new(false, false);
            }

            if (string.IsNullOrWhiteSpace(options.TypeScriptBuild.TypeScriptRoot) || options.TypeScriptBuild.TypeScriptCompilationFiles.Count == 0)
            {
                return new(false, false);
            }

            // Scan the entire TypeScriptRoot tree once for any changes.
            var tsRoot = this.ToAbsolute(root, options.TypeScriptBuild.TypeScriptRoot);
            var allTsFiles = Directory.GetFiles(tsRoot, "*.ts", SearchOption.AllDirectories);
            var tsChanged = allTsFiles.Any(state.HasChanged);

            if (!tsChanged)
            {
                return new(false, false);
            }

            await buildStatus.PublishAsync(BuildStatus.Detected);

            var hasError = false;

            foreach (var entry in options.TypeScriptBuild.TypeScriptCompilationFiles)
            {
                var tsFile = this.ToAbsolute(root, entry.Entry);
                var jsFile = this.ToAbsolute(root, entry.Output);

                if (!File.Exists(tsFile))
                {
                    logger.LogWarning("TypeScript entry file not found, skipping: {Path}", tsFile);
                    continue;
                }

                var ok = this.CompileFile(tsFile, jsFile);

                if (!ok)
                {
                    hasError = true;
                }
            }

            // Update state for all watched files after compiling all entries.
            foreach (var f in allTsFiles)
            {
                state.Update(f);
            }

            state.Save();

            return new(hasError, true);
        }

        // Returns true on success, false on failure.
        public bool CompileFile(string tsPath, string jsPath)
        {
            if (!File.Exists(this._esbuildPath))
            {
                logger.LogError(
                    "Cannot compile {File} — esbuild not found at {EsbuildPath}.",
                    tsPath, this._esbuildPath);
                return false;
            }

            try
            {
                Utils.EnsureDirectory(jsPath);

                var args = $"\"{tsPath}\" {this._tsCompileArgs} --outfile=\"{jsPath}\"";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = this._esbuildPath,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = System.Diagnostics.Process.Start(psi)!;
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    logger.LogError(
                        "esbuild failed for {File}:\n{Error}",
                        tsPath, stderr);
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    logger.LogWarning(
                        "esbuild warnings for {File}:\n{Warnings}",
                        tsPath, stderr);
                }

                logger.LogInformation(
                    "Compiled TypeScript: {Input} -> {Output}",
                    tsPath, jsPath);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "TypeScript compilation failed for {Path}", tsPath);
                return false;
            }
        }
    }
}
