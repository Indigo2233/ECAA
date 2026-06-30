using NINA.Core.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using System;
using System.ComponentModel.Composition;

namespace AltAzDeRotator
{
    [Export(typeof(IDockableVM))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DeRotationViewModel : DockableVM
    {
        private DeRotationService? _deRotationService;

        [ImportingConstructor]
        public DeRotationViewModel(
            IProfileService profileService,
            NINA.Equipment.Interfaces.Mediator.ITelescopeMediator telescopeMediator,
            NINA.Equipment.Interfaces.Mediator.IRotatorMediator rotatorMediator) : base(profileService)
        {
            try
            {
                NINA.Core.Utility.Logger.Info("Alt-Az De-Rotator Plugin logic initializing in ViewModel...");
                _deRotationService = new DeRotationService(telescopeMediator, rotatorMediator, this);
                _deRotationService.Start();
            }
            catch (Exception ex)
            {
                NINA.Core.Utility.Logger.Error($"Alt-Az De-Rotator Plugin logic initialization failed: {ex.Message}");
            }
        }

        public new string Id => "AltAz_DeRotator_Status_Window";
        public new string Title => "Alt-Az De-Rotator";
        public new bool IsTool => true;

        private double _altitude;
        public double Altitude
        {
            get => _altitude;
            set { _altitude = value; RaisePropertyChanged(); }
        }

        private double _azimuth;
        public double Azimuth
        {
            get => _azimuth;
            set { _azimuth = value; RaisePropertyChanged(); }
        }

        private double _rotationRate;
        public double RotationRate
        {
            get => _rotationRate;
            set { _rotationRate = value; RaisePropertyChanged(); }
        }

        private double _targetPosition;
        public double TargetPosition
        {
            get => _targetPosition;
            set { _targetPosition = value; RaisePropertyChanged(); }
        }

        private double _totalRotationApplied;
        public double TotalRotationApplied
        {
            get => _totalRotationApplied;
            set { _totalRotationApplied = value; RaisePropertyChanged(); }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; RaisePropertyChanged(); }
        }

        private bool _isDeRotationEnabled = true;
        public bool IsDeRotationEnabled
        {
            get => _isDeRotationEnabled;
            set { _isDeRotationEnabled = value; RaisePropertyChanged(); }
        }

        private string _status = "Stopped";
        public string Status
        {
            get => _status;
            set { _status = value; RaisePropertyChanged(); }
        }
    }
}
