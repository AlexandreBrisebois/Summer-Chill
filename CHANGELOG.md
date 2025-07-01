# Changelog

All notable changes to the FGLair Control project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Comprehensive documentation structure
- Configuration templates for easy setup
- Security guidelines and best practices
- Architecture documentation
- Contributing guidelines

### Changed
- Improved README structure and navigation
- Enhanced configuration management

## [1.0.0] - 2024-01-XX

### Added
- Initial release of FGLair Control Worker Service
- Automatic louver position cycling (positions 7 and 8)
- FGLair API integration with authentication
- Docker containerization support
- Docker Compose orchestration
- Structured logging with console output
- Configuration via appsettings.json and environment variables
- Debug mode for API testing
- Continuous background operation with 20-minute intervals

### Features
- **Authentication**: Secure FGLair API authentication
- **Louver Control**: Automatic position cycling every 20 minutes
- **Monitoring**: Comprehensive logging and error handling
- **Deployment**: Multiple deployment options (local, Docker, Docker Compose)
- **Configuration**: Flexible configuration management
- **Debug Tools**: Interactive debugging and API testing

### Technical Implementation
- .NET 9 Worker Service architecture
- Dependency injection with IServiceCollection
- HttpClient with proper configuration and disposal
- BackgroundService for continuous operation
- Structured exception handling and retry logic
- Container-ready with health checks

### Security
- Environment variable credential management
- HTTPS-only API communication
- Automatic credential scrubbing in logs
- No hardcoded secrets

### Documentation
- Complete README with setup instructions
- API endpoint documentation
- Docker deployment guides
- Troubleshooting guides
- Configuration examples

---

## Legend

- **Added** for new features
- **Changed** for changes in existing functionality
- **Deprecated** for soon-to-be removed features
- **Removed** for now removed features
- **Fixed** for any bug fixes
- **Security** for vulnerability fixes
