using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EyeVision.Controllers;

public class CliEyeVisionController : EyeVisionController, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    public CliEyeVisionController(ILogger logger, EyeVision cam) : base(logger, cam)
    {
        _ = MonitorKeyPressAsync(_cancellationTokenSource.Token);
    }
    
    private async Task MonitorKeyPressAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);
                
                Console.WriteLine(keyInfo.KeyChar);

                switch (keyInfo.Key)
                {
                    case ConsoleKey.R:
                        StartRecording();
                        break;
                    case ConsoleKey.T:
                        StopRecording();
                        break;
                    case ConsoleKey.Y:
                        PrintAmountOfFfmpegProccesses();
                        break;
                }
            }
            
            await Task.Delay(50, token);
        }
    }

    private void PrintAmountOfFfmpegProccesses()
    {
        var ffmpegs = Process.GetProcesses().Where(p => p.ProcessName.Contains("ffmpeg"));
        Logger.LogInformation("{count} instances of ffmpeg are running", ffmpegs.Count());
    }

    protected override void CamOnWarning(object? sender, bool value)
    {
        Console.WriteLine($"[WARNING]: {value}");
        
        base.CamOnWarning(sender, value);
    }
    
    public override void Dispose()
    {
        _cancellationTokenSource.Cancel();
        
        GC.SuppressFinalize(this);
    }
}