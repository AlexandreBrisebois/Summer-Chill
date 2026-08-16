using FGLairControl;
using FGLairControl.Services;

var builder = Host.CreateApplicationBuilder(args);

// Check if debug mode is requested - done after builder so configuration (appsettings, user secrets) is available
if (args.Length > 0 && args[0].Equals("--debug", StringComparison.OrdinalIgnoreCase))
{
    Console.WriteLine("Running in debug mode...");
    var debugSettings = builder.Configuration.GetSection("FGLair").Get<FGLairSettings>() ?? new FGLairSettings();
    await FGLairDebugger.DebugApiAsync(debugSettings);
    return;
}

// Register HttpClient for FGLair API
builder.Services.AddHttpClient<IFGLairClient, FGLairClient>();

// Register HttpClient for Weather API
builder.Services.AddHttpClient<IWeatherService, WeatherService>();

// Configure FGLair settings from appsettings.json
builder.Services.Configure<FGLairSettings>(
    builder.Configuration.GetSection("FGLair"));

// Register the worker service
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
