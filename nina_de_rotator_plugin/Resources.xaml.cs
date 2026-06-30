using System.ComponentModel.Composition;
using System.Windows;

namespace AltAzDeRotator
{
    [Export(typeof(ResourceDictionary))]
    public partial class Resources : ResourceDictionary
    {
        public Resources()
        {
            InitializeComponent();
        }
    }
}
