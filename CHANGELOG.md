# Changelog

All notable changes to the **Overnight Capture Diagnostics** plugin for N.I.N.A. will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- **Reverse Geocoding Fallbacks**: Expanded OpenStreetMap location name parsing to fall back to regional councils, counties, hamlets, and local district names (e.g., "Hevel Modiin Regional Council, Israel") when city/town names are absent for rural observatory sites.
- **Image Path Parsing Debug Logs**: Added detailed `[OCD Debug]` logging entries for image file path pattern matching and extracted telemetry values when debug logging mode is enabled.

## [v1.0.3.0] - 2026-08-01

### Added
- **Reverse Geocoding**: Automatically resolves rig GPS coordinates (Latitude and Longitude) into human-readable location names (e.g., "Zichron Ya'akov, Israel") via the OpenStreetMap Nominatim API. Falls back to "Observatory Site" if offline.
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
