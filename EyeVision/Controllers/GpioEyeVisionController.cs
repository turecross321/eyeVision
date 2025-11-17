using System.Device.Gpio;
using EyeVision.Configuration;
using Microsoft.Extensions.Logging;

namespace EyeVision.Controllers;

public class GpioEyeVisionController : EyeVisionController, IDisposable
{
    private readonly GpioController _gpioController;
    private readonly EyeVisionConfiguration _configuration;
    private DateTime? _lastToggleButtonPress = null;
    
    public GpioEyeVisionController(ILogger logger, EyeVision cam, EyeVisionConfiguration configuration) : base(logger, cam)
    {
        _gpioController = new GpioController();
        _configuration = configuration;

        _gpioController.OpenPin(_configuration.GpioPins.WarningLedPin, PinMode.Output);
        _gpioController.OpenPin(_configuration.GpioPins.RunningLedPin, PinMode.Output);
        _gpioController.OpenPin(_configuration.GpioPins.RecordingLedPin, PinMode.Output);

        WriteAllLeds(false);
        
        _gpioController.OpenPin(_configuration.GpioPins.ToggleRecordingPin, PinMode.InputPullUp);
        _gpioController.RegisterCallbackForPinValueChangedEvent(_configuration.GpioPins.ToggleRecordingPin, PinEventTypes.Falling, OnToggleRecording);
        
        _gpioController.Write(_configuration.GpioPins.RunningLedPin, true);
    }

    private void OnToggleRecording(object sender, PinValueChangedEventArgs pinvaluechangedeventargs)
    {
        DateTime now = DateTime.Now;

        if (_lastToggleButtonPress != null && now.Subtract(_lastToggleButtonPress.Value).TotalMilliseconds <
            _configuration.GpioPins.ButtonDebounceTimeoutMs)
            return;
        
        Logger.LogInformation("Toggling recording");
        _lastToggleButtonPress = DateTime.Now;
        
        ToggleRecording();
        
    }

    private void WriteAllLeds(bool value)
    {
        Logger.LogInformation("Writing all LEDs {value}", value);
        
        _gpioController.Write(_configuration.GpioPins.WarningLedPin, value);
        _gpioController.Write(_configuration.GpioPins.RunningLedPin, value);
        _gpioController.Write(_configuration.GpioPins.RecordingLedPin, value);
    }


    protected override void CamOnRecordingActivity(object? sender, CameraRecorder recorder)
    {
        int gpioPin = _configuration.GpioPins.RecordingLedPin;
        _gpioController.Write(gpioPin, recorder.Recording);
    }

    protected override void CamOnWarning(object? sender, bool value)
    {
        _gpioController.Write(_configuration.GpioPins.WarningLedPin, value);
    }
    
    
    public override void Dispose()
    {
        Logger.LogInformation("Disposing " + nameof(GpioEyeVisionController));

        WriteAllLeds(false);
        
        _gpioController.Write(_configuration.GpioPins.RecordingLedPin, false);
        
        _gpioController.Dispose();
        
        GC.SuppressFinalize(this);
    }
}