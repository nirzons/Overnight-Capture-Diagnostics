---
name: nina_plugin_icon_submission
description: >
  Knowledge about how N.I.N.A. (Nighttime Imaging 'N' Astronomy) plugin icons work, 
  why the locally-installed plugin icon doesn't appear in the Plugin Manager tab,
  and the complete process for submitting the OCD plugin to the official N.I.N.A.
  plugin repository (isbeorn/nina.plugin.manifests) so the icon and listing appear
  in N.I.N.A.'s built-in Plugin Manager.
---

# N.I.N.A. Plugin Icon & Official Repository Submission

## Why "Icon" in manifest.json Does Nothing

**`"Icon": "logo.png"` in the local `manifest.json` is an unrecognized / ignored field.**

This was confirmed by reflecting against `NINA.Plugin.dll` (NuGet `nina.plugin` 3.0.0.2017-beta):

- `IPluginManifest` interface has **no `Icon` property** — its properties are:
  `Identifier`, `Name`, `License`, `LicenseURL`, `Author`, `Homepage`, `Repository`,
  `ChangelogURL`, `Tags`, `Version`, `MinimumApplicationVersion`, `Installer`, `Descriptions`
- `PluginBase` (which plugin classes inherit) similarly has **no `Icon` property**
- The official manifest schema (`manifest.schema.json` in `isbeorn/nina.plugin.manifests`) does **not** define an `Icon` field

The `logo.png` **is** correctly embedded as a WPF resource in the DLL (`logo.png` shows up in the `.g.resources` stream). It is used internally (e.g., in the `Options.xaml` view), but N.I.N.A.'s Plugin Manager tab does not read it for the plugin list icon.

## How N.I.N.A. Actually Displays Plugin Icons

- For plugins installed via the **N.I.N.A. Plugin Manager** (from the official repository): 
  the icon is fetched online from a `FeaturedImageURL` URL defined in the **central manifest** at `isbeorn/nina.plugin.manifests`.
- For plugins installed **manually** (copied to the Plugins folder): 
  N.I.N.A. shows a **generic default puzzle-piece icon** — there is no supported mechanism to override this.

## Plugin Manager Icon vs. Sequencer Instruction Icon

These are two **different** icons:

| Icon | Type | How It's Set |
|------|------|-------------|
| **Sequencer instruction icon** (shown in the sequence editor next to the item) | `System.Windows.Media.GeometryGroup` | Set on `SequenceItem.Icon` property in the item's constructor via `RegisterCustomIcon()` |
| **Plugin Manager tab icon** (shown in N.I.N.A.'s Plugins settings page) | Fetched from URL | Only works if plugin is in the official repo with a `FeaturedImageURL` |

The OCD sequencer instruction icon works correctly — it's set via SVG path geometry in `OCDSequenceItem.RegisterCustomIcon()`.

## Submitting to the Official N.I.N.A. Plugin Repository

To make OCD appear in N.I.N.A.'s built-in Plugin Manager with icon, description, and auto-install support:

### Repository
`https://github.com/isbeorn/nina.plugin.manifests`

### Directory Structure
Manifests are organized as:
```
manifests/<first_letter_of_name>/<Plugin Name>/<nina_major_version>/manifest.json
```

For OCD:
```
manifests/o/Overnight Capture Diagnostics/3.0.0/manifest.json
```

### Required Manifest JSON Fields

Based on real manifests in the repo, a complete submission looks like:

```json
{
    "Name": "Overnight Capture Diagnostics",
    "Identifier": "8f9c1e2d-3a4b-5c6d-7e8f-9a0b1c2d3e4f",
    "Version": {
        "Major": "1",
        "Minor": "0",
        "Patch": "0",
        "Build": "0"
    },
    "Author": "Nir Zonshine",
    "Homepage": "https://github.com/nirzons/Overnight-Capture-Diagnostics",
    "Repository": "https://github.com/nirzons/Overnight-Capture-Diagnostics",
    "License": "MIT",
    "LicenseURL": "https://opensource.org/licenses/MIT",
    "ChangelogURL": "https://github.com/nirzons/Overnight-Capture-Diagnostics/blob/main/CHANGELOG.md",
    "Tags": ["Sequencer", "Diagnostics", "Report", "Statistics"],
    "MinimumApplicationVersion": {
        "Major": "3",
        "Minor": "0",
        "Patch": "0",
        "Build": "0"
    },
    "Descriptions": {
        "ShortDescription": "Post-sequence telemetry analysis and multi-format (MD & HTML) diagnostic reporting.",
        "LongDescription": "..."
    },
    "Installer": {
        "URL": "https://github.com/nirzons/Overnight-Capture-Diagnostics/releases/download/v1.0.0.0/OvernightCaptureDiagnostics-v1.0.0.0.zip",
        "Type": "ARCHIVE",
        "Checksum": {
            "Type": "MD5",
            "Value": "<md5-hash-of-zip>"
        }
    },
    "FeaturedImageURL": "https://raw.githubusercontent.com/nirzons/Overnight-Capture-Diagnostics/main/logo.png",
    "ScreenshotURL": "https://raw.githubusercontent.com/nirzons/Overnight-Capture-Diagnostics/main/docs/screenshot.png"
}
```

> **Key fields for the icon**: `FeaturedImageURL` points to a raw GitHub URL of the `logo.png`.

### Checksum Generation

Before submitting, generate the MD5 of the release ZIP:
```powershell
(Get-FileHash "path\to\release.zip" -Algorithm MD5).Hash
```

### Submission Process
1. Fork `isbeorn/nina.plugin.manifests` on GitHub
2. Create `manifests/o/Overnight Capture Diagnostics/3.0.0/manifest.json` with the content above (updated version/checksum)
3. Open a Pull Request against `main`
4. The maintainer (isbeorn) reviews and merges; then OCD appears in N.I.N.A.'s Plugin Manager

### Version-Specific Manifests
When releasing a new version, create a **new** manifest file in the same directory:
```
manifests/o/Overnight Capture Diagnostics/3.0.0/manifest.1.1.0.0.json
```
(The convention appears to be `manifest.<Major>.<Minor>.<Patch>.<Build>.json` for version-specific entries,
or just `manifest.json` for the latest stable.)

## Plugin GUID / Identifier

OCD's fixed Identifier (GUID): `8f9c1e2d-3a4b-5c6d-7e8f-9a0b1c2d3e4f`

This is defined in `Properties\AssemblyInfo.cs` as `[assembly: Guid("8f9c1e2d-3a4b-5c6d-7e8f-9a0b1c2d3e4f")]`
and is also present in the local `manifest.json`.

## Current State of OCD Icon

- ✅ `logo.png` is embedded as a WPF resource in the DLL
- ✅ `logo.png` is present in the installed plugin folder
- ✅ The sequencer instruction icon (bar chart SVG geometry) is set and shown in the sequence editor
- ❌ No icon in N.I.N.A.'s Plugin Manager tab (expected — plugin not in official repo)
- ❌ `"Icon": "logo.png"` in local `manifest.json` is a no-op; that field is not recognized by N.I.N.A.

## GitHub Release Asset Naming

When creating a new release, the ZIP file should be named consistently for the installer URL, e.g.:
`OvernightCaptureDiagnostics-v1.0.0.0.zip`

The release workflow (`.github/workflows/release.yml`) automatically creates this on tag push.
