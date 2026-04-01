namespace StaticWebHost.Services
{
    public enum BuildStatus
    {
        Ready,
        Detected,
        Built,
        Error,
    }

    public sealed class BuildStatusService : IDisposable
    {
        private readonly ILogger<BuildStatusService> _logger;
        private readonly Lock _lock = new();
        private readonly List<StreamWriter> _clients = [];

        private BuildStatus _current = BuildStatus.Ready;

        public string BuildToken { get; private set; } = string.Empty;

        public BuildStatusService(ILogger<BuildStatusService> logger)
        {
            this._logger = logger;
        }

        public BuildStatus Current
        {
            get
            {
                lock (this._lock)
                {
                    return this._current;
                }
            }
        }

        public async Task PublishAsync(BuildStatus status)
        {
            string payload;

            lock (this._lock)
            {
                this._current = status;

                if (status == BuildStatus.Built)
                {
                    this.BuildToken = GenerateToken();
                    payload = $"data: built:{this.BuildToken}\n\n";
                }
                else
                {
                    payload = $"data: {StatusName(status)}\n\n";
                }
            }

            List<StreamWriter> snapshot;

            lock (this._lock)
            {
                snapshot = [.. this._clients];
            }

            List<StreamWriter> dead = [];

            foreach (var client in snapshot)
            {
                try
                {
                    await client.WriteAsync(payload);
                    await client.FlushAsync();
                }
                catch
                {
                    dead.Add(client);
                }
            }

            if (dead.Count > 0)
            {
                lock (this._lock)
                {
                    foreach (var d in dead)
                    {
                        this._clients.Remove(d);
                    }
                }
            }

            this._logger.LogInformation("Build status: {Status}", status);
        }

        public async Task AddClientAsync(StreamWriter writer, string? clientToken)
        {
            BuildStatus toSend;
            var acknowledge = false;

            lock (this._lock)
            {
                this._clients.Add(writer);

                if (this._current == BuildStatus.Built)
                {
                    var tokenMatchesCurrentBuild = !string.IsNullOrEmpty(clientToken) && clientToken == this.BuildToken;

                    if (tokenMatchesCurrentBuild)
                    {
                        this._current = BuildStatus.Ready;
                        acknowledge = true;
                    }
                    else if (string.IsNullOrEmpty(clientToken))
                    {
                        this._current = BuildStatus.Ready;
                    }
                }

                toSend = this._current;
            }

            await writer.WriteAsync($"data: {StatusName(toSend)}\n\n");
            await writer.FlushAsync();

            // Notify all other clients of the ready transition
            if (acknowledge)
            {
                await this.PublishToAllExceptAsync(writer, "data: ready\n\n");
                this._logger.LogInformation("Build status: Ready (build acknowledged by browser)");
            }
        }

        public void RemoveClient(StreamWriter writer)
        {
            lock (this._lock)
            {
                this._clients.Remove(writer);
            }
        }

        private async Task PublishToAllExceptAsync(StreamWriter except, string payload)
        {
            List<StreamWriter> snapshot;
            lock (this._lock)
            {
                snapshot = [.. this._clients];
            }

            List<StreamWriter> dead = [];

            foreach (var client in snapshot)
            {
                if (ReferenceEquals(client, except))
                {
                    continue;
                }

                try
                {
                    await client.WriteAsync(payload);
                    await client.FlushAsync();
                }
                catch
                {
                    dead.Add(client);
                }
            }

            if (dead.Count > 0)
            {
                lock (this._lock)
                {
                    foreach (var d in dead)
                    {
                        this._clients.Remove(d);
                    }
                }
            }
        }

        private static string GenerateToken()
        {
            return Convert.ToHexString(Guid.NewGuid().ToByteArray())[..8].ToLowerInvariant();
        }

        private static string StatusName(BuildStatus status)
        {
            return status switch
            {
                BuildStatus.Ready => "ready",
                BuildStatus.Detected => "detected",
                BuildStatus.Built => "built",
                BuildStatus.Error => "error",
                _ => "error",
            };
        }

        public void Dispose()
        {
            lock (this._lock)
            {
                this._clients.Clear();
            }
        }
    }
}
