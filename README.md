# <img src="logo.png" width="80" height="80" align="left" style="margin-right: 15px;"> Overnight Capture Diagnostics for N.I.N.A.

<br>

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![NINA Version](https://img.shields.io/badge/N.I.N.A.-3.0%2B-blue.svg)](https://nighttime-imaging.eu/)

**Overnight Capture Diagnostics (OCD)** is an automated, intelligent session analytics and diagnostic reporting plugin for **N.I.N.A. (Nighttime Imaging 'N' Astronomy)**.

At the end of an imaging night—or on-demand for historic session logs—OCD parses your N.I.N.A. log files, image telemetry headers, guiding data, and equipment profiles to compile publication-ready **Markdown (`.md`)** and rich **HTML (`.html`)** diagnostic reports complete with embedded vector SVG charts.

---

## 💾 Installation

### 1. Recommended: N.I.N.A. Plugin Store (Automatic)
The easiest way to install the plugin is directly through the official N.I.N.A. Plugin Store:
1. Open N.I.N.A. and navigate to the **Plugins** tab on the left sidebar.
2. Select **Available** in the top tab menu.
3. Locate **Overnight Capture Diagnostics** (use the search bar if needed) and click **Install**.
4. Restart N.I.N.A. to activate.

### 2. Manual Installation (For Development & Offline PCs)
If you need to install the plugin manually from a custom compiled build or on an offline computer:
1. Download or compile the release binaries.
2. Navigate to your local AppData directory:  
   `%LOCALAPPDATA%\NINA\Plugins\3.0.0\` (typically `C:\Users\<YourUsername>\AppData\Local\NINA\Plugins\3.0.0\`).
3. Create a new subfolder named **`Overnight Capture Diagnostics`**.
4. Copy the compiled binary files into that directory:
   - `NirZonshine.NINA.OvernightCaptureDiagnostics.dll`
   - `NirZonshine.NINA.OvernightCaptureDiagnostics.pdb`
   - `NirZonshine.NINA.OvernightCaptureDiagnostics.deps.json`
   - `NirZonshine.NINA.OvernightCaptureDiagnostics.runtimeconfig.json`
   - `manifest.json`
5. Restart N.I.N.A. to activate.

---

## ✨ Key Features

- **Automated Sequencer Instruction**: Simply drop the **`[OCD] Overnight Capture Diagnostics`** instruction item into your Advanced Sequencer container (typically at the very end of your sequence after parking your scope).
- **24-Hour Astronomical Observing Window**: Live Session mode automatically calculates the strict 24-hour observing window (**12:00 PM noon to 12:00 PM noon next day**) to strictly isolate last night's imaging session without accumulating previous nights' logs. Historic mode scans from 12:00 PM on the requested date to 12:00 PM the following day.
- **N.I.N.A. 3.2 & Modern Filename Telemetry Extraction**: Dynamic template-driven parsing of N.I.N.A.`$$TAG$$` file save patterns (`$$SENSORTEMP$$`, `$$EXPOSURETIME$$`, `$$HFR$$`, `$$STARCOUNT$$`, `$$RMS$$`, `$$FILTER$$`, `$$TARGETNAME$$`) across `.fits`, `.fit`, `.tif`, and `.xisf` images. Accurately extracts signed sensor temperatures (e.g. `-5.00°C`) without requiring `"C"` or `"TEMP"` prefixes.
- **Single Execution Summary Logging**: Logs a clean, versioned execution summary entry (e.g. `[Overnight Capture Diagnostics v1.0.5.0] Created Live report 'OCD_Report_...html'. Debug Mode: Disabled`) into N.I.N.A.'s log file on every run, regardless of debug setting.
- **Filter Wheel Verification**: Strict validation prevents sequencer condition logs (`pierWest`, `Automated Flip`) from corrupting filter wheel metadata on rigs without a physical filter wheel.
- **Multi-Session & Multi-Equipment Profile Support**: Seamlessly handles N.I.N.A process restarts within a single night. Automatically organizes the report into distinct sub-sessions (`Sub-Session 1`, `Sub-Session 2`) with separate equipment tables detailing camera models, focal lengths, pixel scales, and true Field of View (FOV).
- **Offline Sensor Resolution Lookup**: Includes a built-in sensor fallback database for popular astronomy cameras (e.g., IMX462, IMX533, IMX605, IMX571/2600, IMX294, KASI1600, IMX183, IMX455/6200), ensuring accurate FOV and pixel scale calculations even when drivers are offline or disconnected.
- **Session Execution Window vs. Light Capture Telemetry**: Explicitly reports both the full N.I.N.A execution span (`SessionStart` — `SessionEnd`) and the exact **First Light Captured** — **Last Light Captured** timestamps.
- **Polar Alignment Diagnostics**: Extracts initial start error and final settled alignment error (from 2PPA or TPPA). Automatically filters out pre-flight unhomed test measurements for 100% accurate initial vs. final error readings.
- **N.I.N.A. 3.2 Meridian Flip Impact Analysis**: Detects N.I.N.A 3.2 meridian flip routines (`Meridian Flip - Initializing` → `Exiting`) and evaluates imaging quality across a 30-minute pre-flip vs. post-flip window (`HFR`, `Star Count`, and `Guiding RMS`).
- **⚡ Real-time Power & Dew Prevention Analytics**: Continuous background monitoring of ASCOM power switches and smart hubs. Automatically renders dedicated vector charts plotting instantaneous power draw (Watts) and dew heater PWM duty cycles (%), calculating total battery capacity consumption ($\text{Ah}$) and energy consumed ($\text{Wh}$).
- **Optical & Guiding Performance Summary**: Provides a complete 5-metric statistical breakdown (`Min | Max | Mean | Median | StdDev (σ)`) for `HFR (px)`, `Star Count`, and `Guiding RMS (arcsec)`. Automatically tracks unguided light frames and sub-frame health warnings.
- **Embedded Dual-Axis Vector SVG Charts**:
  - ⏱️ **Session Execution Timeline**: Gantt chart visualizing light exposures, autofocus runs, polar alignment routines, meridian flips, and idle overhead.
  - 📈 **HFR & Star Count Profile**: Dual-axis graph plotting `HFR (px)` on the left axis (solid green) and `Star Count` on the right axis (dashed cyan), complete with min/max value labels and legends.
  - 🌡️⚡ **Environmental & Power Profile**: Dual-axis graph plotting Ambient Temperature & Dew Point (°C) against Relative Humidity (%) and Instantaneous Power Draw (Watts) / Dew Heater Duty (%).
- **Automated Report Generation**: Writes formatted `.md` and `.html` report files directly to your default report folder (`%USERPROFILE%\Documents\N.I.N.A\OCD_Reports\`).

---

## ⚡ Supported & Tested Power Switches / Smart Hubs

OCD features continuous background telemetry monitoring for ASCOM power management devices and weather sensors via N.I.N.A.'s `Switch` and `WeatherData` mediators.

> [!IMPORTANT]
> **Physically Tested & Validated Hardware:**  
> The plugin has been **physically tested and confirmed working on only the following two specific devices**:
> 1. **SVBONY SV241 Pro Power Box**: **Fully Supported**. Reports complete environmental and electrical telemetry: Voltage (V), Current (A), Instantaneous Power Draw (W), Ambient Temperature (°C), Relative Humidity (%), Dew Point (°C), and Dew Heater PWM duty cycle (%).
> 2. **2026 Gemini Astro Power Box HUB USB3.2+DC ASCOM Power Box**: **Power & PWM Supported**. Successfully reports Voltage, Current, Instantaneous Power Draw (W), and Dew Heater PWM duty cycles. *Note: Does not expose ambient temperature or humidity to N.I.N.A. through the ASCOM switch interface, so environmental readings are not available from this device.*

### 🔌 Compatibility with Other ASCOM Power Switches & Smart Hubs
While the two units above are the only devices physically verified by the authors, OCD is built on generic ASCOM standards and **may work with other ASCOM power switches and smart hubs** (e.g., Pegasus Astro Powerbox, PrimaLuceLab EAGLE, WandererBox, etc.) if they satisfy the following conditions:
- **ASCOM Switch Driver**: The device provides a compliant ASCOM Switch driver connectable in N.I.N.A. under **Equipment** ➔ **Switch**.
- **Standard Telemetry Channel Naming**: The ASCOM driver labels or describes its read-only gauges and writable control channels using standard naming keywords recognized by OCD:
  - **Voltage**: Channel name contains `VOLTAGE`, `VOLTS`, `VOLT`, or `V`
  - **Current**: Channel name contains `CURRENT`, `AMPS`, `AMP`, `A`, or `MILLIAMP`/`MA`
  - **Power Draw**: Channel name contains `POWER`, `WATTS`, `WATT`, or `W` *(if power in Watts is not reported as a dedicated channel, OCD automatically calculates $P = V \times I$ whenever both Voltage and Current are available)*
  - **Dew Heaters**: Channel name contains `DEW`, `DEW HEATER`, `PWM`, `HEATER`, or `HEATING` *(OCD automatically normalizes duty cycles across $0-100\%$, $0-253$, or $0-255$ hardware scales)*
  - **Environmental Sensors**: Channel name contains `TEMP`, `TEMPERATURE`, `HUMIDITY`, `HUM`, `DEW POINT` *(either reported through switch analog gauges or via a connected N.I.N.A. **Weather** device)*

---

## 📖 Sequencer Integration & Usage

### 1. Advanced Sequencer Instructions
OCD provides two instructions in N.I.N.A.'s Advanced Sequencer:

1. **`Start OCD Telemetry`** *(Category: Utility)*:
   - Place this instruction at the **start of your imaging night** (e.g., inside your *Startup* or *Pre-imaging* sequence container).
   - Starts non-blocking background polling of connected ASCOM power switches and environmental weather sensors at regular intervals (default: 60 seconds).
2. **`[OCD] Overnight Capture Diagnostics`** *(Category: Utility)*:
   - Place this instruction at the **very end of your imaging sequence** (e.g., after target execution, flat capture, and mount parking).
   - Concludes telemetry monitoring, computes total energy/power consumption, derives session statistics, and generates both `.md` and `.html` diagnostic reports.

### 2. Configuration Options
- **Target Session Date**:
  - **Leave Blank**: Automatically analyzes today's live imaging session.
  - **Specify Date (`YYYY-MM-DD`)**: Parses historic N.I.N.A log files for that specific date.
- **Custom Report Output Folder**: Optionally specify a custom directory where Markdown and HTML diagnostic reports should be saved.

---

## ⚙️ N.I.N.A. File Pattern Configuration & Recommendations

To compile complete per-frame health diagnostics (such as **HFR**, **Star Count**, and **Guiding RMS**), OCD dynamically analyzes the **Image File Pattern** configured in N.I.N.A. (`Options` ➔ `Imaging` ➔ `File Settings` ➔ `Image File Pattern`).

> **Important:** Sub-frame telemetry tags must be included in your N.I.N.A. File Save Pattern so that OCD can extract per-frame HFR, star counts, and RMS from saved image file paths without needing to scan raw image files on disk.

### Recommended File Pattern Examples

You can copy and paste any of the following recommended patterns directly into N.I.N.A. (**Options** ➔ **Imaging** ➔ **File Settings** ➔ **Image File Pattern**):

#### 🌟 Pattern 1: Advanced Metrics Pattern (FWHM, Eccentricity, RMS)
```
$$DATEMINUS12$$\$$IMAGETYPE$$\$$DATETIME$$_$$FILTER$$_$$SENSORTEMP$$_$$EXPOSURETIME$$s_$$FRAMENR$$_$$STARCOUNT$$_$$HFR$$_$$ECCENTRICITY$$_$$TARGETNAME$$_$$FWHM$$_$$RMSARCSEC$$
```
*Example Output Filepath:*  
`2026-09-03\LIGHT\2026-09-03_21-23-32_Ha_-10.0_300.00s_0001_950_1.38_0.42_Sh2 119_2.10_0.44.fits`

#### 🌟 Pattern 2: Comprehensive Diagnostics Pattern
```
$$TARGETNAME$$\$$IMAGETYPE$$\$$DATETIME$$_$$FILTER$$_$$EXPOSURETIME$$s_GAIN$$GAIN$$_TEMP$$SENSORTEMP$$_HFR$$HFR$$_$$STARCOUNT$$STARS_RMS$$RMS$$_$$FRAMENR$$
```
*Example Output Filepath:*  
`LDN 1235\LIGHT\2026-07-22_21-25-49_Ha_300.00s_GAIN100_TEMP-4.90_HFR2.17_193STARS_RMS0.24_0000.fits`

#### ⚡ Pattern 3: Compact Standard Pattern
```
$$TARGETNAME$$\$$IMAGETYPE$$\$$DATETIME$$_$$FILTER$$_-$$SENSORTEMP$$C_$$EXPOSURETIME$$s_$$STARCOUNT$$STARS_$$HFR$$HFR_$$RMS$$RMS_$$FRAMENR$$
```
*Example Output Filepath:*  
`IC 5146\LIGHT\2026-07-22_21-44-31_Ha_-4.90C_300.00s_837STARS_1.65HFR_0.47RMS_0000.fits`

#### 🛠️ Pattern 4: Minimal Un-prefixed Pattern
```
$$DATETIME$$_$$FILTER$$_$$SENSORTEMP$$_$$EXPOSURETIME$$s_$$FRAMENR$$_$$STARCOUNT$$_$$HFR$$_$$TARGETNAME$$_$$RMS$$
```
*Example Output Filepath:*  
`2026-07-22_21-25-49_Ha_-4.90_300.00s_0000_874_16.21_LDN 1235_0.12.fits`

---

## 📊 Sample Diagnostic Report Highlights

The following live excerpts and embedded vector charts are taken directly from an authentic overnight imaging run generated by **OCD v1.0.6.3** in Arlington, Texas, USA.

### 🌟 Session Executive Summary
> **Plugin Version:** v1.0.6.3 | **Session Date:** 2026-09-05 | **Session Start:** 2026-09-05 19:14:51 | **Session End:** 2026-09-06 05:32:00 (**Span:** 10h 17m 9s)  
> **First Light Captured:** 2026-09-05 20:03:44 | **Last Light Captured:** 2026-09-06 05:21:17 | **Astro Night Window:** 20:24 — 04:53 (8h 29m 49s)  
> **Site:** Arlington, Texas, USA (32.73° N, 97.11° W) | **Night Score:** 🌟 **74 / 100** | **Total Integration:** 8h 0m 0s | **Dark Sky Efficiency:** **84.5%** | **Active Duty Cycle:** **85.6%**

---

### ⏱️ Capture & Overhead Breakdown
- **Session Execution Window:** 2026-09-05 19:14:51 — 2026-09-06 05:32:00 (Total Elapsed: 10h 17m 9s)
- **Astro Night Window:** 20:24 — 04:53 (8h 29m 49s)
- **Total Night Integration:** 8h 0m 0s
- **Dark Sky Efficiency:** **84.5%** (Integration within Dark Sky / Dark Sky Duration)
- **Active Imaging Duty Cycle:** **85.6%** (Integration / Active Imaging Span)
- **Total Overhead Time:** 2h 17m 9s (AutoFocus routines, dither settling, meridian flips)
- **Estimated Storage Consumed:** **2.70 GB** (160 total frames)
- **⚡ Total Power Consumed:** **303.3 Wh** (~25.1 Ah @ 12.1V avg | Peak: 57.0 W)

#### Real-time Vector Chart Example: Session Execution Timeline
<div align="center">
<svg width="100%" height="180" viewBox="0 0 900 180" style="background-color: #121824; border-radius: 8px;" xmlns="http://www.w3.org/2000/svg">
  <text x="40" y="25" fill="#E0E6ED" font-size="14" font-weight="bold" font-family="Segoe UI, sans-serif">Session Execution Timeline</text>
  <rect x="40" y="50" width="830" height="45" fill="#1A2332" rx="4" />
  <rect x="40.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="44.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="49.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="54.0" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="58.4" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="62.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="67.7" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="72.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="76.7" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="81.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="86.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="90.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="95.4" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="99.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="104.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="109.2" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="114.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="119.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="123.8" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="128.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="141.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="146.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="150.6" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="155.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="160.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="164.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="168.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="173.8" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="178.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="182.7" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="187.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="192.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="204.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="208.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="213.4" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="217.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="222.7" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="227.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="239.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="244.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="249.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="253.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="258.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="262.8" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="267.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="272.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="276.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="281.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="285.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="290.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="294.8" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="299.7" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="305.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="309.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="314.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="319.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="323.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="328.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="336.6" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="341.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="346.4" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="351.6" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="356.8" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="361.7" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="366.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="372.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="377.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="381.5" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="387.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="392.4" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="397.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="402.4" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="407.6" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="413.1" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="418.3" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="423.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="431.5" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="436.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="440.8" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="446.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="451.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="455.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="460.8" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="465.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="472.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="476.8" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="481.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="486.1" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="490.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="495.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="499.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="504.4" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="508.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="513.7" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="518.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="530.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="535.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="539.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="544.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="549.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="553.4" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="557.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="564.6" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="569.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="573.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="578.4" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="582.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="587.4" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="592.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="596.7" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="601.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="606.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="610.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="615.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="623.8" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="628.2" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="632.7" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="637.6" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="643.3" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="648.6" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="653.4" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="658.7" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="663.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="669.5" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="674.7" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="681.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="686.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="690.5" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="695.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="700.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="704.7" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="709.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="717.8" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="722.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="726.8" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="731.7" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="736.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="740.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="745.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="750.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="754.4" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="759.3" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="763.8" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="768.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="773.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="777.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="782.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="793.4" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="797.9" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="803.7" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="828.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="833.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="837.5" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="842.4" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="846.8" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="851.3" y="50" width="4.5" height="45" fill="#EF4444" opacity="0.85" />
  <rect x="856.2" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="860.6" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="865.1" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <rect x="870.0" y="50" width="4.5" height="45" fill="#00D26A" opacity="0.85" />
  <circle cx="62.7" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="76.5" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="90.4" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="104.2" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="118.9" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="141.2" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="155.0" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="168.8" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="182.6" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="203.9" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="217.7" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="239.5" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="253.3" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="267.1" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="280.9" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="294.7" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="323.3" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="372.0" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="435.8" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="450.8" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="465.2" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="481.1" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="494.9" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="508.7" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="530.1" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="543.9" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="557.8" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="573.4" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="587.2" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="601.1" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="614.9" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="632.6" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="681.0" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="694.8" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="709.1" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="726.7" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="740.5" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="754.3" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="768.1" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="803.5" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="823.5" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="837.3" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="851.2" cy="72.5" r="3.0" fill="#F59E0B" />
  <circle cx="864.9" cy="72.5" r="3.0" fill="#F59E0B" />
  <rect x="136.7" y="45" width="6" height="55" fill="#FFD166" rx="2" />
  <rect x="235.0" y="45" width="6" height="55" fill="#FFD166" rx="2" />
  <rect x="332.0" y="45" width="6" height="55" fill="#FFD166" rx="2" />
  <rect x="426.9" y="45" width="6" height="55" fill="#FFD166" rx="2" />
  <rect x="619.2" y="45" width="6" height="55" fill="#FFD166" rx="2" />
  <rect x="713.2" y="45" width="6" height="55" fill="#FFD166" rx="2" />
  <rect x="192.0" y="45" width="8" height="55" fill="#9B59B6" rx="2" />
  <rect x="781.9" y="45" width="8" height="55" fill="#9B59B6" rx="2" />
  <line x1="40.0" y1="95.0" x2="40.0" y2="99.0" stroke="#475569" stroke-width="1" />
  <text x="40.0" y="111.0" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">20:03</text>
  <line x1="178.3" y1="95.0" x2="178.3" y2="99.0" stroke="#475569" stroke-width="1" />
  <text x="178.3" y="111.0" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">21:36</text>
  <line x1="316.7" y1="95.0" x2="316.7" y2="99.0" stroke="#475569" stroke-width="1" />
  <text x="316.7" y="111.0" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">23:09</text>
  <line x1="455.0" y1="95.0" x2="455.0" y2="99.0" stroke="#475569" stroke-width="1" />
  <text x="455.0" y="111.0" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">00:42</text>
  <line x1="593.3" y1="95.0" x2="593.3" y2="99.0" stroke="#475569" stroke-width="1" />
  <text x="593.3" y="111.0" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">02:15</text>
  <line x1="731.7" y1="95.0" x2="731.7" y2="99.0" stroke="#475569" stroke-width="1" />
  <text x="731.7" y="111.0" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">03:48</text>
  <line x1="870.0" y1="95.0" x2="870.0" y2="99.0" stroke="#475569" stroke-width="1" />
  <text x="870.0" y="111.0" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">05:21</text>
  <rect x="40" y="125" width="12" height="12" fill="#00D26A" rx="2" />
  <text x="58" y="135" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Good Light Exposure</text>
  <rect x="185" y="125" width="12" height="12" fill="#EF4444" rx="2" />
  <text x="203" y="135" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Sub-optimal / AF Fail</text>
  <rect x="340" y="125" width="12" height="12" fill="#FFD166" rx="2" />
  <text x="358" y="135" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Autofocus</text>
  <rect x="435" y="125" width="12" height="12" fill="#9B59B6" rx="2" />
  <text x="453" y="135" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Meridian Flip</text>
  <rect x="545" y="125" width="12" height="12" fill="#F59E0B" rx="2" />
  <text x="563" y="135" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Dither</text>
  <rect x="625" y="125" width="12" height="12" fill="#FF0055" rx="2" />
  <text x="643" y="135" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Hardware Event</text>
</svg>
</div>

---

### 🌡️⚡ Environmental & Power Diagnostics
| Metric | Min | Max | Mean / Value | Status / Margin |
| :--- | :--- | :--- | :--- | :--- |
| **Ambient Temp (°C)** | 24.9°C | 27.8°C | **26.4°C** | Optimal |
| **Relative Humidity (%)** | 76% | 87% | **81%** | Normal |
| **Dew Point (°C)** | 22.4°C | 23.4°C | **22.9°C** | Dew Margin: 2.3°C |
| **Dew Heater Duty (%)** | 0% | 95% | **16%** | Active (Dynamic) |

#### Real-time Vector Chart Example: Environmental & Power Profile
<div align="center">
<svg xmlns="http://www.w3.org/2000/svg" width="100%" height="280" viewBox="0 0 900 280" style="background-color: #121824; border-radius: 8px;">
  <text x="40" y="25" fill="#E0E6ED" font-size="14" font-weight="bold" font-family="Segoe UI, sans-serif">🌡️⚡ Environmental &amp; Power Profile (Temperature, Dew Point, Humidity &amp; Power Draw)</text>
  <line x1="60.0" y1="45" x2="60.0" y2="215" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="60.0" y="233" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">20:00</text>
  <line x1="214.0" y1="45" x2="214.0" y2="215" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="214.0" y="233" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">21:53</text>
  <line x1="368.0" y1="45" x2="368.0" y2="215" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="368.0" y="233" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">23:47</text>
  <line x1="522.0" y1="45" x2="522.0" y2="215" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="522.0" y="233" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">01:40</text>
  <line x1="676.0" y1="45" x2="676.0" y2="215" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="676.0" y="233" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">03:34</text>
  <line x1="830.0" y1="45" x2="830.0" y2="215" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="830.0" y="233" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">05:28</text>
  <circle cx="754.1" cy="130.0" r="3.5" fill="#EF4444" opacity="0.85" />
  <circle cx="764.9" cy="130.0" r="3.5" fill="#EF4444" opacity="0.85" />
  <circle cx="775.8" cy="126.6" r="3.5" fill="#EF4444" opacity="0.85" />
  <circle cx="830.0" cy="128.3" r="3.5" fill="#EF4444" opacity="0.85" />
  <polyline points="60.0,148.7 70.8,155.5 81.7,182.7 92.5,152.1 103.4,155.5 114.2,182.7 125.1,157.2 135.9,157.2 146.8,157.2 157.6,186.1 168.5,155.5 179.3,155.5 190.1,184.4 201.0,148.7 211.8,187.8 222.7,153.8 233.5,155.5 244.4,186.1 255.2,153.8 266.1,153.8 276.9,187.8 287.8,157.2 298.6,155.5 309.4,157.2 320.3,153.8 331.1,155.5 342.0,189.5 352.8,157.2 363.7,186.1 374.5,155.5 385.4,155.5 396.2,155.5 407.0,130.0 417.9,184.4 428.7,152.1 439.6,162.3 450.4,155.5 461.3,155.5 472.1,155.5 483.0,187.8 493.8,152.1 504.7,155.5 515.5,153.8 526.3,157.2 537.2,128.3 548.0,155.5 558.9,186.1 569.7,124.9 580.6,189.5 591.4,158.9 602.3,189.5 613.1,150.4 623.9,155.5 634.8,186.1 645.6,153.8 656.5,130.0 667.3,150.4 678.2,150.4 689.0,165.7 699.9,155.5 710.7,187.8 721.6,150.4 732.4,123.2 743.2,162.3 754.1,182.7 764.9,189.5 775.8,189.5 786.6,153.8 797.5,128.3 808.3,189.5 819.2,153.8 830.0,192.9" fill="none" stroke="#FACC15" stroke-width="2.0" stroke-dasharray="6,2,2,2" />
  <polyline points="60.0,84.6 70.8,85.6 81.7,83.9 92.5,84.4 103.4,85.3 114.2,84.6 125.1,85.1 135.9,84.4 146.8,84.8 157.6,85.1 168.5,84.4 179.3,83.4 190.1,83.2 201.0,83.4 211.8,82.9 222.7,82.2 233.5,81.7 244.4,80.9 255.2,80.0 266.1,79.8 276.9,80.4 287.8,80.0 298.6,79.3 309.4,78.3 320.3,77.3 331.1,78.0 342.0,78.3 352.8,80.2 363.7,79.3 374.5,78.3 385.4,78.8 396.2,79.2 407.0,78.7 417.9,80.0 428.7,79.5 439.6,81.0 450.4,78.7 461.3,79.2 472.1,80.4 483.0,79.5 493.8,75.6 504.7,75.9 515.5,73.7 526.3,72.5 537.2,72.2 548.0,71.3 558.9,70.2 569.7,70.8 580.6,70.8 591.4,70.0 602.3,70.2 613.1,71.2 623.9,71.7 634.8,71.2 645.6,73.7 656.5,71.2 667.3,72.0 678.2,73.1 689.0,71.7 699.9,72.9 710.7,70.8 721.6,71.9 732.4,70.8 743.2,69.3 754.1,67.3 764.9,67.4 775.8,68.5 786.6,70.5 797.5,71.5 808.3,70.0 819.2,68.8 830.0,68.1" fill="none" stroke="#818CF8" stroke-width="2.0" stroke-dasharray="4,3" />
  <polyline points="60.0,160.3 70.8,160.8 81.7,160.4 92.5,161.5 103.4,161.6 114.2,161.8 125.1,163.0 135.9,163.1 146.8,163.8 157.6,163.0 168.5,164.7 179.3,162.5 190.1,162.1 201.0,164.2 211.8,166.4 222.7,165.0 233.5,163.8 244.4,163.7 255.2,163.7 266.1,163.3 276.9,166.0 287.8,165.2 298.6,162.1 309.4,163.3 320.3,163.0 331.1,161.1 342.0,160.1 352.8,159.1 363.7,160.6 374.5,161.8 385.4,161.1 396.2,161.8 407.0,162.5 417.9,163.7 428.7,164.2 439.6,167.4 450.4,167.4 461.3,166.7 472.1,171.0 483.0,172.5 493.8,172.7 504.7,171.8 515.5,172.3 526.3,173.4 537.2,172.7 548.0,172.7 558.9,173.7 569.7,171.6 580.6,171.6 591.4,173.4 602.3,172.0 613.1,170.6 623.9,168.2 634.8,167.2 645.6,165.7 656.5,165.7 667.3,167.2 678.2,164.3 689.0,166.7 699.9,165.7 710.7,168.2 721.6,166.9 732.4,168.2 743.2,170.3 754.1,169.8 764.9,170.1 775.8,168.8 786.6,169.3 797.5,169.6 808.3,171.6 819.2,169.4 830.0,169.8" fill="none" stroke="#06B6D4" stroke-width="2.0" />
  <polyline points="60.0,84.1 70.8,82.4 81.7,85.8 92.5,85.8 103.4,84.1 114.2,85.8 125.1,85.8 135.9,87.5 146.8,87.5 157.6,85.8 168.5,89.2 179.3,89.2 190.1,89.2 201.0,90.9 211.8,94.3 222.7,94.3 233.5,94.3 244.4,96.0 255.2,97.7 266.1,97.7 276.9,99.4 287.8,99.4 298.6,97.7 309.4,101.1 320.3,102.8 331.1,99.4 342.0,97.7 352.8,92.6 363.7,96.0 374.5,99.4 385.4,97.7 396.2,97.7 407.0,99.4 417.9,97.7 428.7,99.4 439.6,99.4 450.4,104.5 461.3,102.8 472.1,104.5 483.0,107.9 493.8,116.4 504.7,114.7 515.5,119.8 526.3,123.2 537.2,123.2 548.0,124.9 558.9,128.3 569.7,124.9 580.6,124.9 591.4,128.3 602.3,126.6 613.1,123.2 623.9,119.8 634.8,119.8 645.6,113.0 656.5,118.1 667.3,118.1 678.2,113.0 689.0,118.1 699.9,114.7 710.7,121.5 721.6,118.1 732.4,121.5 743.2,126.6 754.1,130.0 764.9,130.0 775.8,126.6 786.6,123.2 797.5,121.5 808.3,126.6 819.2,126.6 830.0,128.3" fill="none" stroke="#F59E0B" stroke-width="2.5" />
  <text x="52" y="55" fill="#F59E0B" font-size="11" font-weight="bold" font-family="Segoe UI, sans-serif" text-anchor="end">30.0°C</text>
  <text x="52" y="215" fill="#F59E0B" font-size="11" font-weight="bold" font-family="Segoe UI, sans-serif" text-anchor="end">20.0°C</text>
  <text x="838" y="55" fill="#818CF8" font-size="11" font-weight="bold" font-family="Segoe UI, sans-serif" text-anchor="start">100% / 100W</text>
  <text x="838" y="215" fill="#818CF8" font-size="11" font-weight="bold" font-family="Segoe UI, sans-serif" text-anchor="start">0% / 0W</text>
  <rect x="20" y="258" width="12" height="12" fill="#F59E0B" rx="2" />
  <text x="38" y="268" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Ambient Temp (°C)</text>
  <rect x="180" y="258" width="12" height="12" fill="#06B6D4" rx="2" />
  <text x="198" y="268" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Dew Point (°C)</text>
  <rect x="330" y="258" width="12" height="12" fill="#818CF8" rx="2" />
  <text x="348" y="268" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Humidity (%)</text>
  <rect x="480" y="258" width="12" height="12" fill="#FACC15" rx="2" />
  <text x="498" y="268" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Power Draw (W)</text>
  <rect x="660" y="258" width="12" height="12" fill="#EF4444" rx="2" />
  <text x="678" y="268" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Dew Risk (&lt;2.5°C)</text>
</svg>
</div>

---

### 🔭 Equipment & Optical Profile
| Category | Device / Property | Details |
| :--- | :--- | :--- |
| **Camera** | SVBONY SV605CC (06180010) | Resolution: 3008 x 3008 \| Pixel Size: 11.28 µm |
| **Cooling** | Median Temp | **-5.0°C** |
| **Optics** | AT60ED | Focal Length: 360 mm, Aperture: 60 mm (f/6.0) |
| **Pixel Scale** | **6.46 arcsec/px** | Field of View: 324.01' x 324.01' |
| **Mount** | Scorpio | Guider: PHD2 |
| **Filter Wheel** | Manual Filter Wheel | Active Filters: DUAL |
| **Focuser** | Star Focuser Pro ASCOM | Thermal Slope: 6.2 steps/°C |

---

### 🎯 Target Diagnostics: M27
- **Duration:** 5h 21m 13s (20:03 — 01:24) | **Integration:** 4h 45m 0s (95 frames)
- **Filters Used:** DUAL | **Quality Score:** 🌟 **75 / 100**
- **Sub-frame Health:** 95 frames total (**79 Good/Accepted [83.2%]**, **16 Sub-optimal [16.8%]**)

#### Optical & Guiding Performance Summary
| Metric | Min | Max | Mean | Median | StdDev (σ) |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **HFR (px)** | 2.05 | 2.66 | 2.23 | 2.21 | 0.08 |
| **Star Count** | 1 | 633 | 396 | 452 | 152.3 |
| **Total RMS (arcsec)** | 0.09" | 1.19" | **0.30"** | 0.27" | 0.13" |
| **Sensor Temp (°C)** | -5.1°C | -4.8°C | -5.0°C | **-5.0°C** | -- |

#### Real-time Vector Chart Example: HFR & Star Count Profile
<div align="center">
<svg width="100%" height="300" viewBox="0 0 900 300" style="background-color: #121824; border-radius: 8px;" xmlns="http://www.w3.org/2000/svg">
  <text x="40" y="25" fill="#E0E6ED" font-size="14" font-weight="bold" font-family="Segoe UI, sans-serif">HFR &amp; Star Count Profile</text>
  <line x1="60.0" y1="45" x2="60.0" y2="235" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="60.0" y="253" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">20:06</text>
  <line x1="216.0" y1="45" x2="216.0" y2="235" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="216.0" y="253" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">21:57</text>
  <line x1="372.0" y1="45" x2="372.0" y2="235" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="372.0" y="253" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">23:48</text>
  <line x1="528.0" y1="45" x2="528.0" y2="235" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="528.0" y="253" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">01:39</text>
  <line x1="684.0" y1="45" x2="684.0" y2="235" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="684.0" y="253" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">03:30</text>
  <line x1="840.0" y1="45" x2="840.0" y2="235" stroke="#1E293B" stroke-width="1" stroke-dasharray="3,3" />
  <text x="840.0" y="253" fill="#94A3B8" font-size="11" font-family="Segoe UI, sans-serif" text-anchor="middle">05:21</text>
  <line x1="199.3" y1="33" x2="199.3" y2="235" stroke="#9B59B6" stroke-width="2.0" stroke-dasharray="4,3" />
  <text x="199.3" y="30" fill="#C084FC" font-size="11" font-weight="bold" font-family="Segoe UI, sans-serif" text-anchor="middle">🔄 Flip (21:45)</text>
  <line x1="756.8" y1="33" x2="756.8" y2="235" stroke="#9B59B6" stroke-width="2.0" stroke-dasharray="4,3" />
  <text x="756.8" y="30" fill="#C084FC" font-size="11" font-weight="bold" font-family="Segoe UI, sans-serif" text-anchor="middle">🔄 Flip (04:22)</text>
  <circle cx="60.0" cy="170.2" r="3.0" fill="#00D26A" />
  <circle cx="60.0" cy="109.3" r="2.5" fill="#38BDF8" />
  <circle cx="69.0" cy="173.5" r="4.5" fill="#EF4444" />
  <circle cx="69.0" cy="174.5" r="2.5" fill="#38BDF8" />
  <circle cx="77.4" cy="168.6" r="3.0" fill="#00D26A" />
  <circle cx="77.4" cy="106.4" r="2.5" fill="#38BDF8" />
  <circle cx="86.2" cy="171.9" r="3.0" fill="#00D26A" />
  <circle cx="86.2" cy="107.0" r="2.5" fill="#38BDF8" />
  <circle cx="95.1" cy="170.2" r="3.0" fill="#00D26A" />
  <circle cx="95.1" cy="107.0" r="2.5" fill="#38BDF8" />
  <circle cx="103.5" cy="165.3" r="3.0" fill="#00D26A" />
  <circle cx="103.5" cy="115.5" r="2.5" fill="#38BDF8" />
  <circle cx="112.3" cy="168.6" r="3.0" fill="#00D26A" />
  <circle cx="112.3" cy="107.0" r="2.5" fill="#38BDF8" />
  <circle cx="121.2" cy="173.5" r="4.5" fill="#EF4444" />
  <circle cx="121.2" cy="209.7" r="2.5" fill="#38BDF8" />
  <circle cx="130.4" cy="166.9" r="3.0" fill="#00D26A" />
  <circle cx="130.4" cy="101.6" r="2.5" fill="#38BDF8" />
  <circle cx="139.2" cy="171.9" r="3.0" fill="#00D26A" />
  <circle cx="139.2" cy="106.7" r="2.5" fill="#38BDF8" />
  <circle cx="156.1" cy="168.6" r="3.0" fill="#00D26A" />
  <circle cx="156.1" cy="146.1" r="2.5" fill="#38BDF8" />
  <circle cx="164.5" cy="165.3" r="3.0" fill="#00D26A" />
  <circle cx="164.5" cy="103.0" r="2.5" fill="#38BDF8" />
  <circle cx="173.4" cy="165.3" r="3.0" fill="#00D26A" />
  <circle cx="173.4" cy="103.3" r="2.5" fill="#38BDF8" />
  <circle cx="182.2" cy="171.9" r="3.0" fill="#00D26A" />
  <circle cx="182.2" cy="92.0" r="2.5" fill="#38BDF8" />
  <circle cx="190.6" cy="173.5" r="3.0" fill="#00D26A" />
  <circle cx="190.6" cy="88.3" r="2.5" fill="#38BDF8" />
  <circle cx="199.5" cy="173.5" r="3.0" fill="#00D26A" />
  <circle cx="199.5" cy="89.4" r="2.5" fill="#38BDF8" />
  <circle cx="215.4" cy="153.7" r="3.0" fill="#00D26A" />
  <circle cx="215.4" cy="69.8" r="2.5" fill="#38BDF8" />
  <circle cx="223.8" cy="160.3" r="3.0" fill="#00D26A" />
  <circle cx="223.8" cy="71.8" r="2.5" fill="#38BDF8" />
  <circle cx="232.7" cy="160.3" r="3.0" fill="#00D26A" />
  <circle cx="232.7" cy="78.4" r="2.5" fill="#38BDF8" />
  <circle cx="249.0" cy="175.2" r="3.0" fill="#00D26A" />
  <circle cx="249.0" cy="107.6" r="2.5" fill="#38BDF8" />
  <circle cx="257.5" cy="173.5" r="3.0" fill="#00D26A" />
  <circle cx="257.5" cy="104.7" r="2.5" fill="#38BDF8" />
  <circle cx="266.3" cy="180.1" r="3.0" fill="#00D26A" />
  <circle cx="266.3" cy="90.6" r="2.5" fill="#38BDF8" />
  <circle cx="275.1" cy="173.5" r="3.0" fill="#00D26A" />
  <circle cx="275.1" cy="106.7" r="2.5" fill="#38BDF8" />
  <circle cx="283.5" cy="175.2" r="3.0" fill="#00D26A" />
  <circle cx="283.5" cy="99.3" r="2.5" fill="#38BDF8" />
  <circle cx="292.3" cy="171.9" r="3.0" fill="#00D26A" />
  <circle cx="292.3" cy="143.0" r="2.5" fill="#38BDF8" />
  <circle cx="301.1" cy="180.1" r="3.0" fill="#00D26A" />
  <circle cx="301.1" cy="158.9" r="2.5" fill="#38BDF8" />
  <circle cx="310.4" cy="176.8" r="3.0" fill="#00D26A" />
  <circle cx="310.4" cy="94.8" r="2.5" fill="#38BDF8" />
  <circle cx="319.4" cy="171.9" r="3.0" fill="#00D26A" />
  <circle cx="319.4" cy="99.9" r="2.5" fill="#38BDF8" />
  <circle cx="328.2" cy="175.2" r="3.0" fill="#00D26A" />
  <circle cx="328.2" cy="122.3" r="2.5" fill="#38BDF8" />
  <circle cx="345.2" cy="153.7" r="4.5" fill="#EF4444" />
  <circle cx="345.2" cy="233.8" r="2.5" fill="#38BDF8" />
  <circle cx="359.7" cy="170.2" r="3.0" fill="#00D26A" />
  <circle cx="359.7" cy="101.0" r="2.5" fill="#38BDF8" />
  <circle cx="369.7" cy="175.2" r="3.0" fill="#00D26A" />
  <circle cx="369.7" cy="80.1" r="2.5" fill="#38BDF8" />
  <circle cx="378.5" cy="158.7" r="4.5" fill="#EF4444" />
  <circle cx="378.5" cy="179.6" r="2.5" fill="#38BDF8" />
  <circle cx="394.0" cy="176.8" r="3.0" fill="#00D26A" />
  <circle cx="394.0" cy="82.0" r="2.5" fill="#38BDF8" />
  <circle cx="403.1" cy="138.8" r="4.5" fill="#EF4444" />
  <circle cx="403.1" cy="204.0" r="2.5" fill="#38BDF8" />
  <circle cx="413.2" cy="109.1" r="4.5" fill="#EF4444" />
  <circle cx="413.2" cy="172.5" r="2.5" fill="#38BDF8" />
  <circle cx="425.7" cy="165.3" r="4.5" fill="#EF4444" />
  <circle cx="425.7" cy="194.4" r="2.5" fill="#38BDF8" />
  <circle cx="434.5" cy="155.4" r="4.5" fill="#EF4444" />
  <circle cx="434.5" cy="230.7" r="2.5" fill="#38BDF8" />
  <circle cx="444.1" cy="181.8" r="3.0" fill="#00D26A" />
  <circle cx="444.1" cy="90.6" r="2.5" fill="#38BDF8" />
  <circle cx="453.4" cy="175.2" r="3.0" fill="#00D26A" />
  <circle cx="453.4" cy="107.6" r="2.5" fill="#38BDF8" />
  <circle cx="464.3" cy="180.1" r="3.0" fill="#00D26A" />
  <circle cx="464.3" cy="107.3" r="2.5" fill="#38BDF8" />
  <circle cx="472.7" cy="178.5" r="3.0" fill="#00D26A" />
  <circle cx="472.7" cy="114.1" r="2.5" fill="#38BDF8" />
  <circle cx="481.5" cy="171.9" r="3.0" fill="#00D26A" />
  <circle cx="481.5" cy="170.8" r="2.5" fill="#38BDF8" />
  <circle cx="490.3" cy="175.2" r="3.0" fill="#00D26A" />
  <circle cx="490.3" cy="109.3" r="2.5" fill="#38BDF8" />
  <circle cx="498.8" cy="176.8" r="3.0" fill="#00D26A" />
  <circle cx="498.8" cy="107.3" r="2.5" fill="#38BDF8" />
  <circle cx="507.6" cy="162.0" r="3.0" fill="#00D26A" />
  <circle cx="507.6" cy="151.8" r="2.5" fill="#38BDF8" />
  <circle cx="523.6" cy="166.9" r="3.0" fill="#00D26A" />
  <circle cx="523.6" cy="132.5" r="2.5" fill="#38BDF8" />
  <circle cx="532.1" cy="170.2" r="3.0" fill="#00D26A" />
  <circle cx="532.1" cy="145.9" r="2.5" fill="#38BDF8" />
  <circle cx="540.9" cy="168.6" r="3.0" fill="#00D26A" />
  <circle cx="540.9" cy="149.5" r="2.5" fill="#38BDF8" />
  <circle cx="551.5" cy="160.3" r="4.5" fill="#EF4444" />
  <circle cx="551.5" cy="202.6" r="2.5" fill="#38BDF8" />
  <circle cx="559.9" cy="175.2" r="3.0" fill="#00D26A" />
  <circle cx="559.9" cy="170.8" r="2.5" fill="#38BDF8" />
  <circle cx="568.7" cy="176.8" r="3.0" fill="#00D26A" />
  <circle cx="568.7" cy="135.6" r="2.5" fill="#38BDF8" />
  <circle cx="577.6" cy="180.1" r="3.0" fill="#00D26A" />
  <circle cx="577.6" cy="135.1" r="2.5" fill="#38BDF8" />
  <circle cx="586.0" cy="180.1" r="3.0" fill="#00D26A" />
  <circle cx="586.0" cy="134.8" r="2.5" fill="#38BDF8" />
  <circle cx="594.9" cy="175.2" r="3.0" fill="#00D26A" />
  <circle cx="594.9" cy="135.6" r="2.5" fill="#38BDF8" />
  <circle cx="607.4" cy="173.5" r="3.0" fill="#00D26A" />
  <circle cx="607.4" cy="138.2" r="2.5" fill="#38BDF8" />
  <circle cx="615.8" cy="173.5" r="4.5" fill="#EF4444" />
  <circle cx="615.8" cy="205.7" r="2.5" fill="#38BDF8" />
  <circle cx="625.8" cy="181.8" r="4.5" fill="#EF4444" />
  <circle cx="625.8" cy="219.6" r="2.5" fill="#38BDF8" />
  <circle cx="635.4" cy="160.3" r="4.5" fill="#EF4444" />
  <circle cx="635.4" cy="231.5" r="2.5" fill="#38BDF8" />
  <circle cx="650.5" cy="109.1" r="4.5" fill="#EF4444" />
  <circle cx="650.5" cy="234.9" r="2.5" fill="#38BDF8" />
  <circle cx="661.6" cy="170.2" r="3.0" fill="#00D26A" />
  <circle cx="661.6" cy="179.3" r="2.5" fill="#38BDF8" />
  <circle cx="670.4" cy="150.4" r="4.5" fill="#EF4444" />
  <circle cx="670.4" cy="214.8" r="2.5" fill="#38BDF8" />
  <circle cx="679.6" cy="170.2" r="3.0" fill="#00D26A" />
  <circle cx="679.6" cy="133.4" r="2.5" fill="#38BDF8" />
  <circle cx="688.1" cy="148.7" r="3.0" fill="#00D26A" />
  <circle cx="688.1" cy="137.6" r="2.5" fill="#38BDF8" />
  <circle cx="700.5" cy="162.0" r="3.0" fill="#00D26A" />
  <circle cx="700.5" cy="179.0" r="2.5" fill="#38BDF8" />
  <circle cx="709.3" cy="178.5" r="3.0" fill="#00D26A" />
  <circle cx="709.3" cy="123.7" r="2.5" fill="#38BDF8" />
  <circle cx="717.8" cy="170.2" r="3.0" fill="#00D26A" />
  <circle cx="717.8" cy="137.3" r="2.5" fill="#38BDF8" />
  <circle cx="726.6" cy="168.6" r="3.0" fill="#00D26A" />
  <circle cx="726.6" cy="141.9" r="2.5" fill="#38BDF8" />
  <circle cx="735.4" cy="173.5" r="3.0" fill="#00D26A" />
  <circle cx="735.4" cy="133.1" r="2.5" fill="#38BDF8" />
  <circle cx="743.9" cy="173.5" r="3.0" fill="#00D26A" />
  <circle cx="743.9" cy="143.6" r="2.5" fill="#38BDF8" />
  <circle cx="752.6" cy="170.2" r="3.0" fill="#00D26A" />
  <circle cx="752.6" cy="136.5" r="2.5" fill="#38BDF8" />
  <circle cx="767.6" cy="168.6" r="3.0" fill="#00D26A" />
  <circle cx="767.6" cy="112.7" r="2.5" fill="#38BDF8" />
  <circle cx="777.3" cy="148.7" r="4.5" fill="#EF4444" />
  <circle cx="777.3" cy="220.5" r="2.5" fill="#38BDF8" />
  <circle cx="805.1" cy="155.4" r="3.0" fill="#00D26A" />
  <circle cx="805.1" cy="199.2" r="2.5" fill="#38BDF8" />
  <circle cx="813.9" cy="162.0" r="3.0" fill="#00D26A" />
  <circle cx="813.9" cy="206.8" r="2.5" fill="#38BDF8" />
  <circle cx="822.3" cy="115.7" r="4.5" fill="#EF4444" />
  <circle cx="822.3" cy="206.0" r="2.5" fill="#38BDF8" />
  <circle cx="831.2" cy="124.0" r="3.0" fill="#00D26A" />
  <circle cx="831.2" cy="203.4" r="2.5" fill="#38BDF8" />
  <circle cx="840.0" cy="152.1" r="3.0" fill="#00D26A" />
  <circle cx="840.0" cy="199.5" r="2.5" fill="#38BDF8" />
  <polyline points="60.0,170.2 69.0,173.5 77.4,168.6 86.2,171.9 95.1,170.2 103.5,165.3 112.3,168.6 121.2,173.5 130.4,166.9 139.2,171.9 156.1,168.6 164.5,165.3 173.4,165.3 182.2,171.9 190.6,173.5 199.5,173.5 215.4,153.7 223.8,160.3 232.7,160.3 249.0,175.2 257.5,173.5 266.3,180.1 275.1,173.5 283.5,175.2 292.3,171.9 301.1,180.1 310.4,176.8 319.4,171.9 328.2,175.2 345.2,153.7 359.7,170.2 369.7,175.2 378.5,158.7 394.0,176.8 403.1,138.8 413.2,109.1 425.7,165.3 434.5,155.4 444.1,181.8 453.4,175.2 464.3,180.1 472.7,178.5 481.5,171.9 490.3,175.2 498.8,176.8 507.6,162.0 523.6,166.9 532.1,170.2 540.9,168.6 551.5,160.3 559.9,175.2 568.7,176.8 577.6,180.1 586.0,180.1 594.9,175.2 607.4,173.5 615.8,173.5 625.8,181.8 635.4,160.3 650.5,109.1 661.6,170.2 670.4,150.4 679.6,170.2 688.1,148.7 700.5,162.0 709.3,178.5 717.8,170.2 726.6,168.6 735.4,173.5 743.9,173.5 752.6,170.2 767.6,168.6 777.3,148.7 805.1,155.4 813.9,162.0 822.3,115.7 831.2,124.0 840.0,152.1" fill="none" stroke="#00D26A" stroke-width="2.5" />
  <polyline points="60.0,109.3 69.0,174.5 77.4,106.4 86.2,107.0 95.1,107.0 103.5,115.5 112.3,107.0 121.2,209.7 130.4,101.6 139.2,106.7 156.1,146.1 164.5,103.0 173.4,103.3 182.2,92.0 190.6,88.3 199.5,89.4 215.4,69.8 223.8,71.8 232.7,78.4 249.0,107.6 257.5,104.7 266.3,90.6 275.1,106.7 283.5,99.3 292.3,143.0 301.1,158.9 310.4,94.8 319.4,99.9 328.2,122.3 345.2,233.8 359.7,101.0 369.7,80.1 378.5,179.6 394.0,82.0 403.1,204.0 413.2,172.5 425.7,194.4 434.5,230.7 444.1,90.6 453.4,107.6 464.3,107.3 472.7,114.1 481.5,170.8 490.3,109.3 498.8,107.3 507.6,151.8 523.6,132.5 532.1,145.9 540.9,149.5 551.5,202.6 559.9,170.8 568.7,135.6 577.6,135.1 586.0,134.8 594.9,135.6 607.4,138.2 615.8,205.7 625.8,219.6 635.4,231.5 650.5,234.9 661.6,179.3 670.4,214.8 679.6,133.4 688.1,137.6 700.5,179.0 709.3,123.7 717.8,137.3 726.6,141.9 735.4,133.1 743.9,143.6 752.6,136.5 767.6,112.7 777.3,220.5 805.1,199.2 813.9,206.8 822.3,206.0 831.2,203.4 840.0,199.5" fill="none" stroke="#38BDF8" stroke-width="2.0" stroke-dasharray="4,3" />
  <text x="52" y="55" fill="#00D26A" font-size="11" font-weight="bold" font-family="Segoe UI, sans-serif" text-anchor="end">2.98 px</text>
  <text x="52" y="235" fill="#00D26A" font-size="11" font-weight="bold" font-family="Segoe UI, sans-serif" text-anchor="end">1.83 px</text>
  <text x="848" y="55" fill="#38BDF8" font-size="11" font-weight="bold" font-family="Segoe UI, sans-serif" text-anchor="start">672 stars</text>
  <text x="848" y="235" fill="#38BDF8" font-size="11" font-weight="bold" font-family="Segoe UI, sans-serif" text-anchor="start">2 stars</text>
  <rect x="60" y="278" width="12" height="12" fill="#00D26A" rx="2" />
  <text x="78" y="288" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">HFR (px) - Left Axis</text>
  <rect x="230" y="278" width="12" height="12" fill="#38BDF8" rx="2" />
  <text x="248" y="288" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Star Count - Right Axis (Dashed)</text>
  <rect x="450" y="278" width="12" height="12" fill="#EF4444" rx="2" />
  <text x="468" y="288" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Sub-optimal Frame 🔴</text>
  <rect x="620" y="278" width="12" height="12" fill="#9B59B6" rx="2" />
  <text x="638" y="288" fill="#A0AEC0" font-size="12" font-family="Segoe UI, sans-serif">Meridian Flip 🔄</text>
</svg>
</div>

---

#### 🔄 Meridian Flip Diagnostics
| Timestamp | Duration | HFR (Pre → Post) | Star Count (Pre → Post) | Guiding RMS (Pre → Post) | Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **21:45:48** | 5m 01s | 2.22 px → 2.27 px | 448 → 575 | 0.25" → 0.27" | ✅ **Completed Successfully** |

#### 🔍 AutoFocus Diagnostics
| Timestamp | Filter | Temp | Curve Quality (R²) | HFR (Initial → Final \| Δ) | Result |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **21:08:40** | DUAL | 27.0°C | 1.00 | 1.72 px → 1.81 px (+0.09 px) | ✅ Success |
| **22:14:44** | DUAL | 26.8°C | 0.90 | 1.73 px → 1.90 px (+0.17 px) | ✅ Success |
| **23:19:54** | DUAL | 26.3°C | 0.99 | 1.94 px → 1.82 px (-0.12 px) | ✅ Success |

---

## 🏗️ Developer & Architecture Overview

The OCD plugin codebase is structured into clean, decoupled layers:

1. **Models (`Models/`)**:
   - `SessionData.cs`: Master session state tracking sub-sessions, integration time, quality scores, and safety aborts.
   - `TargetSessionData.cs`: Per-target statistics container (`HFR`, `Star Count`, `RMS`, `Anomalies`).
   - `EquipmentDetails.cs` & `EquipmentProfileRecord.cs`: Hardware profile records and optical calculation properties.
   - `MeridianFlipRecord.cs`, `PolarAlignmentRecord.cs`, `FrameRecord.cs`: Fine-grained telemetry records.
2. **Log Ingestion & File Pattern Parsing Engine (`Services/LogParserService.cs` & `Services/NinaFilePatternParserService.cs`)**:
   - Performs chronological multi-log ingestion, template-driven N.I.N.A tag pattern matching (`$$SENSORTEMP$$`, `$$EXPOSURETIME$$`, `$$STARCOUNT$$`, `$$HFR$$`, `$$RMS$$`), regex telemetry extraction, pier-side transition tracking, and sensor resolution fallback lookups.
3. **Statistics & Anomaly Engine (`Services/SessionStatsCalculator.cs`)**:
   - Handles downsampling, Z-score anomaly detection, 30-minute pre/post flip impact analysis, pixel-to-arcsecond RMS conversion, and star count statistics.
4. **SVG Vector Chart Generator (`Services/SvgChartGeneratorService.cs`)**:
   - Generates standalone, responsive vector SVG graphics for execution timelines and dual-axis HFR/Star Count profiles.
5. **Report Writers (`Services/MarkdownReportWriter.cs` & `Services/HtmlReportWriter.cs`)**:
   - Render publication-ready Markdown and HTML report documents.
6. **Sequencer Integration (`Sequencer/OCDSequenceItem.cs`)**:
   - Asynchronous N.I.N.A Advanced Sequencer instruction item with registered custom vector WPF UI icon.

---

## 📄 License

Distributed under the **MIT License**. See `LICENSE` for details.

*Created by Nir Zonshine.*
