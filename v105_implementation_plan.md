# Implementation Plan: v1.0.5.0 - Astronomical Darkness, Active Duty Cycle & AutoFocus Delta Formatting

This plan outlines the architecture and changes required to implement **Astronomical Darkness & Active Duty Cycle Refinement** and **AutoFocus Delta Formatting** in **Overnight Capture Diagnostics (OCD) v1.0.5.0**.

---

## 📌 Feature Summary

1. **Astronomical Darkness & Active Duty Cycle Refinement**:
   - Replaces the generic process-elapsed efficiency metric with two domain-accurate astrophotography metrics:
     - **Active Imaging Duty Cycle ($D_{\text{imaging}}$)**: Measures sequence tightness during active imaging:
       $$D_{\text{imaging}} = \frac{\text{Total Integration Time}}{\text{Last Light Frame End} - \text{First Light Frame Start}} \times 100\%$$
     - **Dark Sky Efficiency ($E_{\text{dark}}$)**: Measures utilization of available dark sky ($\text{Sun Altitude} \le -18^\circ$):
       $$E_{\text{dark}} = \frac{\text{Total Integration inside Astro Darkness}}{\text{Astro Dawn} - \text{Astro Dusk}} \times 100\%$$
   - Solves the "Early Startup / Late Park" efficiency penalty (e.g. starting N.I.N.A. in afternoon or parking overnight no longer depresses efficiency).
   - Includes high-latitude summer fallback to Nautical Twilight ($\le -12^\circ$), Civil Twilight ($\le -6^\circ$), or `N/A`.

2. **AutoFocus Table Labeling & Delta Formatting**:
   - Renames table header to **`HFR (Initial → Final | Δ)`**.
   - Replaces `Degr` / `Impr` string labels with signed mathematical deltas (e.g., `+0.02 px`, `-0.07 px`, `0.00 px`).

---

## 🛠️ Proposed Changes

---

### 1. Solar Calculation Utility & Midnight Boundary Anchor
#### [NEW] [Services/AstroUtils.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Services/AstroUtils.cs)
- Implement a NOAA solar position calculation utility.
- Function `CalculateSunPosition(DateTime utcTime, double lat, double lon)` computes Sun Altitude.
- Function `GetAstronomicalNightWindow(DateTime sessionStart, double lat, double lon)` finds exact `AstroDusk` ($\text{Sun Alt} = -18^\circ$ descending) and `AstroDawn` ($\text{Sun Alt} = -18^\circ$ ascending).
- **Midnight Boundary Anchoring**:
  - Normalizes the session date using OCD's 12:00 PM (noon) astronomical anchor:
    `DateTime astroAnchorDate = sessionStart.TimeOfDay < TimeSpan.FromHours(12) ? sessionStart.Date.AddDays(-1) : sessionStart.Date;`
  - Guarantees that sequences starting after midnight (e.g. 01:30 AM) correctly pair the preceding evening's `AstroDusk` (Day N 21:15) with morning `AstroDawn` (Day N+1 04:15).
- **Tiered Twilight Fallback**:
  - Astro ($-18^\circ$) ➔ Nautical ($-12^\circ$) ➔ Civil ($-6^\circ$) ➔ N/A (Summer Solstice at high latitudes). Sets `UsedNauticalFallback = true`.

---

### 2. Data Models
#### [MODIFY] [Models/SessionData.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Models/SessionData.cs)
- Add new properties to master session state:
  - `public DateTime? AstroDusk { get; set; }`
  - `public DateTime? AstroDawn { get; set; }`
  - `public TimeSpan AstroDarknessDuration { get; set; }`
  - `public double? AstroDarknessEfficiency { get; set; }`
  - `public double ImagingDutyCycle { get; set; }`
  - `public bool UsedNauticalFallback { get; set; }`

#### [MODIFY] [Models/AutoFocusRecord.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Models/AutoFocusRecord.cs)
- Add formatted helper property: `HfrDeltaString` returning `+0.02 px`, `-0.05 px`, or `0.00 px`.

---

### 3. Statistics & Calculation Engine
#### [MODIFY] [Services/SessionStatsCalculator.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Services/SessionStatsCalculator.cs)
- **Compute `ImagingDutyCycle`**:
  - Calculate active imaging span: `ActiveSpan = LastLightEnd - FirstLightStart`.
  - Calculate `ImagingDutyCycle = TotalIntegrationSeconds / ActiveSpan.TotalSeconds * 100`.
- **Compute `AstroDarknessEfficiency`**:
  - **Missing GPS Guard Clause**: If `Lat == 0 && Lon == 0`, set `AstroDarknessEfficiency = null` (displays `N/A (GPS Required)`).
  - Pass `session.SessionStart` to `AstroUtils.GetAstronomicalNightWindow(session.SessionStart, lat, lon)` anchored to 12:00 PM noon.
  - **Sub-frame Overlap Clipping**: For each light frame $i$, clip exposure start/end to `[AstroDusk, AstroDawn]`:
    $$\text{ClippedSeconds}_i = \max\Big(0, \min(\text{FrameEnd}_i, \text{Dawn}) - \max(\text{FrameStart}_i, \text{Dusk})\Big)$$
  - Calculate `AstroDarknessEfficiency = Sum(ClippedSeconds_i) / AstroDarknessDuration.TotalSeconds * 100`.

---

### 4. Report Writers
#### [MODIFY] [Services/MarkdownReportWriter.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Services/MarkdownReportWriter.cs)
#### [MODIFY] [Services/HtmlReportWriter.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Services/HtmlReportWriter.cs)
- **Summary Banner**:
  - Display `Astro Night Window`: `21:15 — 03:45 (6h 30m)`
  - Display `Dark Sky Efficiency`: **`71.5%`** (or `N/A`)
  - Display `Active Duty Cycle`: **`94.3%`**
- **AutoFocus Table**:
  - Change column header to **`HFR (Initial → Final | Δ)`**.
  - Render rows as `1.99 px → 2.01 px (+0.02 px)`.

---

### 5. Plugin Versioning
#### [MODIFY] [manifest.json](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/manifest.json)
#### [MODIFY] [Properties/AssemblyInfo.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Properties/AssemblyInfo.cs)
#### [MODIFY] [Overnight Capture Diagnostics.csproj](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Overnight%20Capture%20Diagnostics.csproj)
#### [MODIFY] [CHANGELOG.md](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/CHANGELOG.md)
#### [MODIFY] [README.md](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/README.md)
- Bump version to **`1.0.5.0`** across all manifests and documentation files.

---

## 🧪 Verification Plan

### Automated Build Verification
- Run `dotnet build "Overnight Capture Diagnostics.csproj"` to confirm clean compilation (**0 Errors**).

### Real Log Telemetry Verification
- Run historic log parser against `rig2` and `my rig` sample logs.
- Verify that `AstroDusk` and `AstroDawn` are calculated accurately for Zikhron Yaakov and Hevel Modiin coordinates.
- Confirm `ImagingDutyCycle` reflects sequence tightness (~94%) and `AstroDarknessEfficiency` reflects dark sky usage (~71%).
- Verify that AutoFocus table displays `+0.02 px` instead of `Degr`.
