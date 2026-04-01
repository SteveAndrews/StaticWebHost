using StaticWebHost.Models;
using StaticWebHost.Properties;
using StaticWebHost.Services;
using StaticWebHost.Services.FileServices;

internal class Program
{
    private static void Main(string[] args)
    {
        EnsureWWWRoot();

        var builder = WebApplication.CreateBuilder(args);

        var options = builder.Configuration
            .GetSection("StaticWebHost")
            .Get<StaticWebHostOptions>() ?? new StaticWebHostOptions();

        builder.Services.AddSingleton(options);

        if (options.IsEnabled)
        {
            builder.Services.AddSingleton<StateService>();
            builder.Services.AddSingleton<BuildStatusService>();
            builder.Services.AddSingleton<StaticCopyService>();
            builder.Services.AddSingleton<ScssCompilerService>();
            builder.Services.AddSingleton<TypeScriptCompilerService>();
            builder.Services.AddHostedService<AssetWatcherService>();
        }

        var app = builder.Build();

        if (options.IsEnabled && options.ShowStatus)
        {
            // ----- SSE endpoint -----
            app.MapGet("/_buildstatus/events", async (BuildStatusService buildStatus, HttpContext context, CancellationToken ct) =>
            {
                context.Response.Headers.Append("Content-Type", "text/event-stream");
                context.Response.Headers.Append("Cache-Control", "no-cache");
                context.Response.Headers.Append("X-Accel-Buffering", "no");

                var clientToken = context.Request.Query["token"].FirstOrDefault();

                var writer = new StreamWriter(context.Response.Body, leaveOpen: true)
                {
                    AutoFlush = false,
                };

                await buildStatus.AddClientAsync(writer, clientToken);

                var tcs = new TaskCompletionSource();
                ct.Register(() => tcs.TrySetResult());
                await tcs.Task;

                buildStatus.RemoveClient(writer);
                await writer.DisposeAsync();
            });

            // ----- HTML injection middleware -----
            // Appends the build status script tag before </body> in any HTML response.
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value ?? string.Empty;

                var shouldInject = options.StatusIndicatorUrlPaths
                    .Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase));

                if (!shouldInject)
                {
                    await next();
                    return;
                }

                var originalBody = context.Response.Body;
                using var buffer = new MemoryStream();
                context.Response.Body = buffer;

                await next();

                buffer.Seek(0, SeekOrigin.Begin);
                var body = await new StreamReader(buffer).ReadToEndAsync();

                if (context.Response.ContentType?.Contains("text/html") == true)
                {
                    const string inject = "\n<script src=\"/_buildstatus/build-status.min.js\"></script>";
                    body = body.Replace("</body>", inject + "\n</body>",
                        StringComparison.OrdinalIgnoreCase);
                }

                context.Response.Body = originalBody;
                await context.Response.WriteAsync(body);
            });
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.Run();
    }

    private static void EnsureWWWRoot()
    {
        var contentRootPath = AppContext.BaseDirectory;

        if (contentRootPath.Contains("\\bin\\"))
        {
            contentRootPath = contentRootPath!.Substring(0, contentRootPath.IndexOf("bin"));
        }

        var wwwrootDirectory = Path.Combine(contentRootPath, "wwwroot");
        if (!Directory.Exists(wwwrootDirectory))
        {
            Directory.CreateDirectory(wwwrootDirectory);
        }

        var buildStatusDir = Path.Combine(wwwrootDirectory, "_buildstatus");
        if (!Directory.Exists(buildStatusDir))
        {
            Directory.CreateDirectory(buildStatusDir);
        }

        var buildStatusJsPath = Path.Combine(buildStatusDir, "build-status.min.js");
        if (!File.Exists(buildStatusJsPath))
        {
            File.WriteAllBytes(buildStatusJsPath, Resources.build_status_min_js);
        }

        var buildStatusCssPath = Path.Combine(buildStatusDir, "build-status.min.css");
        if (!File.Exists(buildStatusCssPath))
        {
            File.WriteAllBytes(buildStatusCssPath, Resources.build_status_min_css);
        }
    }
}