using System.Diagnostics;
using System.Runtime.InteropServices;
using EyeVision.Configuration;
using Microsoft.Extensions.Logging;

namespace EyeVision;

public class CameraRecorder(
    EyeVision eyeVision,
    ILogger logger,
    CameraConfiguration cameraConfig,
    string videoEncoder,
    string audioEncoder)
    : IDisposable
{
    public CameraConfiguration CameraConfig { get; } = cameraConfig;
    public bool Recording { get; private set; } = false;
    private Process? _process = null;

    private string VideoEncoder { get; } = videoEncoder;
    private string AudioEncoder { get; } = audioEncoder;
    public DateTimeOffset? StartDate { get; private set; }
    public string VideoFileName => StartDate?.ToString("yyyy-MM-dd_HH-mm-ss") + "." + cameraConfig.FileFormat;
    public string? CurrentTripDirectory { get; private set; }

    private CancellationToken? _recordingCancellationToken;

    private static string GetVideoFormat()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "v4l2";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "dshow";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "avfoundation";
        
        throw new Exception("Unsupported OS");
    }
    
    private static string GetAudioFormat()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "alsa"; // Most common for Linux
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "dshow";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "avfoundation";

        throw new Exception("Unsupported OS");
    }

    private static string GetFfmpegArguments(CameraConfiguration camera, string videoEncoder, string audioEncoder, string output)
    {
        string arguments = "";
        string videoFormat = GetVideoFormat();

        arguments += $" -framerate {camera.Fps}" +
                     $" -f {videoFormat}" +
                     $" -rtbufsize {camera.BufferSizeMb}M" +
                     $" -video_size {camera.ResolutionWidth}x{camera.ResolutionHeight}";

        if (videoFormat == "dshow")
        {
            arguments += $" -i video=\"{camera.DeviceName}\"";
        }
        else
            arguments += $" -i {camera.DeviceName}";

        if (camera.RecordAudio)
        {
            string audioFormat = GetAudioFormat();
            
            // if the video uses dshow, we combine the audio to the video input with a colon
            if (videoFormat == "dshow" && audioFormat == videoFormat)
                arguments += $":audio=\"{camera.AudioDevice}\"";
            else
                arguments += $" -f {GetAudioFormat()}" +
                         $" -i {camera.AudioDevice}";
        }

        arguments +=
            $" -c:v {videoEncoder}" +
            $" -preset veryfast" +
            $" -b:v {camera.VideoBitrateKbps}k" +
            $" -g 10" +
            $" -fps_mode vfr";

        if (camera.RecordAudio)
            arguments += $" -c:a {audioEncoder}" +
                         $" -b:a {camera.AudioBitrateKbps}k";

        arguments +=
            $" -vf format=yuv420p" +
            $" -movflags +faststart" +
            $" -async 1" + // Ensure audio and video are in sync
            $" -threads {camera.Threads}" +
            $" -y" +
            $" \"{output}\"";

        return arguments;
    }
    public void StartRecording(string directory, CancellationToken cancellationToken)
    {
        StartDate = DateTimeOffset.Now;
        CurrentTripDirectory = directory;
        string output = Path.Combine(directory, VideoFileName);
        logger.LogInformation("Starting recording for {device} at {output}", CameraConfig, output);
        
        var startInfo = new ProcessStartInfo 
        {
            FileName = "ffmpeg",
            Arguments = GetFfmpegArguments(CameraConfig, VideoEncoder, AudioEncoder, output),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        _process = Process.Start(startInfo);
        
        if (_process == null)
        {
            throw new InvalidOperationException($"Failed to start recording with {CameraConfig}. Is FFmpeg installed and added to PATH?");
        }
        
        _process.ErrorDataReceived += ProcessOnDataReceived;
        _process.OutputDataReceived += ProcessOnDataReceived;
        _process.Exited += ProcessOnExited;

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        _recordingCancellationToken = cancellationToken;
        _recordingCancellationToken.Value.Register(StopRecording);
    }

private void StopRecording()
{
    Recording = false;
    
    if (_process == null || _process.HasExited)
    {
        logger.LogWarning("Recording process is not running or has already exited.");
        eyeVision.InvokeWarning();
        return;
    }

    try
    {
        logger.LogInformation("Stopping recording for {device}", CameraConfig);

        // Send 'q' to ffmpeg to gracefully stop
        _process.StandardInput.WriteLine("q");
        _process.StandardInput.Flush();

        _process.WaitForExit(5000); // Wait for clean termination
        if (!_process.HasExited)
        {
            logger.LogWarning("Recording process did not stop cleanly. Forcing termination...");
            eyeVision.InvokeWarning();
            _process.Kill();
        }

        // Linux-specific flush to disk
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && CurrentTripDirectory != null && StartDate != null)
        {
            string tempPath = Path.Combine(CurrentTripDirectory, VideoFileName + ".tmp");
            string finalPath = Path.Combine(CurrentTripDirectory, VideoFileName);

            // rename the original ffmpeg output to temp file if needed
            if (File.Exists(finalPath))
                File.Move(finalPath, tempPath);

            int fd = NativeMethods.open(tempPath, NativeMethods.O_RDONLY);
            if (fd != -1)
            {
                NativeMethods.fsync(fd);
                NativeMethods.close(fd);
            }

            // sync directory
            fd = NativeMethods.open(CurrentTripDirectory, NativeMethods.O_RDONLY);
            if (fd != -1)
            {
                NativeMethods.fsync(fd);
                NativeMethods.close(fd);
            }

            // atomically rename temp -> final
            File.Move(tempPath, finalPath);

            // sync directory again to commit the rename
            fd = NativeMethods.open(CurrentTripDirectory, NativeMethods.O_RDONLY);
            if (fd != -1)
            {
                NativeMethods.fsync(fd);
                NativeMethods.close(fd);
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error stopping recording process for {device}", CameraConfig);
    }
    finally
    {
        _process?.Dispose();
        _process = null;
        eyeVision.InvokeRecordingActivity(this);
        StartDate = null;
        CurrentTripDirectory = null;
    }
}
    
    private void ProcessOnExited(object? sender, EventArgs e)
    {
        logger.LogInformation("Recorder process exited");
    }
    
    private void ProcessOnDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null)
            return;
        
        if (!_recordingCancellationToken!.Value.IsCancellationRequested && !Recording && e.Data.StartsWith("frame="))
        {
            Recording = true;
            eyeVision.InvokeRecordingActivity(this);
        }
        
        if (e.Data.Contains("Cannot open") || e.Data.Contains("Device or resource busy") || e.Data.Contains("error", StringComparison.InvariantCultureIgnoreCase))
        {
            logger.LogError("{data}", e.Data);
            eyeVision.InvokeWarning();
        }
        else
        {
            logger.LogDebug("{data}", e.Data);
        }
    }
    
    public void Dispose()
    {
        if (_process != null)
            StopRecording();
        
        _process?.Dispose();
    }
}