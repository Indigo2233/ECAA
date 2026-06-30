using System.Windows.Controls;
using System.ComponentModel.Composition;

namespace AltAzDeRotator
{
    [Export]
    public partial class DeRotationSettingsView : UserControl
    {
        public DeRotationSettingsView()
        {
            try
            {
                InitializeComponent();
            }
            catch (System.Exception ex)
            {
                NINA.Core.Utility.Logger.Error($"FAILED TO INITIALIZE UI VIEW: {ex}");
            }
        }
    }
}
