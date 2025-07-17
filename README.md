# FGLair Control

A .NET Worker Service that automatically controls Fujitsu General air conditioner louver positions and dynamically adjusts temperature based on outside weather conditions. The service cycles between louver positions every 20 minutes for better air distribution and automatically manages heat pump temperature during hot weather.

## Features

- **Automatic Louver Control**: Cycles between configured positions (default: 7,8) every 20 minutes
- **Weather-Based Temperature Control**: Dynamically adjusts heat pump temperature based on outside temperature
- **Smart Heat Management**: When outside temperature ≥ 30°C, ensures heat pump is not set more than 10°C cooler than outside temperature
- **Free Weather API**: Uses Open-Meteo API (no subscription required)
- **Configurable**: Easy to customize intervals, positions, and weather location

## Quick Start

### Prerequisites
- .NET 8+ SDK
- FGLair account credentials  
- Device DSN from FGLair mobile app
- Location coordinates (latitude/longitude) for weather data

### Setup
```bash
# Clone and configure
git clone <repository-url>
cd Summer-Chill
cp appsettings.template.json appsettings.json

# Edit appsettings.json with your credentials and location
dotnet run
```

### Docker
```bash
# Edit docker-compose.yml with your credentials
docker-compose up -d
```

## Configuration

Add your FGLair credentials and weather location to `appsettings.json`:
```json
{
  "FGLair": {
    "Username": "your-email@example.com",
    "Password": "your-password",
    "DeviceDsn": "DSXXXXXXXXXXXXXXXX",
    "LouverPositions": "7,8",
    "Interval": 20,
    "WeatherLatitude": 40.7128,
    "WeatherLongitude": -74.0060,
    "EnableWeatherControl": true
  }
}
```

### Weather Configuration

- **WeatherLatitude/WeatherLongitude**: Your location coordinates for weather data
- **EnableWeatherControl**: Set to `false` to disable weather-based temperature control
- Find your coordinates: Use any online coordinate finder or GPS app

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