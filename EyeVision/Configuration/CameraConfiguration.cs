namespace EyeVision.Configuration;

public record CameraConfiguration
{
    public string DeviceName { get; set; } = "/dev/video0";
    public int Fps { get; set; } = 30;
    public long VideoBitrateKbps { get; set; } = 1_500;
    public int ResolutionWidth { get; set; } = 640;
    public int ResolutionHeight { get; set; } = 480;
    public bool RecordAudio { get; set; } = false;
    public long AudioBitrateKbps { get; set; } = 128;
    public string? AudioDevice { get; set; } = "";
    public int Threads { get; set; } = 1;
    public int BufferSizeMb { get; set; } = 100;
}