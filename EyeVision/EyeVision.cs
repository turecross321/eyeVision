using System.Diagnostics;
using EyeVision.Configuration;
using Microsoft.Extensions.Logging;

namespace EyeVision;

public class EyeVision : IDisposable
{
    private readonly CameraRecorder? _recorder;
    
    private bool _recording = false;
    public bool IsRecording() => _recording;

    private readonly ILogger _logger;
    private readonly EyeVisionConfiguration _configuration;
    private CancellationTokenSource? _cancellationTokenSource;

    public event EventHandler<bool>? Warning; 
    public event EventHandler<CameraRecorder>? RecordingActivity; 

    public EyeVision(ILogger logger, EyeVisionConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _recorder = new(this, logger, configuration.Camera, configuration.VideoEncoder, configuration.AudioEncoder);
    }
    
    public async void StartRecording()
    {
        if (IsRecording())
        {
            _logger.LogWarning("Attempted to start recording while one was already active");
            return;
        }

        if (_recorder == null)
        {
            _logger.LogError("Attempted to start recording while recorder was null");
            InvokeWarning();
            return;
        }
        
        _logger.LogInformation("Starting recording");
        _recording = true;
        _cancellationTokenSource = new CancellationTokenSource();
        
        _recorder.StartRecording(_configuration.VideosPath, _cancellationTokenSource.Token);
        _logger.LogInformation("Started recording");
    }

    public async void StopRecording()
    {
        if (!IsRecording())
        {
            _logger.LogWarning("Attempted to stop recording while one wasn't active");
            return;
        }
        
        _logger.LogInformation("Stopping recording");
        _recording = false;
        await _cancellationTokenSource!.CancelAsync();
        
        _logger.LogInformation("Stopped recording");
    }

    /// <summary>
    /// Signal to listeners that something has gone wrong
    /// </summary>
    public void InvokeWarning()
    {
        Warning?.Invoke(this, true);
    }
    
    /// <summary>
    /// Signal to listeners about video recording updates
    /// </summary>
    public void InvokeRecordingActivity(CameraRecorder recorder)
    {
        RecordingActivity?.Invoke(this, recorder);
    }

    public void Dispose()
    {
        if (_recording)
            StopRecording();
    }
}