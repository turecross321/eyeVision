using Microsoft.Extensions.Logging;

namespace EyeVision.Configuration;

public class EyeVisionConfiguration
{
    public CameraConfiguration Camera { get; init; } = new CameraConfiguration();
    public string VideosPath { get; init; } = Path.Combine(Environment.CurrentDirectory, "videos/");
    public string VideoEncoder { get; init; } = "h264_v4l2m2m";
    public string AudioEncoder { get; init; } = "aac";
    public bool UseCliController { get; init; } = true;
    public bool UseGpioController { get; init; } = false;
    
    public GpioPinsConfiguration GpioPins { get; init; } = new GpioPinsConfiguration();

    private static string FilePath => Path.Combine(Directory.GetCurrentDirectory(), "config.json");

    public static EyeVisionConfiguration LoadOrCreate(ILogger logger)
    {
        EyeVisionConfiguration? configuration = LoadFromFile(logger, FilePath);

        if (configuration != null)
            return configuration;
        
        configuration = new EyeVisionConfiguration();
        logger.LogInformation("Configuration file could not be loaded. Using default configuration.");
        SaveToFile(logger, FilePath, configuration);

        return configuration;
    }
    
    private static EyeVisionConfiguration? LoadFromFile(ILogger logger, string filePath)
    {
        logger.LogInformation($"Loading configuration from {filePath}.");
        
        if (!File.Exists(filePath))
        {
            logger.LogWarning("Configuration file could not be found.");
            return null;
        }

        string json = File.ReadAllText(filePath);
        return CrazyJsonSerializer.Deserialize<EyeVisionConfiguration>(json);
    }

    private static void SaveToFile(ILogger logger, string filePath, EyeVisionConfiguration dashCamConfiguration)
    {
        logger.LogInformation($"Saving configuration to {filePath}");
        string json = CrazyJsonSerializer.Serialize(dashCamConfiguration);
        File.WriteAllText(filePath, json);
    }
}