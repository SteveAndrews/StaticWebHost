namespace StaticWebHost.Models
{
    public class StaticWebHostOptions
    {
        public Config Config { get; init; } = new();
        public TypeScriptBuild TypeScriptBuild { get; init; } = new();
        public SCSSBuild SCSSBuild { get; init; } = new();
        public StaticFilesCopy StaticFilesCopy { get; init; } = new();

        public bool IsEnabled => !this.Config.Mode.Equals("Off", StringComparison.OrdinalIgnoreCase);
        public bool ShowStatus => this.Config.Mode.Equals("BuildAndStatus", StringComparison.OrdinalIgnoreCase);
    }

    public class Config
    {
        public string Mode { get; init; } = "BuildAndStatus";
        public double PollingIntervalSeconds { get; init; } = 2;
        public List<string> StatusIndicatorUrlPaths { get; init; } = [];
    }

    public class TypeScriptBuild
    {
        public bool Enable { get; init; } = true;
        public string EsbuildPath { get; init; } = "/_sys/esbuild.exe";
        public string EsbuildCompileArgs { get; init; } = "--bundle --minify --sourcemap --format=esm --target=es2020";
        public string TypeScriptRoot { get; set; } = string.Empty;
        public List<TypeScriptEntry> TypeScriptCompilationFiles { get; init; } = [];
    }

    public class TypeScriptEntry
    {
        public string Entry { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
    }

    public class SCSSBuild
    {
        public bool Enable { get; init; } = true;
        public List<ScssEntry> ScssCompilationPaths { get; init; } = [];
    }

    public class ScssEntry
    {
        public string ScanDir { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
    }

    public class StaticFilesCopy
    {
        public bool Enable { get; init; } = true;
        public List<string> StaticCopyExcludePaths { get; init; } = [];
    }
}