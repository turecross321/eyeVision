namespace EyeVision.Configuration;

public class GpioPinsConfiguration
{
    public int WarningLedPin { get; set; } = 19;
    public int RunningLedPin { get; set; } = 26;
    public int RecordingLedPin { get; set; } = 22;
    public int ToggleRecordingPin { get; set; } = 13;
}