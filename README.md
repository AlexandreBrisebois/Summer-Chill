# FGLair Control

A .NET Worker Service that automatically controls Fujitsu General air conditioner louver positions. The service cycles between positions 7 and 8 every 20 minutes for better air distribution.

## Quick Start

### Prerequisites
- .NET 9 SDK
- FGLair account credentials  
- Device DSN from FGLair mobile app

### Setup
```bash
# Clone and configure
git clone <repository-url>
cd Summer-Chill
cp appsettings.template.json appsettings.json

# Edit appsettings.json with your credentials
dotnet run
```

### Docker
```bash
# Edit docker-compose.yml with your credentials
docker-compose up -d
```

## Configuration

Add your FGLair credentials to `appsettings.json`:
```json
{
  "FGLair": {
    "Username": "your-email@example.com",
    "Password": "your-password",
    "DeviceDsn": "DSXXXXXXXXXXXXXXXX"
  }
}
```

**Finding your DSN**: Open FGLair app → Device Settings → Device Information

**Production**: Use environment variables instead:
```bash
export FGLair__Username="your-email@example.com"
export FGLair__Password="your-password"
export FGLair__DeviceDsn="DSXXXXXXXXXXXXXXXX"
```

## Troubleshooting

**Authentication failed**: Verify credentials in FGLair app
**Device not found**: Check DSN format (starts with "DS")
**Service stops**: Check logs with `docker logs fglaircontrol`

## Development

See [CONTRIBUTING.md](./docs/CONTRIBUTING.md) for development setup and guidelines.

## Security

⚠️ **Never commit credentials to version control.** Use environment variables in production. See [SECURITY.md](./docs/SECURITY.md) for details.