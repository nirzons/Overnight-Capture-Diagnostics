using System.Reflection;
using System.Runtime.InteropServices;

// [MANDATORY] Unique identifier of the plugin
[assembly: Guid("8f9c1e2d-3a4b-5c6d-7e8f-9a0b1c2d3e4f")]

// [MANDATORY] Assembly versioning
[assembly: AssemblyVersion("1.0.3.0")]
[assembly: AssemblyFileVersion("1.0.3.0")]

// [MANDATORY] Name and description of your plugin
[assembly: AssemblyTitle("Overnight Capture Diagnostics")]
[assembly: AssemblyDescription("Post-sequence telemetry analysis and diagnostic reporting for N.I.N.A.")]
[assembly: AssemblyCompany("Nir Zonshine")]
[assembly: AssemblyProduct("Overnight Capture Diagnostics")]
[assembly: AssemblyCopyright("Copyright © 2026 Nir Zonshine")]

// [CRITICAL] The minimum version of N.I.N.A. that this plugin is compatible with
[assembly: AssemblyMetadata("MinimumApplicationVersion", "3.0.0.0")]

// Additional Plugin Metadata
[assembly: AssemblyMetadata("License", "MIT")]
[assembly: AssemblyMetadata("LicenseURL", "https://opensource.org/licenses/MIT")]
[assembly: AssemblyMetadata("Repository", "https://github.com/nirzons/Overnight-Capture-Diagnostics")]
[assembly: AssemblyMetadata("Homepage", "https://github.com/nirzons/Overnight-Capture-Diagnostics")]
[assembly: AssemblyMetadata("ChangelogURL", "https://github.com/nirzons/Overnight-Capture-Diagnostics/blob/main/CHANGELOG.md")]

[assembly: ComVisible(false)]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
