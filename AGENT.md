# AI Agent Guidelines for Pixiv Novel Archivist

## Project Overview

**Pixiv Novel Archivist** is a .NET 9.0 console application that downloads and archives Pixiv novels (Japanese web novels) into clean UTF-8 text files. It supports both single-chapter downloads and full series archival with concurrent, rate-limited HTTP requests.

### Purpose
- Fetch novel content from Pixiv via their AJAX API endpoints
- Handle authentication via Pixiv session cookies
- Process both individual chapters and complete series
- Generate well-formatted, readable text files
- Respect rate limits with configurable throttling

## Technical Stack

### Core Technologies
- **Runtime**: .NET 9.0
- **Language**: C# 13 with nullable reference types enabled
- **Logging**: Serilog (Console sink)
- **HTTP Resilience**: Polly (retry policies)
- **HTML Parsing**: HtmlAgilityPack
- **User Agent Generation**: UserAgentGenerator

### JSON Serialization
- **Uses JSON Source Generator** for AOT and trimming support
- All serialization goes through `PixivJsonContext` (defined in `src/Utils/JsonContext.cs`)
- Configuration files support JSONC format (comments and trailing commas)
- Important: When adding new models, register them in `JsonContext.cs` with `[JsonSerializable]` attributes

## Project Structure

```
Pixiv/
├── src/
│   ├── Models/              # Data models for API responses
│   │   ├── BookInfo.cs      # Represents a complete novel/series
│   │   ├── ChapterItem.cs   # Individual chapter metadata
│   │   ├── NovelContentResponse.cs   # API response for chapter content
│   │   └── SeriesContentResponse.cs  # API response for series listing
│   ├── Services/            # Business logic layer
│   │   ├── BookScraper.cs   # Scrapes series metadata from HTML
│   │   ├── ChapterExtractor.cs  # Fetches chapter content via API
│   │   ├── Downloader.cs    # Orchestrates parallel downloads
│   │   └── HttpFetcher.cs   # HTTP client wrapper with retry logic
│   ├── Utils/               # Utility classes
│   │   ├── ConfigLoader.cs  # JSONC configuration loader
│   │   └── JsonContext.cs   # JSON Source Generator context
│   └── Program.cs           # Application entry point
├── .github/workflows/
│   └── release.yml          # CI/CD for multi-platform releases
├── appsettings.example.jsonc  # Configuration template
└── Pixiv.csproj             # Project file
```

## Architecture Patterns

### Layered Design
1. **Presentation Layer**: `Program.cs` - CLI interface and user interaction
2. **Service Layer**: `Services/*` - Business logic, HTTP calls, data processing
3. **Data Layer**: `Models/*` - DTOs and domain models
4. **Utility Layer**: `Utils/*` - Cross-cutting concerns (config, JSON)

### Key Design Decisions
- **Primary constructor syntax** for dependency injection
- **Record types** for immutable data models
- **Async/await** throughout for I/O operations
- **Polly resilience** for network fault tolerance
- **Concurrent downloads** with SemaphoreSlim for rate limiting

## Configuration System

### Files
- `appsettings.jsonc` (user config, not committed)
- `appsettings.example.jsonc` (template, committed)

### Key Settings
```jsonc
{
  "DefaultTimeoutMs": 15000,    // HTTP timeout
  "Concurrency": 10,             // Parallel download limit
  "OutputDir": "data",           // Output directory
  "Cookie": "",                  // Pixiv PHPSESSID cookie
  "RequestDelayMs": 500          // Delay between requests
}
```

## Development Guidelines

### When Adding Features

1. **New API Models**: 
   - Create record in `src/Models/`
   - Register in `JsonContext.cs` with `[JsonSerializable(typeof(YourModel))]`
   - Use nullable reference types appropriately

2. **New Services**:
   - Place in `src/Services/`
   - Use primary constructor for dependencies
   - Make classes `sealed` when not intended for inheritance
   - Use `ILogger` or `Serilog.Log` for logging

3. **Configuration Changes**:
   - Update both `appsettings.jsonc` and `appsettings.example.jsonc`
   - Add getter in `ConfigLoader.cs` if needed
   - Document in README.md

### Code Style
- Use modern C# features (records, primary constructors, pattern matching)
- Prefer `var` for local variables with obvious types
- Use string interpolation over concatenation
- Follow async best practices (ConfigureAwait is not needed in console apps)
- Traditional Chinese for user-facing messages
- English for code comments and documentation

### Testing Locally
```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run

# Publish single-file executable
dotnet publish -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:PublishTrimmed=true
```

## CI/CD Pipeline

### GitHub Actions Workflow
- **Trigger**: Push tags starting with `v` (e.g., `v1.0.0`) or manual dispatch
- **Platforms**: Windows x64, Linux x64, macOS x64, macOS ARM64
- **Output**: Single-file executables with bundled .NET runtime
- **Optimization**: Code trimming enabled, ~17MB per platform
- **Release**: Automatically creates GitHub Release with all binaries

### Creating a Release
```bash
git tag v1.x.x -m "Release description"
git push origin v1.x.x
```

## Common Tasks

### Adding a New Dependency
1. Add to `Pixiv.csproj`: `<PackageReference Include="PackageName" Version="x.y.z" />`
2. Run `dotnet restore`
3. Commit both `.csproj` and `obj/project.assets.json` changes

### Handling New Pixiv API Endpoints
1. Inspect API response in browser DevTools
2. Create model in `src/Models/` matching the JSON structure
3. Register model in `JsonContext.cs`
4. Add method in appropriate service class
5. Use `PixivJsonContext.Default.YourModel` for deserialization

### Modifying HTTP Behavior
- **Timeout**: Adjust in `HttpFetcher` constructor
- **Retry Policy**: Modify in `HttpFetcher.cs`
- **User Agent**: Generated dynamically in `Program.cs`
- **Cookie**: Configured in `appsettings.jsonc`

## Troubleshooting

### Build Issues
- **"PixivJsonContext does not exist"**: Run `dotnet clean` then `dotnet build` (Source Generator needs to run)
- **Trimming warnings (IL2026)**: Expected for generic deserialization in `ConfigLoader.TryGet<T>` - already suppressed

### Runtime Issues
- **JSON parse errors**: Check `JsonContext.cs` has all required models registered
- **HTTP 429 errors**: Increase `RequestDelayMs` or decrease `Concurrency`
- **Empty output**: Verify Pixiv cookie is valid and not expired

## Important Notes

1. **JSON Source Generator**: Always use `JsonSerializer.Deserialize(json, PixivJsonContext.Default.YourType)` instead of generic `Deserialize<T>()`
2. **JSONC Support**: Configuration files support comments and trailing commas (configured in `JsonSourceGenerationOptions`)
3. **Cookie Authentication**: Required for accessing most Pixiv content
4. **Rate Limiting**: Critical to avoid IP bans - always test with conservative settings first
5. **Encoding**: All output is UTF-8 without BOM

## Git Commit Convention

Use conventional commit format:
```
<type>: <subject>

<body>
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Adding tests
- `chore`: Build process or auxiliary tool changes
- `ci`: CI/CD changes

Example:
```
feat: Add retry mechanism for HTTP 502 errors

Implement exponential backoff when encountering 502 Bad Gateway
responses from Pixiv API. Configurable max retry attempts.
```

## Useful Resources

- [Pixiv API Documentation](https://www.pixiv.net/ajax/) (unofficial)
- [.NET 9 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)
- [JSON Source Generator](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation)
- [Polly Documentation](https://www.thepollyproject.org/)

## Contact & Support

This is a personal archival tool. When helping with this project:
- Prioritize code quality and maintainability
- Keep the codebase simple and readable
- Test thoroughly before committing
- Use English for all commit messages and documentation
- Use Traditional Chinese for user-facing error messages and prompts

