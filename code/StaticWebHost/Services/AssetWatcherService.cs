using StaticWebHost.Models;
using StaticWebHost.Services.FileServices;

namespace StaticWebHost.Services
{
    public class AssetWatcherService(
        StaticWebHostOptions options,
        StateService state,
        ScssCompilerService scss,
        TypeScriptCompilerService ts,
        StaticCopyService staticCopy,
        BuildStatusService buildStatus,
        IWebHostEnvironment env) : IHostedService, IAsyncDisposable
    {
        private CancellationTokenSource? _cts;
        private Task? _pollTask;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (File.Exists(state.StatePath))
            {
                File.Delete(state.StatePath);
            }

            state.Load();

            this._cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await this.RunCompilePass();

            this._pollTask = this.PollLoop(this._cts.Token);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (this._cts is not null)
            {
                await this._cts.CancelAsync();
            }

            if (this._pollTask is not null)
            {
                await this._pollTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task PollLoop(CancellationToken ct)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.Config.PollingIntervalSeconds));

            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    await this.RunCompilePass();
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
        }

        private async Task RunCompilePass()
        {
            var anyChanged = false;
            var hasError = false;

            // ----- Static Files -----
            if (options.StaticFilesCopy.Enable)
            {
                var copyResult = await staticCopy.Process();
                anyChanged |= copyResult.AnyChanged;
                hasError |= copyResult.HasError;
            }

            // ----- SCSS -----
            if (options.SCSSBuild.Enable)
            {
                var scssResult = await scss.Process(options, env);
                anyChanged |= scssResult.AnyChanged;
                hasError |= scssResult.HasError;
            }

            // ----- TypeScript -----
            if (options.TypeScriptBuild.Enable)
            {
                var tsResult = await ts.Process(options, env);
                anyChanged |= tsResult.AnyChanged;
                hasError |= tsResult.HasError;
            }

            if (anyChanged)
            {
                await buildStatus.PublishAsync(hasError ? BuildStatus.Error : BuildStatus.Built);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this._cts is not null)
            {
                await this._cts.CancelAsync();

                this._cts.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}
