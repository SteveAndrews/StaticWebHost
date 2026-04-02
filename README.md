# StaticWebHost

StaticWebHost is a minimal ASP.NET Core host for static websites with automatic real-time SCSS and TypeScript compilation. Drop the files into a web site root, configure your paths, and write SCSS and TypeScript with a clean dev space and no build pipeline, task runner, or terminal required.

## License

StaticWebHost is free for non-commercial use under the
[PolyForm Noncommercial License 1.0.0](LICENSE-NONCOMMERCIAL.md).
This covers personal projects, hobby use, education, research, and use by
non-commercial organizations. Any use that benefits a commercial enterprise, including internal tooling at a company, requires a commercial license.

## How it works

StaticWebHost runs as a lightweight ASP.NET Core app that serves your static files and watches your source files in the background. When a `.scss` or `.ts` file changes, it recompiles automatically. By the time you switch to the browser, the output is ready.

- **SCSS** is compiled via [DartSassHost](https://github.com/Taritsyn/DartSassHost). Both `.css` and `.min.css` are written.
- **TypeScript** is compiled and bundled via [esbuild](https://esbuild.github.io/). A single `.bundle.min.js` output is produced, with source map.
- **Static files** in `dev/wwwroot/` are synced to `wwwroot/` automatically when changed.
- **Polling**: on startup, all source files are compiled fresh. During the session, source files are checked every 2 seconds (by default) and recompiled automatically when changes are detected.
- **Build status indicator**: an unobtrusive overlay injected into configured HTML pages shows live compilation state in the browser.

## Requirements

- [.NET 9 runtime](https://dotnet.microsoft.com/download)
- The correct `esbuild` binary for your platform, from [esbuild releases](https://github.com/evanw/esbuild/releases), placed in `_sys/`:
  - Windows: `esbuild.exe` — download `esbuild-windows-64.zip`
  - Linux: `esbuild` — download `esbuild-linux-64.zip`; mark executable with `chmod +x _sys/esbuild`
  - macOS: `esbuild` — download `esbuild-darwin-64.zip`; mark executable with `chmod +x _sys/esbuild`

StaticWebHost uses Kestrel (the built-in ASP.NET Core web server) and requires no additional web server to run. For development, `dotnet run` or F5 in Visual Studio is all that's needed.

If you want to run it behind a web server (for SSL, port sharing, or process management), it should be compatible with IIS, nginx, Apache, and Caddy on their respective platforms.

## Getting started

1. Clone or copy this repository into your project folder.
2. Place the correct `esbuild` binary for your platform in `_sys/` (see Requirements above).
3. Edit `appsettings.json` to point at your source files (see Configuration below).
4. Hit F5 in Visual Studio, or run `dotnet run`.
5. Your compiled CSS and JS will appear at the configured output paths on first run, and update automatically as you save files.

A pre-compiled build is available in the `dist/` folder. Point a web host at it, open an editor, and start building.

## Project structure

```
_sys/
  esbuild.exe             ← required, not included (see Getting started)
  state.json              ← generated during a run, deleted and rebuilt on each startup
dev/                      ← open this folder in your IDE
  src/
    main/
      app.ts              ← TypeScript entry point
  styles/
    styles.scss           ← SCSS source
  wwwroot/                ← static assets (HTML, images, fonts, etc.)
    index.html
wwwroot/                  ← generated output, served by Kestrel
  _buildstatus/           ← build status indicator (generated at runtime)
  js/                     ← compiled JS output (generated)
  styles/                 ← compiled CSS output (generated)
  index.html              ← copied from dev/wwwroot/
Models/
  FileServiceResult.cs
  StaticWebHostOptions.cs
Resources/
  build-status.min.css
  build-status.min.js
Services/
  AssetWatcherService.cs
  BaseFileService.cs
  BuildStatusService.cs
  StateService.cs
  FileServices/
    ScssCompilerService.cs
    StaticCopyService.cs
    TypeScriptCompilerService.cs
appsettings.json
Program.cs
```

The `dev/` folder is where all development happens. Open it in your IDE. The `wwwroot/` folder at the project root is entirely generated and served by Kestrel. It is never edited directly.

## Configuration

All paths are configured in `appsettings.json` under the `StaticWebHost` key, organized into four sections:

```json
"StaticWebHost": {
    "Config": {
        "Mode": "BuildAndStatus",
        "PollingIntervalSeconds": 2,
        "StatusIndicatorUrlPaths": [
            "/",
            "/index.html"
        ]
    },
    "TypeScriptBuild": {
        "Enable": true,
        "EsbuildPath": "/_sys/esbuild.exe",
        "EsbuildCompileArgs": "--bundle --minify --format=esm --target=es2020",
        "TypeScriptRoot": "/dev/src",
        "TypeScriptCompilationFiles": [
            {
                "entry": "/dev/src/main/app.ts",
                "output": "/wwwroot/js/app.bundle.min.js"
            }
        ]
    },
    "SCSSBuild": {
        "Enable": true,
        "ScssCompilationPaths": [
            {
                "scanDir": "/dev/styles",
                "output": "/wwwroot/styles"
            }
        ]
    },
    "StaticFilesCopy": {
        "Enable": true,
        "StaticCopyExcludePaths": [
            "/dev/src",
            "/dev/styles"
        ]
    }
}
```

All paths are relative to the project root. Output paths use the `/wwwroot/` prefix explicitly.

### Config.Mode

Controls the level of tooling active at runtime:

| Value | Behaviour |
|---|---|
| `BuildAndStatus` | Watcher runs, build status indicator injected into configured pages |
| `Build` | Watcher runs. No indicator |
| `Off` | No watcher. Pure static file host serves files from `/wwwroot` |

### Config.StatusIndicatorUrlPaths

The URL paths where the build status indicator script will be injected. Both `/` and `/index.html` should be listed since the browser may request either:

```json
"StatusIndicatorUrlPaths": [ "/", "/index.html" ]
```

For multi-page sites, add each page that should show the indicator.

### TypeScriptBuild

The `EsbuildPath` should point to the esbuild binary for your platform (see Requirements). The binary is not included; download it from [esbuild releases](https://github.com/evanw/esbuild/releases).

`TypeScriptRoot` is the root directory watched for TypeScript changes. Each entry in `TypeScriptCompilationFiles` defines an entry point and its output path. Multiple entry points are supported and watched independently — a change in one entry point's source tree does not trigger a recompile of the others:

```json
"TypeScriptCompilationFiles": [
    { "entry": "/dev/src/main/app.ts",      "output": "/wwwroot/js/app.bundle.min.js" },
    { "entry": "/dev/src/kanban/kanban.ts", "output": "/wwwroot/js/kanban.bundle.min.js" }
]
```

Organise TypeScript by feature directory under `dev/src/`. Each entry point watches its own subdirectory recursively and imports from nested folders are tracked automatically.

Note that esbuild transpiles TypeScript without type checking. Type errors will not fail the build. IDEs such as Visual Studio and Rider should catch type errors as you work.

### TypeScriptBuild.EsbuildCompileArgs

The arguments passed to esbuild for every TypeScript compilation. The default produces a minified ESM bundle with source map:

```
--bundle --minify --format=esm --target=es2020
```

### SCSSBuild

Each entry in `ScssCompilationPaths` defines a directory to scan for `.scss` files and an output directory. Multiple SCSS directories are supported:

```json
"ScssCompilationPaths": [
    { "scanDir": "/dev/styles",        "output": "/wwwroot/styles" },
    { "scanDir": "/dev/admin/styles",  "output": "/wwwroot/admin/styles" }
]
```

Partial files (prefixed with `_`, e.g. `_variables.scss`) are supported. Saving a partial triggers a recompile of all non-partial files in the same directory.

### StaticFilesCopy.StaticCopyExcludePaths

A string array of paths under `dev/` that should not be copied to `wwwroot/`. Any path not excluded will be synced to automatically.

```json
"StaticCopyExcludePaths": [ "/dev/pathToExclude" ]
```

## Demo code

| Source | Output |
|---|---|
| `dev/styles/main.scss` | `wwwroot/styles/main.css`, `wwwroot/styles/main.min.css` |
| `dev/src/main/app.ts` | `wwwroot/js/app.bundle.min.js` |
| `dev/wwwroot/index.html` | `wwwroot/index.html` |

The minified files are referenced in the HTML:

```html
<link rel="stylesheet" href="/styles/main.min.css" />
<script type="module" src="/js/app.bundle.min.js"></script>
```

## Build status indicator

When `Mode` is `BuildAndStatus`, a small circular indicator appears in the bottom-right corner of configured pages. It reflects the current compilation state in real time via a Server-Sent Events connection.

| State | Colour | Icon | Meaning |
|---|---|---|---|
| Ready | Green | ✓ | All assets up to date and loaded |
| Detected | Orange | ⧗ | Change detected, compiling |
| Built | Blue | ↓ | Compiled successfully, page not yet reloaded |
| Error | Red | ✕ | Compilation failed. Check the console or logs |

When a build completes, the browser automatically reloads to pick up the new assets. Once reloaded, the indicator returns to Ready. If a compilation error occurs, the indicator stays red until the error is resolved and a successful build completes.

A preview of all four indicator states is available at `/_buildstatus/preview.html` while the server is running.

## Deployment

StaticWebHost is primarily a development tool. When you're ready to deploy, copy the `wwwroot` folder to any web server capable of serving static files.

## Roadmap

- More detail in build status indicator including last build time
- CSHTML template compilation
- TypeScript type checking via `tsc` (requires Node)
- JSON data file minification
- Image optimisation

## Acknowledgements

- [esbuild](https://github.com/evanw/esbuild) by Evan Wallace — MIT license
- [DartSassHost](https://github.com/Taritsyn/DartSassHost) by Andrey Taritsyn — Apache 2.0 license
- [JavaScriptEngineSwitcher.Jint](https://github.com/Taritsyn/JavaScriptEngineSwitcher) by Andrey Taritsyn — Apache 2.0 license

## Disclaimer

StaticWebHost is provided as-is, without warranty of any kind. The authors accept no liability for any damages or losses arising from its use. Use at your own risk.
