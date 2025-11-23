# Pixiv Novel Archivist

Pixiv Novel Archivist is a .NET 9 console utility that turns any Pixiv novel URL (single chapter or full series) into a clean UTF-8 text file. It automates the heavy lifting—authenticating with your Pixiv cookie, enumerating every chapter in a series, throttling and retrying HTTP calls with Polly, and formatting the final manuscript into a reader-friendly document.

## Features

- Full-series crawler that resolves every chapter via Pixiv's AJAX endpoints.
- Single-chapter download path for ad-hoc grabs.
- Configurable concurrency, timeout, and inter-request delays to respect Pixiv rate limits.
- Cookie-aware HTTP client with automatic decompression and resilient retries.
- Output normalization that trims whitespace and inserts chapter headers in Traditional Chinese typography.
- Portable scripts (`build.bat`, `run.bat`, `restore.bat`) for Windows users; native `dotnet` CLI support on macOS/Linux.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A Pixiv account cookie (copy the value of the `PHPSESSID` cookie after logging in)
- Windows Terminal or any shell that can run `dotnet` commands

## Getting Started

1. **Install dependencies**
   ```bash
   dotnet restore
   ```
2. **Copy the configuration template**
   ```bash
   copy appsettings.example.jsonc appsettings.jsonc
   ```
   Edit `appsettings.jsonc` and fill in your Pixiv cookie plus any custom limits.
3. **Run the CLI**
   ```bash
   dotnet run --project Pixiv.csproj
   ```
   When prompted, paste a Pixiv novel URL such as `https://www.pixiv.net/novel/series/11713692`.

The resulting `.txt` file is written to the folder specified by `OutputDir` (defaults to `data/`).

## Configuration

| Key              | Default | Description |
|------------------|---------|-------------|
| `DefaultTimeoutMs` | 15000 | Per-request timeout in milliseconds. |
| `Concurrency`    | 5       | Maximum parallel chapter downloads. |
| `OutputDir`      | `data`  | Folder where `.txt` exports are written. |
| `SortChapters`   | `false` | Reserved for future custom sorting (currently passthrough). |
| `RequestDelayMs` | 1000    | Delay injected between requests to avoid throttling. |
| `Cookie`         | `""`    | Pixiv cookie header; set to your `PHPSESSID` to access locked content. |

## Project Overview

The application follows a classic layered console layout:
- The entry point (`Program.cs`) wires up logging, configuration loading, and the interactive prompt.
- Service classes handle network access, scraping Pixiv metadata, extracting chapter bodies, and writing output files with concurrency control.
- Lightweight model records capture Pixiv API responses, while a single utility manages JSONC configuration and defaults.

This keeps the codebase small and approachable while mirroring patterns used in larger .NET CLI projects.

## Usage Notes

- The tool auto-detects whether a URL represents a single chapter (`/novel/show.php?id=...`) or an entire series (`/novel/series/...`).
- Each chapter is fetched via `https://www.pixiv.net/ajax/novel/{id}` with the provided cookie and referer.
- Logs are emitted via Serilog; set the console log level inside `Program.cs` if you need more/less verbosity.
- Use `build.bat` / `run.bat` for scripted workflows, or `dotnet publish -c Release` to generate a standalone binary.

## Troubleshooting

- **HTTP 429 or repeated retries**: Increase `RequestDelayMs` and/or lower `Concurrency`.
- **Empty output file**: Ensure the cookie is valid and the novel isn't private.
- **Garbled characters**: The app writes UTF-8 without BOM; open the file with an editor that supports UTF-8 (VS Code, Notepad++, etc.).

## License

No explicit license file is included yet. If you plan to share the code publicly, consider adding an MIT, Apache-2.0, or other license that matches your intentions.

