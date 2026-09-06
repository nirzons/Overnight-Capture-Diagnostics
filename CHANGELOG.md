# Changelog

All notable changes to the **Overnight Capture Diagnostics** plugin for N.I.N.A. will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v1.0.6.4] - 2026-09-06

### Added
- **Location Privacy Controls**: Added a "Resolve Location" sequencer checkbox (disabled by default) to prevent automated outbound OpenStreetMap network requests and protect rig location privacy.
- **Custom Location Label**: Added a configurable "Location Name" field in the sequencer block allowing users to specify a custom label (e.g., "Home", "Texas", "Dark Sky Site") when online geocoding is disabled.
- **Strict Coordinate Redaction**: Completely removed exact GPS coordinates (latitude/longitude) from report executive summaries across both Markdown and HTML diagnostic reports, displaying only the sanitized site name.

## [v1.0.6.3] - 2026-09-04

### Added
- **Active Profile In-Memory Pattern Injection**: `OCDSequenceItem` now reads `ProfileService.ActiveProfile.ImageFileSettings.FilePattern` directly from N.I.N.A.'s active in-memory profile and passes it straight into `ParseLogFiles`, guaranteeing 100% accurate token extraction during live runs without disk-search guessing.
- **Extended N.I.N.A. Token Dictionary**: Added comprehensive token translation in `NinaFilePatternParserService` for `$$RMSARCSEC$$`, `$$RMSPIXELS$$`, `$$ECCENTRICITY$$`, `$$FWHMARCSEC$$`, `$$FWHMPIXELS$$`, `$$FWHM$$`, `$$CAMERA$$`, `$$TELESCOPE$$`, `$$ROTATORANGLE$$`, `$$READOUTMODE$$`, `$$SITENAME$$`, `$$TARGETRA$$`, `$$TARGETDEC$$`, `$$ALTITUDE$$`, `$$AZIMUTH$$`, `$$AIRMASS$$`, `$$MOONPHASE$$`, `$$MOONALTITUDE$$`, `$$MOONILLUMINATION$$`, `$$ROW$$`, and `$$COL$$`.
- **Standalone Power & Energy Profile Graph**: `SvgChartGeneratorService` now generates a standalone **⚡ Power & Energy Profile** vector chart whenever power or dew heater telemetry is recorded, even if no dedicated weather station device is connected in N.I.N.A.

### Fixed
- **PHD2 Guide Log Parsing & Scaling**: Corrected column index alignment when extracting RA and DEC guide errors (`RARawDistance` and `DECRawDistance`) from raw `PHD2_GuideLog.txt` files, preventing double-scaling errors that previously multiplied arcseconds by pixel scale and caused phantom $60''-120''$ RMS spikes.
- **PHD2 Dither & Settle Filter**: Filtered out unsettled dither excursions and guide star dropouts ($> 15''$) from subframe RMS integration.
- **Dynamic Dew Heater Graphing**: Enabled automatic rendering of dynamic Dew Heater Duty cycles (`#EC4899` pink dashed curve) on the Environmental & Power Profile SVG chart whenever duty cycles vary across the session.
- **Gemini Power Hub Compatibility**: Full compatibility with `DEW6` and `DEW7` channel designations and analog power gauges on Gemini Power & Data Hubs Advanced 3.

## [v1.0.6.2] - 2026-09-02

### Fixed
- **PWM Dew Heater Channel Matching**: Extended switch channel regex to match `PWM1`, `PWM2`, `PWM\w*`, `HEATER\w*`, `DEW\d*`, and `HEATING` to properly identify automatic/manual heating strips on ASCOM power switches.
- **Multi-Heater Duty Aggregation**: Telemetry monitor now evaluates all active dew heater channels (e.g. `PWM1` and `PWM2`) and records the highest active duty cycle.
- **ASCOM Switch 253/255-Level Scaling**: Automatic scaling dividing raw hardware values ($0 - 253$ / $0 - 255$) by the switch's maximum capacity to accurately calculate percentage ($0\% - 100\%$).
- **Dew Point vs. Dew Heater Separation**: Explicitly decoupled the switch's `DEW point(°)` gauge from dew heater output channels.
- **Dew Heater Table Status Formatting**: Computed session-wide Min, Max, and Average duty cycle in the Environmental table, reporting status cleanly as `Constant` (for fixed power) or `Active (Dynamic)` (for varying auto-dew).

## [v1.0.6.1] - 2026-09-01

### Fixed
- **Midnight Sub-Session Rollover & Duration**: Continuous multi-day sub-sessions are now merged across midnight log rotations without artificial truncations at `23:59:59`, preserving accurate start and end times across days (e.g. `19:33 - 05:28`).
- **Equipment State Carry-Forward**: Connected equipment state is preserved across midnight log rolls, preventing unpopulated sensor resolutions or dropped sub-session profiles.
- **Active-Window Hardware Error Filtering**: Disconnect and reconnect events outside active target capture windows (e.g. daytime testing and pre-session cable setup) are filtered out, focusing only on anomalies during active capture.
- **Sub-Degree Polar Alignment Parsing**: Fixed angle string parsing regex to accurately parse sub-degree total errors (e.g. `01' 24"` now correctly evaluates to `1.4'` instead of `0.0'`).
- **Plate-Solve Overwrite Guard**: Protected true sensor pixel size and camera dimensions from binned plate-solve logs ($11.28\ \mu\text{m} \rightarrow 3.76\ \mu\text{m}$).
- **Guiding RMS Anomaly Floor**: Enforced an absolute floor threshold ($1.5''$) to eliminate false-positive guiding spike warnings on stellar sub-arcsecond frames ($0.41''$ on $0.24''$ median).
- **Session-Wide Sensor Cooling Selection**: Equipment profile now computes the true session-wide median sensor temperature across all light frames, preventing uncooled setup exposures from skewing cooling stats.

### Added
- **Integrated Power Draw Profile**: Unified instantaneous Power Draw (Watts) into the Environmental Profile SVG Chart using a dual-purpose $0-100$ scale for Humidity (%) and Power (Watts).

## [v1.0.6.0] - 2026-08-31

### Added
- **Continuous Telemetry Monitor & `Start OCD Telemetry` Instruction**: Introduced non-blocking background telemetry polling for ASCOM power switches and environmental/weather stations (`IWeatherDataMediator`, `ISwitchMediator`).
- **Power & Energy Analytics**: Added trapezoidal integration to calculate Total Energy Consumed (Watt-hours) and Battery Capacity Consumed (Amp-hours), average supply voltage, and peak instantaneous power draw.
- **Dual-Axis Environmental SVG Profile**: Embedded interactive SVG charts plotting Ambient Temperature (°C) and Dew Point (°C) against Relative Humidity (%) with real-time dew risk convergence warnings (< 2.5°C margin).
- **Session Telemetry CSV Export**: Automatically generates timestamped `OCD_Telemetry_*.csv` snapshots containing full-resolution sensor logs.
- **Fail-Safe Background Polling**: Added re-entrancy protection, thread-safe buffering, token cancellation disarm, and guarded Magnus-Tetens dew point derivations.

## [v1.0.5.0] - 2026-08-06

### Added
- **Astronomical Darkness & Solar Position Engine**: Integrated NOAA solar calculation engine (`AstroUtils.cs`) anchored to 12:00 PM (noon) to calculate exact `AstroDusk` and `AstroDawn` timestamps. Includes Tiered Twilight Fallback (Nautical/Civil) for high latitudes during summer solstice.
- **Dark Sky Efficiency Metric ($E_{\text{dark}}$)**: Introduced sub-frame overlap clipping to compute exact integration efficiency against true dark sky duration, eliminating early startup and late parking time penalties.
- **Active Imaging Duty Cycle ($D_{\text{imaging}}$)**: Added sequence tightness metric comparing total integration against active imaging span (first frame start to last frame end).
- **Signed AutoFocus Delta Formatting**: Refactored AutoFocus table headers to `HFR (Initial → Final | Δ)` and replaced generic labels with signed deltas (e.g., `+0.02 px`, `-0.07 px`, `0.00 px`).

## [v1.0.4.0] - 2026-08-06

### Fixed
- **Filter Wheel Parsing**: Tightened filter regex and added string validation to prevent N.I.N.A. sequence condition lines (e.g., `pierWest` / `Automated Flip`) from corrupting filter wheel data on rigs without a filter wheel.

### Added
- **Template-Driven Telemetry & Sensor Temp Parsing**: Enhanced `NinaFilePatternParserService` to extract sensor temperatures (e.g. `-5.00`), HFR, star counts, and RMS directly from N.I.N.A. `$$SENSORTEMP$$` file pattern tags, fallback candidate templates, and raw decimal formats.
- **Reverse Geocoding Fallbacks**: Expanded OpenStreetMap location name parsing to fall back to regional councils, counties, hamlets, and local district names (e.g., "Tarrant County, Texas, USA") when city/town names are absent for rural observatory sites.
- **Image Path Parsing Debug Logs**: Added detailed `[OCD Debug]` logging entries for image file path pattern matching and extracted telemetry values when debug logging mode is enabled.

## [v1.0.3.0] - 2026-08-01

### Added
- **Reverse Geocoding**: Automatically resolves rig GPS coordinates (Latitude and Longitude) into human-readable location names (e.g., "Arlington, Texas, USA") via the OpenStreetMap Nominatim API. Falls back to "Observatory Site" if offline.
- **Advanced Debug Logging**: Added a new UI checkbox to toggle verbose parsing debug logs. Detailed diagnostics on file locks and Regex matching are securely printed to N.I.N.A's native log file.

### Fixed
- **Historic Report Equipment Fallback**: Greatly improved resilience when reading `.profile` equipment files from disk. Implemented `FileShare.ReadWrite` to safely read profiles actively locked by N.I.N.A.
- **In-Memory Equipment Backfill**: Removed restrictive session checks; historic reports now safely leverage N.I.N.A.'s active memory `ProfileService` to backfill missing Telescope and Mount details.
- **Hardware Disconnect Filtering**: Hardware disconnect events are now properly filtered to only display events occurring within the active capture time block.

## [v1.0.0.0] - 2026-07-21

### Added
- **Initial Release of Overnight Capture Diagnostics for N.I.N.A. 3.0+ & 3.2**.
- **Automated Sequencer Instruction**: `[OCD] Overnight Capture Diagnostics` Advanced Sequencer container item.
- **24-Hour Astronomical Observing Window**: Live session analysis automatically isolates the strict 12:00 PM to 12:00 PM observing window without accumulating past log sessions. Historic analysis scans 12:00 PM on the requested date to 12:00 PM the following day.
- **N.I.N.A 3.2 Telemetry Parsing**: Direct extraction of `HFR`, `Star Count`, `Filter`, `Gain`, `Sensor Temp`, and `Guiding RMS` (e.g. `RMS0.24`) from saved image filenames when standalone PHD2 logs are absent.
- **N.I.N.A 3.2 Meridian Flip Diagnostics**: Detects N.I.N.A 3.2 meridian flip routines (`Meridian Flip - Initializing` → `Exiting`) and evaluates imaging quality across a 30-minute pre-flip vs. post-flip window.
- **Multi-Session & Multi-Equipment Support**: Organizes Process restarts within a single night into sub-sessions (`Sub-Session 1`, `Sub-Session 2`) with detailed optical properties (pixel scale, FOV).
- **Embedded Dual-Axis Vector SVG Charts**: Interactive Gantt timeline charts and dual-axis HFR/Star Count profiles.
- **Sub-frame Health Engine**: Z-score anomaly tracking for HFR focus spikes, star count cloud drops, and guiding RMS tracking spikes.
- **Multi-Format Output**: Automatically compiles publication-ready `.md` and dark-mode `.html` diagnostic reports.
