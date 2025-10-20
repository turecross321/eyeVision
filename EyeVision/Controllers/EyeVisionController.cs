using Microsoft.Extensions.Logging;

namespace EyeVision.Controllers;

public abstract class EyeVisionController : IDisposable
{
    protected readonly ILogger Logger;
    private readonly EyeVision _cam;

    protected EyeVisionController(ILogger logger, EyeVision cam)
    {
        Logger = logger;
        _cam = cam;
        
        _cam.Warning += CamOnWarning;
        _cam.RecordingActivity += CamOnRecordingActivity;
    }

    protected virtual void CamOnRecordingActivity(object? sender, CameraRecorder recorder)
    {

    }

    protected virtual void CamOnObdActivity(object? sender, bool value)
    {
        
    }

    protected virtual void CamOnWarning(object? sender, bool value)
    {
        
    }

    protected void StopRecording()
    {
        if (!_cam.IsRecording())
            return;
        
        _cam.StopRecording();
    }

    protected void StartRecording()
    {
        if (_cam.IsRecording())
            return;
        
        _cam.StartRecording();
    }

    public virtual void Dispose()
    {
        _cam.Warning -= CamOnWarning;
        _cam.RecordingActivity -= CamOnRecordingActivity;
        
        _cam.Dispose();
        
        GC.SuppressFinalize(this);
    }
}