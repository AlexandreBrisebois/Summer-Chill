# FGLair Control - .NET Worker Service

A .NET Worker Service application to control Fujitsu General air conditioner louver position via the FGLair API. The service runs continuously in the background, monitoring and adjusting the louver position at configurable intervals.

## Architecture

This is a .NET 9 Worker Service that:
- Uses `BackgroundService` for continuous operation
- Implements dependency injection with `IServiceCollection`
- Provides structured logging with `ILogger`
- Uses `HttpClient` with proper configuration
- Supports both interactive and containerized deployment

## Features

- **Authentication**: Secure authentication with FGLair API using app credentials
- **Louver Control**: Get and set vertical louver direction (positions 0-8)
- **Continuous Monitoring**: Periodic position checking and adjustment
- **Debug Mode**: Interactive debugging tool for API testing
- **Docker Support**: Full containerization with Docker and Docker Compose
- **Structured Logging**: Comprehensive logging with colored console output
- **Configuration Management**: Flexible configuration through appsettings.json or environment variables

## Project Structure
FGLairControl/
??? Program.cs                          # Application entry point
??? Worker.cs                           # Main background service
??? FGLairSettings.cs                   # Configuration model
??? FGLairControl/
?   ??? FGLairDebugger.cs              # Debug utility
?   ??? Services/
?       ??? IFGLairClient.cs           # Client interface
?       ??? FGLairClient.cs            # API client implementation
?       ??? DeviceInfo.cs              # Device information service
?       ??? Models/                     # API models
?           ??? AuthenticationModels.cs
?           ??? DeviceModels.cs
??? appsettings.json                    # Application configuration
??? Dockerfile                          # Container configuration
??? docker-compose.yml                  # Container orchestration
??? .http/rest.http                     # API testing examples
## Configuration

### Option 1: appsettings.json
Update `appsettings.json` with your credentials:
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
    "DeviceDsn": "your-device-dsn",
    "LouverPosition": "8",
    "CommandIntervalMinutes": 30
  }
}
### Option 2: Environment Variables (Docker)
Configure via environment variables (useful for Docker deployment):
FGLair__Username=your-email@example.com
FGLair__Password=your-password
FGLair__DeviceDsn=your-device-dsn
FGLair__LouverPosition=8
FGLair__CommandIntervalMinutes=30
### Louver Position Values

- `"0"` - Auto (system determines position)
- `"1"` - Position 1 (highest)
- `"2"` - Position 2
- `"3"` - Position 3
- `"4"` - Position 4
- `"5"` - Position 5 (lowest/down)
- `"6"` - Position 6
- `"7"` - Position 7
- `"8"` - Position 8

## Usage

### Local Development

#### Normal Operation (Worker Service)dotnet run
The service will:
1. Authenticate with the FGLair API
2. Check the current louver position
3. Set the desired louver position
4. Continue monitoring and adjusting at the configured interval

#### Debug Mode (Interactive Testing)dotnet run --debug
Debug mode provides:
- Interactive API testing
- Manual authentication verification
- Device property inspection
- Real-time API response analysis

### Docker Deployment

#### Build and Run with Docker# Build the image
docker build -t fglaircontrol .

# Run with environment variables
docker run -e FGLair__Username=your-email@example.com \
           -e FGLair__Password=your-password \
           -e FGLair__DeviceDsn=your-device-dsn \
           fglaircontrol
#### Docker Compose (Recommended)# Update docker-compose.yml with your credentials
# Then run:
docker-compose up -d
## API Integration

The application integrates with the FGLair API using these endpoints:

| Endpoint | Method | Purpose |
|----------|---------|---------|
| `/users/sign_in.json` | POST | User authentication |
| `/apiv1/dsns/{dsn}/properties` | GET | Get device properties |
| `/apiv1/dsns/{dsn}/properties/{property}/datapoints` | POST | Set device property |

### Authentication
Uses app-specific credentials:
- **App ID**: `CJIOSP-id`
- **App Secret**: `CJIOSP-Vb8MQL_lFiYQ7DKjN0eCFXznKZE`

## Dependencies

- **.NET 9**: Target framework
- **Microsoft.Extensions.Hosting**: Worker service framework
- **Microsoft.Extensions.Http**: HTTP client factory
- **Microsoft.VisualStudio.Azure.Containers.Tools.Targets**: Docker support

## Service Behavior

### Startup Sequence
1. **Configuration Loading**: Reads settings from appsettings.json or environment
2. **Service Registration**: Configures dependency injection
3. **Authentication**: Establishes session with FGLair API
4. **Initial Check**: Reads current louver position
5. **Command Execution**: Sets desired louver position
6. **Periodic Operation**: Continues monitoring at configured intervals

### Logging
The service provides structured logging with:
- **Information**: Normal operation status
- **Warning**: Non-critical issues
- **Error**: Failures and exceptions
- **Colored Console**: Visual indicators for key events

### Error Handling
- Automatic authentication retry
- Graceful degradation on API failures
- Comprehensive exception logging
- Service continuation on transient errors

## Development Notes

- The service uses `BackgroundService` for proper .NET hosting
- HTTP client is configured with appropriate headers and timeouts
- JSON serialization uses camelCase naming policy
- All API calls include proper cancellation token support
- Docker configuration follows .NET best practices

## Testing

Use the included `.http/rest.http` file with VS Code REST Client extension or similar tools to test API endpoints manually.