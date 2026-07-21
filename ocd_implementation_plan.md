# 🌙 Overnight Capture Diagnostics (OCD) Plugin — Implementation Plan

**Plugin Name:** Overnight Capture Diagnostics (OCD)  
**Type:** Headless, Sequencer-Only N.I.N.A. Plugin (`.NET 8.0-windows`)  
**Target N.I.N.A. Version:** 3.0+ / 3.1+  
**Primary Goal:** Automatically analyze N.I.N.A. session logs and telemetric data upon sequence completion, generating a comprehensive, highly visual Markdown (`.md`) **and** standalone HTML (`.html`) report summarizing equipment details, exposure statistics, guiding/optical performance, environmental drifts, meridian flips, autofocus performance, statistical anomalies, and actionable recommendations.

---

## 🎯 1. Target Intelligence: Per-Target Breakdown + Master Session Summary

A common workflow in astrophotography is capturing multiple targets over a single night (e.g., Target 1: M31 from 22:00 to 01:30, Target 2: M42 from 01:45 to 05:00). To avoid redundant or confusing reports:

1. **Single-Target Sessions:**
   - If only **one target** was imaged during the session, a single unified report is generated containing all performance metrics, graphs, statistics, and hardware details without duplicated sections.

2. **Multi-Target Sessions:**
   - If **multiple targets** were imaged during the night, the report structures the diagnostic data into clear, dedicated sections:
     - **Overall Master Session Summary:** Total night integration, global capture efficiency, overall night quality score, global temperature drift, full-night overhead breakdown, and total frame counts.
     - **Per-Target Diagnostics (Target 1, Target 2, ...):** Dedicated diagnostic subsections for each individual target, including target-specific exposure counts, filter breakdown, altitude vs. HFR trend, median star counts, target-specific RMS guiding performance, and target-specific AF runs / Meridian flips.
     - In the HTML report, users can seamlessly switch between **Overall Session** and **Per-Target Tabs** (or expanding accordion views).

---

## 🌐 2. Dual Output Formats: Markdown (`.md`) + Web HTML (`.html`)

To ensure the report can be read effortlessly by any user without needing a dedicated Markdown viewer, the plugin will generate **two complementary reports** simultaneously:

1. **Markdown Report (`.md`)**:
   - Clean, GitHub-flavored Markdown text file.
   - Ideal for Obsidian, Notion, GitHub repos, or pasting directly into astronomy forums / Discord.

2. **Standalone HTML Web Report (`.html`)**:
   - Modern, single-file HTML document with embedded CSS styling and inline Base64 SVG graphs.
   - Requires zero external dependencies or internet connection to render.
   - Designed with a premium **Dark Mode Astro Theme** (matching N.I.N.A.'s UI aesthetic), responsive tables, glowing KPI highlight cards, alert callouts, and clean typography.
   - **Double-clickable:** Users can open it immediately in any web browser (Edge, Chrome, Firefox, Safari).
   - **Sequencer Option:** Includes a toggle to **"Auto-open HTML report in default browser on sequence finish"**.

---

## 📊 3. Graph Rendering Strategy in Markdown & HTML

We will use **Embedded Vector SVG Charts** rendered natively in C#:

- **Self-Contained SVGs (Data URIs / Vector Graphics):**
  - Generated in C# using vector graphics generation.
  - Embedded directly into both `.md` (`<img src="data:image/svg+xml;base64,..." />`) and `.html` (`<svg>...</svg>` or `data:image/svg+xml`).
  - **Benefits:** 100% self-contained single-file reports without extra image asset folders or external web dependencies.
- **Mermaid.js Diagrams (` ```mermaid ` in `.md` / rendered in `.html`):**
  - Timeline Gantt charts for session execution (Slew → AF → Target 1 Exposures → Meridian Flip → Target 2 Exposures → Warmup).
- **Unicode Sparklines & Progress Bars:**
  - High-density inline visual meters inside tables (e.g. `[████████░░ 80%]`, `HFR: 2.1 ───↗↘── 2.8`).

---

## 🔍 4. Comprehensive Data Collection & Statistical Analytics

When the OCD sequence instruction executes, it reads N.I.N.A. log files for the current session along with runtime hardware mediators (`ICameraMediator`, `ITelescopeMediator`, `IFocuserMediator`, `IFilterWheelMediator`, `IGuiderMediator`, `ISwitchMediator`, `IProfileService`).

### A. Site & Session Overview
- **Mount Location:** Latitude, Longitude, Elevation, Site Name.
- **Session Duration:** Start time, End time, Total elapsed time.
- **Targets Imaged:** List of targets, RA/Dec coordinates, Altitude/Azimuth range per target.

### B. Hardware Equipment Profile & Optical Calculations
- **Camera:** Model, Sensor resolution (Width x Height), Pixel size ($\mu m$), Gain, Offset, Cooler status.
- **Telescope / Optics:** Focal Length ($mm$), Aperture ($mm$), Focal Ratio ($f/$ number).
- **Calculated Pixel Scale:** Image scale in $\text{arcsec/pixel}$ using $\text{Scale} = \frac{\text{Pixel Size} \times 206.265}{\text{Focal Length}}$.
- **Field of View (FOV):** Dimensions in arcminutes/degrees.
- **Focuser:** Model, step size, temperature sensor presence.
- **Filter Wheel:** Model, filter names, filter positions used.
- **Guider:** Guide camera, guide scope focal length, software (PHD2/NINA internal).
- **Power Hub / Switch:** Model, active ports, dew heater duty cycles.

### C. Capture Efficiency & Overhead Analysis (Global + Per Target)
- **Frame Counts:** Total exposures taken, accepted, rejected, aborted (overall and broken down per target).
- **Per-Filter Breakdown Table:** Count, Exposure time, Total integration time (Hours/Minutes) per filter for each target.
- **Efficiency Breakdown:**
  - Active Imaging Time vs Overhead Time (Focusing, Slewing, Plate Solving, Meridian Flipping, Dithering, Idle).
  - Duty Cycle Percentage (e.g., **78.4%** of session spent collecting photons).

### D. Quality & Environmental Statistics (Min, Max, Avg, Median, StdDev)
Evaluated both **for the whole night** and **per target**:
1. **HFR (Half Flux Radius):** Min, Max, Average, Median, Standard Deviation ($\sigma$), and slope trend over time & target altitude.
2. **Star Count:** Min, Max, Average, Median, StdDev per exposure.
3. **Guiding RMS:**
   - RA RMS ($\text{arcsec}$ and $\text{px}$), DEC RMS ($\text{arcsec}$ and $\text{px}$), Total RMS ($\text{arcsec}$).
   - Min, Max, Avg, Median, StdDev, worst guide spike per target.
4. **Temperatures:**
   - Camera Sensor Temperature (Stability & setpoint variance).
   - Ambient / Focuser Temperature (Min, Max, Total drift, °C/hour cooling rate).
   - Power Switch telemetry (Voltage, Amperage, Total Watt-hours consumed, Dew heater duty cycle %).

---

## 🎯 5. Specialized Events & Anomaly Diagnostics

### A. Meridian Flip Analysis
- Timestamp and sequence step when flip occurred (associated with specific target).
- Duration of the entire flip routine (Slew → Plate Solve → Re-center → Guide Start → Resume).
- **Before vs. After Metrics:** Comparison of HFR, Star Count, and Guiding RMS 30 minutes before vs 30 minutes after flip.

### B. Autofocus (AF) Analysis
- Timestamps and triggers (Temperature Delta, Time Interval, HFR Increase %, Filter Change, Start of Target).
- HFR immediately before vs after AF run (Calculates % HFR improvement).
- AF curve metrics (Initial step, Focus point step position, V-curve slope quality $R^2$).
- Temperature vs Focuser Position trend line (Calculates system's Thermal Coefficient $\mu m / ^\circ C$).

### C. Statistical Anomaly Detection & Alerts
The plugin scans session data for anomalies using Z-score outlier detection ($|Z| > 2.5$) and sudden threshold deviations:
- **HFR Spikes:** Sudden increase in HFR ($>2.5 \sigma$) — flags passing clouds, defocus, or high altitude haze.
- **Star Count Drops:** Sudden drop in detected stars ($>50\%$ drop) — flags thick clouds, tree/roof obstruction.
- **Guiding Spikes & Star Loss:** RMS spikes ($>3\text{x}$ average) — flags wind gusts, cable snags, or lost guide stars.
- **Temperature Drift Alert:** Rapid temperature shift ($>1.5^\circ C/\text{hr}$) without triggering AF.
- **Dew / Power Warnings:** Voltage dips or heater maxing out at 100%.

---

## 💡 6. Additional Creative Ideas & Value-Add Features

1. **⭐ Overall & Per-Target Imaging Quality Score (0–100 Rating):**
   - Automated rating for overall session and individual targets based on guiding quality, focus consistency, efficiency, and frame rejection rate.

2. **⚖️ Filter Comparison Table:**
   - Side-by-side comparison of median HFR, star count, and RMS for each filter used across targets.

3. **🤖 Actionable Recommendations Engine:**
   - Smart advice generated based on night diagnostics (e.g. mount balance tips, thermal AF threshold tuning, target altitude limits).

4. **📱 Webhook / Notification Integration (Optional):**
   - Option in instruction settings to send executive summary + Quality Score directly to **Discord** or **Telegram** webhooks.

5. **🌐 Auto-Open Browser Feature:**
   - Optional setting in the instruction to launch the default web browser showing the newly generated `.html` report as soon as the sequence finishes!

---

## 🖼️ 7. Report Layout & Visual Design Preview

The HTML report will feature interactive target section tabs:

```
+-----------------------------------------------------------------------------------+
| 🔭 Overnight Capture Diagnostics Report                                           |
| Session Date: 2026-07-20 | Multi-Target Session (2 Targets)                     |
+-----------------------------------------------------------------------------------+
| [ Night Score: 92/100 ] [ Total Integration: 6h 15m ] [ Targets: M31, M42 ]       |
+-----------------------------------------------------------------------------------+
| [ 🌐 Overall Master Summary ]   [ 🎯 Target 1: M31 ]   [ 🎯 Target 2: M42 ]       |
+-----------------------------------------------------------------------------------+
| ⚙️ Equipment & Optical Profile                                                    |
| ⏱️ Capture & Overhead Breakdown (Per-Filter Table & Duty Cycle)                  |
| 📈 HFR, Star Count, Guiding RMS, Temp Graphs (Embedded Base64 SVG)               |
| 🔄 Meridian Flip & Autofocus Diagnostics                                         |
| 🚨 Anomalies & Actionable Recommendations                                         |
+-----------------------------------------------------------------------------------+
```

---

## 🏗️ 8. Technical Architecture & Project Structure

### Project Layout (`C:\Users\Nir\repos\Overnight Capture Diagnostics`)
```
Overnight Capture Diagnostics/
├── Overnight Capture Diagnostics.csproj
├── OvernightCaptureDiagnostics.cs           # PluginManifest implementation
├── Options.xaml                             # SequenceBlockView DataTemplate
├── Options.xaml.cs
├── Properties/
│   └── AssemblyInfo.cs
├── Sequencer/
│   └── OCDSequenceItem.cs                 # SequenceItem instruction
├── Services/
│   ├── LogParserService.cs                  # NINA log reader & regex extractor
│   ├── SessionStatsCalculator.cs            # Statistics engine (Min/Max/Avg/StdDev/Anomalies/Per-Target)
│   ├── ChartGeneratorService.cs             # SVG chart generator
│   ├── MarkdownReportWriter.cs              # Markdown document builder
│   ├── HtmlReportWriter.cs                  # HTML document builder with target tabs & embedded CSS
│   └── WebhookService.cs                    # Optional Discord/Telegram poster
└── Models/
    ├── SessionData.cs
    ├── TargetSessionData.cs
    ├── FrameRecord.cs
    ├── AutofocusRecord.cs
    ├── MeridianFlipRecord.cs
    └── AnomalyRecord.cs
```

---

## 🛡️ 9. Proposed Plan of Action & Verification

1. **Phase 1: Project Setup**
   - Create solution and `.csproj` targeting `net8.0-windows` with `<CodePage>65001</CodePage>` and NINA 3.0 NuGet packages.
   - Configure `PluginManifest` and `PostBuild` copy commands to N.I.N.A. plugin directory.

2. **Phase 2: Core Data Models & Log Parser (Target-Aware)**
   - Create data structures (`SessionData`, `TargetSessionData`, `FrameRecord`, `AutofocusRecord`, `MeridianFlipRecord`).
   - Implement `LogParserService` to scan N.I.N.A. log directory and group exposures/events by Target Name.

3. **Phase 3: Statistics & SVG Chart Engine**
   - Implement `SessionStatsCalculator` to compute overall AND per-target statistics.
   - Implement `ChartGeneratorService` to render responsive SVG graphs per target and overall session.

4. **Phase 4: Markdown & HTML Report Generators**
   - Implement `MarkdownReportWriter` and `HtmlReportWriter` with target section navigation.
   - Implement auto-open browser logic.

5. **Phase 5: Sequencer Instruction & UI View**
   - Implement `OCDSequenceItem` and `Options.xaml` WPF template with settings options.

6. **Phase 6: Verification & Testing**
   - Compile in Release configuration.
   - Test log parsing against multi-target N.I.N.A log files.
   - Open generated `.html` in web browsers to test target tab switching and responsive layouts.
