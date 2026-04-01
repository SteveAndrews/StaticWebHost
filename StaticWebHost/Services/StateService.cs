using System.Text.Json;

namespace StaticWebHost.Services
{
    // Singleton service that tracks file state (size + modified date) across
    // poll cycles. Used by SCSS and TypeScript compiler services to detect
    // source file changes. Static copy uses direct file comparison instead.
    public class StateService(IWebHostEnvironment env, ILogger<StateService> logger)
    {
        private readonly JsonSerializerOptions _serializerOptions = new() { WriteIndented = true };
        private Dictionary<string, FileState> _state = [];

        internal string StatePath => Path.Combine(env.ContentRootPath, "_sys", "state.json");

        public void Load()
        {
            try
            {
                if (!File.Exists(this.StatePath))
                {
                    return;
                }

                var json = File.ReadAllText(this.StatePath);
                this._state = JsonSerializer.Deserialize<Dictionary<string, FileState>>(json) ?? [];
            }
            catch
            {
                this._state = [];
            }
        }

        public void Save()
        {
            try
            {
                Utils.EnsureDirectory(this.StatePath);
                var json = JsonSerializer.Serialize(this._state, this._serializerOptions);
                File.WriteAllText(this.StatePath, json);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to save state file.");
            }
        }

        // Returns true if the file has changed since it was last recorded,
        // or if it has never been recorded.
        public bool HasChanged(string path)
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var info = new FileInfo(path);

            if (!this._state.TryGetValue(path, out var prev))
            {
                return true;
            }

            return info.Length != prev.Size ||
                   NormalizeToSeconds(info.LastWriteTimeUtc) != NormalizeToSeconds(prev.Modified);
        }

        public void Update(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            var info = new FileInfo(path);
            this._state[path] = new FileState(info.Length, info.LastWriteTimeUtc);
        }

        private static long NormalizeToSeconds(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, dt.Day,
                dt.Hour, dt.Minute, dt.Second, DateTimeKind.Utc).Ticks;
        }
    }

    public record FileState(long Size, DateTime Modified);
}
