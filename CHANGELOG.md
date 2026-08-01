# Changelog

All notable changes to the **Overnight Capture Diagnostics** plugin for N.I.N.A. will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
