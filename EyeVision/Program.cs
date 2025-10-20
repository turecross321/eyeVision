using EyeVision.Configuration;
using EyeVision.Controllers;
using Microsoft.Extensions.Logging;

bool verbose = args.Contains("-v") || args.Contains("--verbose");

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(verbose ? LogLevel.Trace : LogLevel.Information);
});
ILogger logger = loggerFactory.CreateLogger<Program>();

CancellationTokenSource programCancellationTokenSource = new();
Console.CancelKeyPress += (_, e) =>
{
    // Prevent the application from terminating immediately on CTRL + C
    e.Cancel = true;
    programCancellationTokenSource.Cancel();
};

EyeVisionConfiguration config = EyeVisionConfiguration.LoadOrCreate(logger);
using EyeVision.EyeVision cam = new(logger, config);

List<EyeVisionController> controllers = [];

if (config.UseCliController) controllers.Add(new CliEyeVisionController(logger, cam));
if (config.UseGpioController) controllers.Add(new GpioEyeVisionController(logger, cam, config));

foreach (var controller in controllers)
{
    logger.LogInformation("Using {type}", controller.GetType().Name);
}

try
{
    await Task.Delay(Timeout.Infinite, programCancellationTokenSource.Token);
}
catch (OperationCanceledException)
{
    logger.LogInformation("Quitting...");

    foreach (var controller in controllers)
    {
        controller.Dispose();
    }
}