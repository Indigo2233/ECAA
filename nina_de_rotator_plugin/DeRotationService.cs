using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;

namespace AltAzDeRotator
{
    public class DeRotationService
    {
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _backgroundTask;
        private NINA.Equipment.Interfaces.Mediator.ITelescopeMediator? _telescopeMediator;
        private NINA.Equipment.Interfaces.Mediator.IRotatorMediator? _rotatorMediator;
        private readonly DeRotationViewModel _viewModel;
        private double _accumulatedError; // accumulated derotation angle not yet applied

        public DeRotationService(NINA.Equipment.Interfaces.Mediator.ITelescopeMediator? telescopeMediator, NINA.Equipment.Interfaces.Mediator.IRotatorMediator? rotatorMediator, DeRotationViewModel viewModel)
        {
            _telescopeMediator = telescopeMediator;
            _rotatorMediator = rotatorMediator;
            _viewModel = viewModel;
        }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            if (_cancellationTokenSource != null)
            {
                _backgroundTask = Task.Run(() => BackgroundLoop(_cancellationTokenSource.Token));
                _viewModel.IsActive = true;
                _viewModel.Status = "Running";
            }
            Logger.Info("DeRotationService background loop started.");
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                // Wait briefly for the task to complete
                _backgroundTask?.Wait(TimeSpan.FromSeconds(2));
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                _viewModel.IsActive = false;
                _viewModel.Status = "Stopped";
                Logger.Info("DeRotationService background loop stopped.");
            }
        }

        private async Task BackgroundLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!_viewModel.IsDeRotationEnabled)
                    {
                        _viewModel.Status = "Disabled";
                        _viewModel.IsActive = false;
                        await Task.Delay(1000, token);
                        continue;
                    }

                    // Utilize mediators
                    if (_telescopeMediator != null && _rotatorMediator != null)
                    {
                        var telescope = _telescopeMediator?.GetDevice() as NINA.Equipment.Interfaces.ITelescope;
                        var rotatorDevice = _rotatorMediator?.GetDevice() as NINA.Equipment.Interfaces.IRotator;

                        if (telescope != null && telescope.Connected && rotatorDevice != null && rotatorDevice.Connected)
                        {
                            _viewModel.Status = "Running";
                            _viewModel.IsActive = true;
                            
                            // 1. Get current altitude and azimuth from the mount telemetry
                            double alt = 0.0;
                            double az = 0.0;
                            double lat = 40.0;

                            try
                            {
                                // The mount tracking altitude
                                alt = telescope.Altitude;
                                az = telescope.Azimuth;
                                // The user's geological location
                                lat = telescope.SiteLatitude;
                            }
                            catch (Exception ex)
                            {
                                Logger.Error($"Failed to fetch mount telemetry. Mount drivers might not expose Altitude: {ex.Message}");
                            }
                            
                            _viewModel.Altitude = alt;
                            _viewModel.Azimuth = az;

                            // 2. Calculate required rotation rate using our MathEngine
                            double requiredRateDegreesPerHour = MathEngine.CalculateRotationRate(alt, az, lat);
                            _viewModel.RotationRate = requiredRateDegreesPerHour;

                            // Calculate how many degrees we should move in this 1-second polling interval
                            double degreesPerSecond = requiredRateDegreesPerHour / 3600.0;
                            
                            // We can query position from the rotator mediator directly or its info
                            // Let's assume rotatorMediator has GetTargetPosition or similar, 
                            // but we can also use dynamic device object:
                            // We already verified the rotator is connected above
                            {
                                double currentRotatorPosition = 0;
                                try
                                {
                                    currentRotatorPosition = rotatorDevice.Position;
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error($"Failed to fetch rotator position: {ex.Message}");
                                    await Task.Delay(1000, token);
                                    continue;
                                }
                                
                                // Accumulate error over polling cycles so slow
                                // field rotation rates still get corrected.
                                _accumulatedError += degreesPerSecond;
                                
                                double targetAbsolute = (currentRotatorPosition + _accumulatedError) % 360.0;
                                if (targetAbsolute < 0) targetAbsolute += 360.0;
                                _viewModel.TargetPosition = targetAbsolute;

                                Logger.Info($"DEROT: pos={currentRotatorPosition:F4}° accum={_accumulatedError:F4}° target={targetAbsolute:F4}° rate={requiredRateDegreesPerHour:F2}°/hr");
                                
                                if (Math.Abs(_accumulatedError) > 0.01) 
                                {
                                    double applied = _accumulatedError;
                                    
                                    // Use N command (no-backlash) for smooth field rotation tracking
                                    // Formula: steps = angle * stepsPerDegree + centerSteps
                                    // Default: stepsPerDegree=100, centerSteps=20000
                                    int stepsPerDegree = 100;
                                    long targetSteps = (long)(targetAbsolute * stepsPerDegree + 200 * stepsPerDegree);
                                    string cmd = $"N {targetSteps}#";
                                    
                                    bool usedNCommand = false;
                                    try
                                    {
                                        // Try to send N command via ASCOM CommandString
                                        string response = rotatorDevice.Action("CommandString", cmd);
                                        Logger.Info($"DEROT: N cmd sent, target={targetAbsolute:F4}° steps={targetSteps} response={response}");
                                        usedNCommand = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.Debug($"DEROT: N command failed ({ex.Message}), falling back to MoveAbsolute");
                                    }
                                    
                                    if (!usedNCommand)
                                    {
                                        Logger.Info($"DEROT: MoveAbsolute to {targetAbsolute:F4}° (applying {applied:F4}°)");
                                        rotatorDevice.MoveAbsolute((float)targetAbsolute, token);
                                    }
                                    
                                    // Brief delay then check actual position
                                    await Task.Delay(200, token);
                                    double newPos = rotatorDevice.Position;
                                    Logger.Info($"DEROT: after move, pos={newPos:F4}°");
                                    
                                    _viewModel.TotalRotationApplied += applied;
                                    _accumulatedError = 0;
                                }
                            }
                        }
                        else
                        {
                             _viewModel.Status = "Mount and rotator are not connected";
                             _viewModel.IsActive = false;
                        }
                    }
                    
                    await Task.Delay(1000, token); // Poll and update every second
                }
                catch (TaskCanceledException)
                {
                    // Expected when the loop is stopped
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in DeRotationService background loop: {ex.Message}");
                    _viewModel.Status = $"Error: {ex.Message}";
                    await Task.Delay(1000, token); // Prevent runaway loop
                }
            }
        }
    }
}