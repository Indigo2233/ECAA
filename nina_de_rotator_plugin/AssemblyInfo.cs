using System.Reflection;
using System.Runtime.InteropServices;

// [MANDATORY] Unique GUID for the plugin
[assembly: Guid("6b3e8e2c-7b9a-4c1d-8e5f-2a3b4c5d6e7f")]

// [MANDATORY] Versioning
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// [MANDATORY] Meta Data
[assembly: AssemblyTitle("Alt-Az De-Rotator")]
[assembly: AssemblyDescription("Actively compensates for field rotation on Alt-Azimuth mounts.")]
[assembly: AssemblyCompany("ArchitectJuan")]
[assembly: AssemblyProduct("Alt-Az De-Rotator")]
[assembly: AssemblyCopyright("Copyright © 2026 ArchitectJuan")]

// [MANDATORY] NINA Specific Metadata
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.9001")]
[assembly: AssemblyMetadata("License", "MPL-2.0")]
[assembly: AssemblyMetadata("LicenseURL", "https://opensource.org/licenses/MPL-2.0")]
[assembly: AssemblyMetadata("Repository", "https://github.com/ArchitectJuan/Nina_Rotator_Plugin")]
[assembly: AssemblyMetadata("Homepage", "https://github.com/ArchitectJuan/Nina_Rotator_Plugin")]
[assembly: AssemblyMetadata("Tags", "rotator,alt-az,de-rotation")]

[assembly: ComVisible(false)]
