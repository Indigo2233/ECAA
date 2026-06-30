using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.ManifestDefinition;
using NINA.Core.Utility;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;

namespace AltAzDeRotator
{
    [Export(typeof(IPluginManifest))]
    public class PluginManifest : IPluginManifest
    {
        public string Identifier => "AltAz.DeRotator";
        public string Name => "Alt-Az De-Rotator";
        public string Author => "ArchitectJuan";
        public string Description => "Actively compensates for field rotation on Alt-Azimuth mounts.";
        
        public IPluginVersion Version => new PluginVersion("1.2.0.0");
        public IPluginVersion MinimumApplicationVersion => new PluginVersion("3.0.0.9001");

        public string LicenseURL => "https://opensource.org/licenses/MPL-2.0";
        public string Homepage => "https://github.com/ArchitectJuan/Nina_Rotator_Plugin";
        public string Repository => "https://github.com/ArchitectJuan/Nina_Rotator_Plugin.git";
        public string ChangelogURL => "https://github.com/ArchitectJuan/Nina_Rotator_Plugin/releases";
        public string License => "MPL-2.0";
        
        public string[] Tags => new string[] { "equipment", "rotator", "alt-az" };

        public IPluginDescription Descriptions => new PluginGuide();
        public IPluginInstallerDetails Installer => null;

        public Task Initialize()
        {
            Logger.Info("Alt-Az De-Rotator: Discovered Manifest.");
            return Task.CompletedTask;
        }

        public Task Teardown()
        {
            return Task.CompletedTask;
        }
    }

    public class PluginGuide : IPluginDescription
    {
        public string ShortDescription => "Alt-Az De-Rotator";
        public string LongDescription => "This plugin actively compensates for field rotation on Alt-Azimuth mounts by calculating the necessary rotation rate based on your current mount coordinates and commanding an OnStep or ASCOM-compatible rotator.\n\n" +
            "How to Use:\n" +
            "1. Navigate to the Imaging tab. You will find the 'Alt-Az De-Rotator' panel.\n" +
            "2. Ensure both your Telescope Mount and your Rotator are connected in the NINA Equipment tab.\n" +
            "3. Toggle the switch to 'ON'. The background service will begin polling the mount's altitude, azimuth, and your site location.\n" +
            "4. The plugin calculates the exact degrees-per-second required to counter field rotation and commands your rotator to slew dynamically.\n\n" +
            "Troubleshooting:\n" +
            "• If the status says 'Mount and rotator are not connected', verify both devices are connected in the Equipment tab.\n" +
            "• The rotation rate naturally approaches infinity near the Zenith (the 'Zenith Hole'). Tracking may fail or behave erratically if imaging directly overhead.";
            
        public string FeaturedImageURL => "Icon.png";
        public string ScreenshotURL => "";
        public string AltScreenshotURL => "";
    }
}
