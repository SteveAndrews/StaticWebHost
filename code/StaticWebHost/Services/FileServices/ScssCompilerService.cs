using DartSassHost;
using JavaScriptEngineSwitcher.Jint;
using StaticWebHost.Models;

namespace StaticWebHost.Services.FileServices
{
    public class ScssCompilerService(StateService state, BuildStatusService buildStatus, ILogger<ScssCompilerService> logger) : BaseFileService
    {
        internal async Task<FileServiceResult> Process(StaticWebHostOptions options, IWebHostEnvironment env)
        {
            var root = env.ContentRootPath;
            var hasError = false;
            var anyChanged = false;

            foreach (var entry in options.SCSSBuild.ScssCompilationPaths)
            {
                var scanDir = this.ToAbsolute(root, entry.ScanDir);
                var outDir = this.ToAbsolute(root, entry.Output);

                if (!Directory.Exists(scanDir))
                {
                    continue;
                }

                var scssFiles = Directory.GetFiles(scanDir, "*.scss");

                var partialChanged = Directory
                    .GetFiles(scanDir, "_*.scss")
                    .Any(state.HasChanged);

                var nonPartialChanged = scssFiles
                    .Where(f => !Path.GetFileName(f).StartsWith('_'))
                    .Any(state.HasChanged);

                if (partialChanged || nonPartialChanged)
                {
                    anyChanged = true;

                    await buildStatus.PublishAsync(BuildStatus.Detected);

                    foreach (var scssFile in scssFiles)
                    {
                        if (Path.GetFileName(scssFile).StartsWith('_'))
                        {
                            state.Update(scssFile);
                            continue;
                        }

                        if (state.HasChanged(scssFile) || partialChanged)
                        {
                            var cssFile = Path.Combine(outDir,
                                Path.GetFileNameWithoutExtension(scssFile) + ".css");

                            var ok = this.CompileFile(scssFile, cssFile);

                            if (!ok)
                            {
                                hasError = true;
                            }
                        }

                        state.Update(scssFile);
                    }
                }
            }

            if (anyChanged)
            {
                state.Save();
            }

            return new(hasError, anyChanged);
        }

        // Returns true on success, false on failure.
        public bool CompileFile(string scssPath, string cssPath)
        {
            try
            {
                using var compiler = new SassCompiler(new JintJsEngineFactory());
                var result = compiler.CompileFile(scssPath);

                Utils.EnsureDirectory(cssPath);

                File.WriteAllText(cssPath, result.CompiledContent);

                var minPath = cssPath.Replace(".css", ".min.css");
                var minResult = compiler.CompileFile(scssPath, options: new CompilationOptions
                {
                    OutputStyle = OutputStyle.Compressed
                });

                Utils.EnsureDirectory(minPath);

                File.WriteAllText(minPath, minResult.CompiledContent);

                logger.LogInformation("Compiled SCSS: {Input} -> {Output}", scssPath, cssPath);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SCSS compilation failed for {Path}", scssPath);

                return false;
            }
        }
    }
}
