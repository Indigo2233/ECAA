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
                                // TEST MODE: 10x speed for visible rotation during testing
                                _accumulatedError += degreesPerSecond * 10;
                                
                                double targetAbsolute = (currentRotatorPosition + _accumulatedError) % 360.0;
                                if (targetAbsolute < 0) targetAbsolute += 360.0;
                                _viewModel.TargetPosition = targetAbsolute;

                                Logger.Info($"DEROT: pos={currentRotatorPosition:F4}° accum={_accumulatedError:F4}° target={targetAbsolute:F4}° rate={requiredRateDegreesPerHour:F2}°/hr");
                                
                                // Query stepsPerDegree from ASCOM driver (cache would be better but simple for now)
                                int stepsPerDegree = 100; // default
                                try
                                {
                                    string spd = rotatorDevice.Action("StepsPerDegree", "");
                                    stepsPerDegree = int.Parse(spd);
                                }
                                catch { }
                                
                                // Calculate step resolution to determine move threshold
                                double stepResolution = 1.0 / stepsPerDegree;
                                
                                if (Math.Abs(_accumulatedError) >= stepResolution) 
                                {
                                    // Calculate current and target steps
                                    long centerSteps = 200L * stepsPerDegree;
                                    long currentSteps = (long)Math.Round(currentRotatorPosition * stepsPerDegree) + centerSteps;
                                    long targetSteps = (long)Math.Round(targetAbsolute * stepsPerDegree) + centerSteps;
                                    long movedSteps = targetSteps - currentSteps;
                                    
                                    // Only move if we have at least 1 step difference
                                    if (Math.Abs(movedSteps) >= 1)
                                    {
                                        string cmd = $"N {targetSteps}#";
                                        
                                        bool usedNCommand = false;
                                        try
                                        {
                                            // Try to send N command via ASCOM CommandString
                                            string response = rotatorDevice.Action("CommandString", cmd);
                                            Logger.Info($"DEROT: N cmd sent, target={targetAbsolute:F4}° steps={targetSteps} moved={movedSteps} response={response}");
                                            usedNCommand = true;
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.Debug($"DEROT: N command failed ({ex.Message}), falling back to MoveAbsolute");
                                        }
                                        
                                        if (!usedNCommand)
                                        {
                                            Logger.Info($"DEROT: MoveAbsolute to {targetAbsolute:F4}° (applying {_accumulatedError:F4}°)");
                                            rotatorDevice.MoveAbsolute((float)targetAbsolute, token);
                                        }
                                        
                                        // Calculate actual moved angle from integer steps (no precision loss)
                                        double actualMoved = (double)movedSteps / stepsPerDegree;
                                        
                                        // Brief delay then check actual position
                                        await Task.Delay(200, token);
                                        double newPos = rotatorDevice.Position;
                                        Logger.Info($"DEROT: after move, pos={newPos:F4}° actualMoved={actualMoved:F4}°");
                                        
                                        _viewModel.TotalRotationApplied += actualMoved;
                                        // Only subtract actual moved amount, preserve sub-step residual
                                        _accumulatedError -= actualMoved;
                                    }
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