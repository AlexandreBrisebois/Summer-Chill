# Changelog

All notable changes to this project will be documented in this file.

## [Unreleased]

### Added
- `LouverPositions` is now a first-class property on `FGLairSettings`, allowing it to be set via `appsettings.json` or environment variables
- Typed request models (`LoginRequest`, `RefreshTokenRequest`, `RefreshCredentials`, `DatapointRequest`) replace anonymous objects in `FGLairClient`
- `Worker` now uses `IOptions<FGLairSettings>` for strongly-typed configuration instead of raw `IConfiguration`
- Retry delay constant (`RetryDelayMinutes`) added to `Worker` for clearer retry behaviour
- Docker image now runs as the non-root `app` user for improved container security
- `FGLairDebugger` accepts `FGLairSettings` directly so debug mode reads configuration through the normal DI/configuration pipeline

### Changed
- Target framework upgraded from .NET 9 to .NET 10
- `Microsoft.Extensions.Hosting` and `Microsoft.Extensions.Http` bumped to `10.0.0`
- Dockerfile base and SDK images updated to `mcr.microsoft.com/dotnet/runtime:10.0-noble-chiseled` and `mcr.microsoft.com/dotnet/sdk:10.0`
- `dotnet publish` now runs with `--no-restore` to avoid redundant package restores in CI
- CI workflow updated to use `dotnet-version: '10.0.x'`
- Worker loop refactored from a single `try/catch` to a `while` loop with per-iteration error handling and token-aware retry delays

## [1.0.0] - Initial Release

### Added
- Initial release
- Automatic louver position cycling
- Docker support
- Configuration templates
