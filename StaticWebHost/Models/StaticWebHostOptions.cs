namespace StaticWebHost.Models
{
    public class StaticWebHostOptions
    {
        public string Mode { get; init; } = "BuildAndStatus";
        public double PollingIntervalSeconds { get; init; } = 2;
        public string EsbuildPath { get; init; } = "/_sys/esbuild.exe";
        public List<string> StatusIndicatorUrlPaths { get; init; } = [];
        public string EsbuildCompileArgs { get; init; } = "--bundle --minify --sourcemap --format=esm --target=es2020";
        public string TypeScriptRoot { get; set; } = string.Empty;
        public List<TypeScriptEntry> TypeScriptCompilationFiles { get; init; } = [];
        public List<ScssEntry> ScssCompilationPaths { get; init; } = [];
        public List<string> StaticCopyExcludePaths { get; init; } = [];

        public bool IsEnabled => !this.Mode.Equals("Off", StringComparison.OrdinalIgnoreCase);
        public bool ShowStatus => this.Mode.Equals("BuildAndStatus", StringComparison.OrdinalIgnoreCase);
    }

    public class TypeScriptEntry
    {
        // Relative path to the .ts entry file, e.g. "/src/app.ts"
        public string Entry { get; set; } = string.Empty;

        // Relative path for the compiled bundle output, e.g. "/js/app.bundle.min.js"
        public string Output { get; set; } = string.Empty;
    }

    public class ScssEntry
    {
        // Relative path to a directory to scan for .scss files, e.g. "/styles"
        public string ScanDir { get; set; } = string.Empty;

        // Relative path to the directory to write compiled .css files into
        public string Output { get; set; } = string.Empty;
    }
}