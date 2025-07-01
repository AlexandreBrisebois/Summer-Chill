# FGLair Control - .NET Worker Service

A .NET Worker Service application that automatically controls Fujitsu General air conditioner louver positions via the FGLair API. The service runs continuously in the background, cycling between louver positions at configurable intervals to improve air distribution.

> 📚 **Documentation Index**: [CONTRIBUTING.md](./docs/CONTRIBUTING.md) | [DEPLOYMENT.md](./docs/DEPLOYMENT.md) | [TROUBLESHOOTING.md](./docs/TROUBLESHOOTING.md) | [API.md](./docs/API.md) | [SECURITY.md](./docs/SECURITY.md)

## Quick Start

### Prerequisites
- .NET 9 SDK
- FGLair account credentials  
- Air conditioner device DSN (Device Serial Number)

> ⚠️ **Security Note**: Never commit credentials to version control. See [SECURITY.md](./docs/SECURITY.md) for best practices.

### Running Locally
```bash
# Clone and navigate to project
git clone <repository-url>
cd Summer-Chill

# Copy configuration template
cp appsettings.template.json appsettings.json

# Configure your credentials in appsettings.json
# Then run the service
dotnet run
```

### Running with Docker
```bash
# Edit docker-compose.yml with your credentials
# Then start the service
docker-compose up -d
```

> 📖 **Need Help?** Check our [Troubleshooting Guide](./docs/TROUBLESHOOTING.md) for common issues and solutions.

## How It Works

The service automatically cycles your air conditioner's vertical louver between positions 7 and 8 every 20 minutes. This provides better air distribution without manual intervention.

**Operation Flow:**
1. Authenticates with FGLair API
2. Reads current louver position
3. Sets louver to position 7 or 8 (alternating)
4. Waits 20 minutes
5. Repeats the cycle

## Project Architecture

### Core Components

```
FGLairControl/
├── Program.cs                     # Application entry point and DI setup
├── Worker.cs                      # Main background service (louver cycling logic)
├── FGLairSettings.cs             # Configuration model with validation
├── Services/
│   ├── IFGLairClient.cs          # API client interface
│   ├── FGLairClient.cs           # HTTP client for FGLair API
│   └── DeviceInfo.cs             # Device discovery and management
├── Models/                        # Data transfer objects
│   ├── AuthenticationModels.cs   # Login/token models
│   └── DeviceModels.cs           # Device property models
└── FGLairDebugger.cs             # Interactive debugging utility
```

### Key Design Decisions

- **BackgroundService**: Ensures proper .NET hosting lifecycle management
- **Dependency Injection**: Makes components testable and loosely coupled
- **HttpClient Factory**: Provides proper connection pooling and disposal
- **Structured Logging**: Enables monitoring and troubleshooting
- **Configuration Pattern**: Supports both development and production environments

## Configuration

### Required Settings

| Setting | Description | Example |
|---------|-------------|---------|
| `FGLair:Username` | Your FGLair account email | `user@example.com` |
| `FGLair:Password` | Your FGLair account password | `your-password` |
| `FGLair:DeviceDsn` | Device serial number from FGLair app | `DSXXXXXXXXXXXXXXXX` |

### Finding Your Device DSN

1. Open the FGLair mobile app
2. Go to your device settings
3. Look for "Device Information" or "DSN"
4. Copy the full DSN string (usually starts with "DS")

### Configuration Methods

#### Method 1: appsettings.json (Development)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "FGLair": {
    "Username": "your-email@example.com",
    "Password": "your-password",
    "DeviceDsn": "DSXXXXXXXXXXXXXXXX"
  }
}
```

#### Method 2: Environment Variables (Production/Docker)
```bash
# Linux/Mac
export FGLair__Username="your-email@example.com"
export FGLair__Password="your-password"
export FGLair__DeviceDsn="DSXXXXXXXXXXXXXXXX"

# Windows
set FGLair__Username=your-email@example.com
set FGLair__Password=your-password
set FGLair__DeviceDsn=DSXXXXXXXXXXXXXXXX
```

#### Method 3: Docker Compose (Recommended for Production)
```yaml
services:
  fglaircontrol:
    image: fglaircontrol
    environment:
      - FGLair__Username=your-email@example.com
      - FGLair__Password=your-password
      - FGLair__DeviceDsn=DSXXXXXXXXXXXXXXXX
```

## Deployment Options

### Local Development

#### Standard Operation
```bash
# Run the worker service
dotnet run

# View logs in real-time
dotnet run --verbosity detailed
```

#### Debug Mode (Interactive Testing)
```bash
# Run interactive debugging session
dotnet run --debug
```

Debug mode provides:
- Manual API testing
- Device property inspection
- Authentication verification
- Real-time API response analysis

### Docker Deployment

#### Build and Run
```bash
# Build the container
docker build -t fglaircontrol .

# Run with environment variables
docker run -d \
  --name fglaircontrol \
  --restart unless-stopped \
  -e FGLair__Username=your-email@example.com \
  -e FGLair__Password=your-password \
  -e FGLair__DeviceDsn=DSXXXXXXXXXXXXXXXX \
  fglaircontrol
```

#### Docker Compose (Recommended)
```bash
# Start the service
docker-compose up -d

# View logs
docker-compose logs -f

# Stop the service
docker-compose down
```

## Louver Positions Reference

| Position | Description |
|----------|-------------|
| 0 | Auto (system controlled) |
| 1 | Highest position |
| 2-4 | Middle positions |
| 5 | Lowest position |
| 6-8 | Lower positions |

**Current Behavior**: The service cycles between positions 7 and 8 every 20 minutes.

## Monitoring and Logs

### Log Levels
- **Information**: Normal operation (position changes, API calls)
- **Warning**: Recoverable issues (temporary API failures)
- **Error**: Serious problems (authentication failures, configuration errors)

### Key Log Messages
- `Worker running at: {timestamp}` - Service is healthy
- `Successfully authenticated with FGLair API` - API connection established
- `Current louver position: {position}` - Position status
- `Setting louver to position {position}` - Position change initiated

### Monitoring Commands
```bash
# Docker logs
docker logs -f fglaircontrol

# Docker Compose logs
docker-compose logs -f fglaircontrol

# Local development logs
dotnet run --verbosity detailed
```

## Troubleshooting

### Common Issues

#### Authentication Failures
**Symptoms**: `Authentication failed` errors in logs
**Solutions**:
1. Verify username/password in FGLair mobile app
2. Check for typos in configuration
3. Ensure account is active and not locked

#### Device Not Found
**Symptoms**: `Device not found` or `DSN invalid` errors
**Solutions**:
1. Verify DSN in FGLair mobile app
2. Ensure device is online and connected
3. Check DSN format (should start with "DS")

#### Network Connectivity
**Symptoms**: `HTTP request failed` or timeout errors
**Solutions**:
1. Check internet connection
2. Verify firewall settings
3. Test API endpoints manually using `.http/rest.http`

#### Service Stops Unexpectedly
**Symptoms**: Container exits or process terminates
**Solutions**:
1. Check logs for error messages
2. Verify configuration is complete
3. Ensure sufficient system resources

### Debug Tools

#### Interactive Debugging
```bash
# Run debug mode for API testing
dotnet run --debug
```

#### API Testing
Use the `.http/rest.http` file with VS Code REST Client extension to test endpoints manually.

#### Health Checks
```bash
# Check if service is running (Docker)
docker ps | grep fglaircontrol

# Check service logs
docker logs fglaircontrol --tail 50
```

## Maintenance

### Regular Tasks
- **Monitor logs** for authentication or API errors
- **Update credentials** if password changes
- **Restart service** if behavior seems abnormal
- **Check device connectivity** if commands fail

### Updates
```bash
# Update Docker image
docker-compose pull
docker-compose up -d

# Update local development
git pull
dotnet restore
dotnet run
```

### Backup Configuration
Keep a backup of your configuration:
- `appsettings.json` (remove sensitive data)
- `docker-compose.yml`
- Environment variable documentation

## Documentation

### For Users
- 🚀 **[Quick Start Guide](./docs/QUICK_START.md)** - Get up and running in 5 minutes
- 🔧 **[Configuration Guide](./docs/CONFIGURATION.md)** - Complete configuration options
- 🐳 **[Deployment Guide](./docs/DEPLOYMENT.md)** - Production deployment scenarios
- 🔍 **[Troubleshooting](./docs/TROUBLESHOOTING.md)** - Common issues and solutions

### For Developers  
- 👥 **[Contributing Guide](./docs/CONTRIBUTING.md)** - How to contribute to this project
- 🏗️ **[Architecture Guide](./docs/ARCHITECTURE.md)** - Technical architecture and design decisions
- 🧪 **[Testing Guide](./docs/TESTING.md)** - Testing strategies and tools
- 📋 **[API Reference](./docs/API.md)** - Complete API documentation

### For Operations
- 🔒 **[Security Guide](./docs/SECURITY.md)** - Security best practices and considerations
- 📊 **[Monitoring Guide](./docs/MONITORING.md)** - Monitoring and observability setup
- 🔄 **[Maintenance Guide](./docs/MAINTENANCE.md)** - Regular maintenance tasks

## License

[Add your license information here]

## Support

For issues or questions:
1. Check the troubleshooting section above
2. Review logs for error messages
3. Test API connectivity manually
4. Create an issue with full error details and configuration (remove sensitive data)