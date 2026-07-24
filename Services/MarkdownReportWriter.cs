#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NirZonshine.NINA.OvernightCaptureDiagnostics.Models;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Services {
    public class MarkdownReportWriter {
        public string GenerateMarkdownReport(SessionData session, SvgChartGeneratorService chartService) {
            var sb = new StringBuilder();

            var allLightFrames = session.Targets.SelectMany(t => t.Frames.Where(f => !f.IsCalibrationFrame)).ToList();
            var allFlips = session.Targets.SelectMany(t => t.MeridianFlips).ToList();
            string hfrChartSvg = chartService.GenerateHfrStarSvg(allLightFrames, allFlips);
            string timelineSvg = chartService.GenerateTimelineSvg(session);

            string analysisType = session.IsLiveSession ? "Live Session Analysis" : "Historic Session Analysis";
            string siteInfo = (session.Equipment.SiteLatitude != 0 || session.Equipment.SiteLongitude != 0)
                ? $"{session.Equipment.SiteName} ({session.Equipment.SiteLatitude:F2}° N, {session.Equipment.SiteLongitude:F2}° E)"
                : session.Equipment.SiteName;

            string startStr = session.SessionStart != default ? session.SessionStart.ToString("yyyy-MM-dd HH:mm:ss") : "--";
            string endStr = session.SessionEnd != default ? session.SessionEnd.ToString("yyyy-MM-dd HH:mm:ss") : "--";
            string elapsedStr = FormatTimeSpan(session.TotalSessionDuration);
            string integrationStr = FormatTimeSpan(TimeSpan.FromSeconds(session.TotalNightIntegrationSeconds));

            string firstLightStr = session.FirstLightTimestamp.HasValue ? session.FirstLightTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A";
            string lastLightStr = session.LastLightTimestamp.HasValue ? session.LastLightTimestamp.Value.ToString("yyyy-MM-dd HH:mm:ss") : "N/A";

            sb.AppendLine($"# 🔭 Overnight Capture Diagnostics Report v1.0.1 ({analysisType})");
            sb.AppendLine($"> **Plugin Version:** v1.0.1 | **Session Date:** {session.SessionStart:yyyy-MM-dd} | **Session Start:** {startStr} | **Session End:** {endStr} (**Span:** {elapsedStr})");
            sb.AppendLine($"> **First Light Captured:** {firstLightStr} | **Last Light Captured:** {lastLightStr}");
            sb.AppendLine($"> **Site:** {siteInfo} | **Night Score:** 🌟 **{session.MasterQualityScore:F0} / 100** | **Total Integration:** {integrationStr} (**{session.ImagingEfficiencyPercent:F1}% Efficiency**)");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            sb.AppendLine("## ⏱️ Session Execution Timeline");
            sb.AppendLine("<div align=\"center\">");
            sb.AppendLine(timelineSvg);
            sb.AppendLine("</div>");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();

            sb.AppendLine("## ⚙️ Equipment & Optical Profile");
            if (session.EquipmentProfiles.Count > 1) {
                foreach (var prof in session.EquipmentProfiles) {
                    var eq = prof.Equipment;
                    sb.AppendLine($"### ⚙️ {prof.ProfileName} ({prof.StartTime:HH:mm} - {prof.EndTime:HH:mm})");
                    sb.AppendLine("| Category | Device / Property | Details |");
                    sb.AppendLine("| :--- | :--- | :--- |");
                    sb.AppendLine($"| **Camera** | {eq.CameraName} | Resolution: {eq.CameraWidth} x {eq.CameraHeight} | Pixel Size: {eq.PixelSizeMicrons:F2} µm |");
                    sb.AppendLine($"| **Optics** | {eq.TelescopeName} | Focal Length: {eq.FocalLengthMm:F0} mm | Aperture: {eq.ApertureMm:F0} mm | f/{eq.FocalRatio:F1} |");
                    sb.AppendLine($"| **Pixel Scale** | **{eq.PixelScaleArcsec:F2} arcsec/px** | Field of View: {eq.FovWidthArcmin:F2}' x {eq.FovHeightArcmin:F2}' |");
                    sb.AppendLine($"| **Mount** | {eq.MountName} | Guider: {eq.GuiderName} |");
                    sb.AppendLine($"| **Filter Wheel** | {eq.FilterWheelName} | Active Filters: {session.FiltersUsedFormatted} |");
                    sb.AppendLine($"| **Focuser** | {eq.FocuserName} | Thermal Slope: {session.ThermalFocusSlopeStepsPerDegree:F1} steps/°C |");
                    sb.AppendLine();
                }
            } else {
                var eq = session.Equipment;
                sb.AppendLine("| Category | Device / Property | Details |");
                sb.AppendLine("| :--- | :--- | :--- |");
                sb.AppendLine($"| **Camera** | {eq.CameraName} | Resolution: {eq.CameraWidth} x {eq.CameraHeight} | Pixel Size: {eq.PixelSizeMicrons:F2} µm |");
                sb.AppendLine($"| **Optics** | {eq.TelescopeName} | Focal Length: {eq.FocalLengthMm:F0} mm | Aperture: {eq.ApertureMm:F0} mm | f/{eq.FocalRatio:F1} |");
                sb.AppendLine($"| **Pixel Scale** | **{eq.PixelScaleArcsec:F2} arcsec/px** | Field of View: {eq.FovWidthArcmin:F2}' x {eq.FovHeightArcmin:F2}' |");
                sb.AppendLine($"| **Mount** | {eq.MountName} | Guider: {eq.GuiderName} |");
                sb.AppendLine($"| **Filter Wheel** | {eq.FilterWheelName} | Active Filters: {session.FiltersUsedFormatted} |");
                sb.AppendLine($"| **Focuser** | {eq.FocuserName} | Thermal Slope: {session.ThermalFocusSlopeStepsPerDegree:F1} steps/°C |");
                sb.AppendLine();
            }

            var displayPolar = GetDisplayPolarAlignments(session.PolarAlignments);
            if (displayPolar.Any()) {
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine("## 🧭 Polar Alignment Diagnostics");
                sb.AppendLine("| Timestamp | Stage / Source | Altitude Error | Azimuth Error | Total Polar Error |");
                sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
                foreach (var pa in displayPolar) {
                    sb.AppendLine($"| {pa.Timestamp:HH:mm:ss} | **{pa.SourcePlugin}** | {pa.AltitudeErrorFormatted} | {pa.AzimuthErrorFormatted} | **{pa.TotalErrorFormatted}** ({pa.TotalErrorArcmin:F1}') |");
                }
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(hfrChartSvg)) {
                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine("## 📈 HFR & Star Count Profile");
                sb.AppendLine("<div align=\"center\">");
                sb.AppendLine(hfrChartSvg);
                sb.AppendLine("</div>");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
            double storageGb = session.TotalStorageBytes / (1024.0 * 1024.0 * 1024.0);
            sb.AppendLine("## ⏱️ Capture & Overhead Breakdown");
            sb.AppendLine($"- **Session Execution Window:** {startStr} — {endStr} (Total Elapsed: {elapsedStr})");
            sb.AppendLine($"- **First Light Frame Captured:** {firstLightStr}");
            sb.AppendLine($"- **Last Light Frame Captured:** {lastLightStr}");
            sb.AppendLine($"- **Total Night Integration:** {integrationStr} (**{session.ImagingEfficiencyPercent:F1}% Efficiency**)");
            sb.AppendLine($"- **Total Overhead Time:** {FormatTimeSpan(TimeSpan.FromSeconds(session.TotalOverheadSeconds))}");
            sb.AppendLine($"- **Estimated Storage Consumed:** **{storageGb:F2} GB** ({session.Targets.Sum(t => t.Frames.Count)} total frames)");
            sb.AppendLine();

            if (session.HardwareErrors.Any()) {
                sb.AppendLine("### ⚠️ Hardware Disconnects & Critical Events");
                sb.AppendLine("| Timestamp | Device / Component | Event Type | Details / Message |");
                sb.AppendLine("| :--- | :--- | :--- | :--- |");
                foreach (var err in session.HardwareErrors.Take(15)) {
                    sb.AppendLine($"| {err.Timestamp:HH:mm:ss} | **{err.DeviceName}** | `{err.ErrorType}` | {err.Message} |");
                }
                sb.AppendLine();
            }

            if (session.CalibrationFrames.Any()) {
                sb.AppendLine("### 🧪 Calibration & Utility Frames Quarantine");
                sb.AppendLine("| Frame Type | Frame Count | Exposure Time | Total Footprint |");
                sb.AppendLine("| :--- | :--- | :--- | :--- |");
                foreach (var cal in session.CalibrationFrames) {
                    sb.AppendLine($"| **{cal.FrameType}** | {cal.Count} | {cal.ExposureSeconds:F1}s | {FormatTimeSpan(TimeSpan.FromSeconds(cal.TotalSeconds))} |");
                }
                sb.AppendLine();
            }

            if (session.WeatherSamples.Any()) {
                sb.AppendLine("### 🌡️ Environmental Diagnostics & Ambient Conditions");
                sb.AppendLine("| Metric | Min | Max | Mean / Value | Status / Margin |");
                sb.AppendLine("| :--- | :--- | :--- | :--- | :--- |");
                sb.AppendLine($"| **Ambient Temp (°C)** | {session.AmbientTempMin:F1}°C | {session.AmbientTempMax:F1}°C | **{session.AmbientTempAvg:F1}°C** | Optimal |");
                sb.AppendLine($"| **Relative Humidity (%)** | {session.HumidityMin:F0}% | {session.HumidityMax:F0}% | **{session.HumidityAvg:F0}%** | {(session.HumidityAvg > 85 ? "⚠️ High Humidity" : "Normal")} |");
                sb.AppendLine($"| **Dew Point (°C)** | {session.DewPointMin:F1}°C | {session.DewPointMax:F1}°C | **{session.DewPointAvg:F1}°C** | Dew Margin: {session.MinDewPointMargin:F1}°C |");
                if (session.SqmAvg > 0) {
                    sb.AppendLine($"| **SQM Sky Quality** | -- | -- | **{session.SqmAvg:F2} mag/arcsec²** | Sky Brightness |");
                }
                sb.AppendLine($"| **Dew Heater Status** | -- | -- | **{session.DewHeaterStatus}** | Duty Cycle |");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## 🎯 Target Diagnostics");

            foreach (var target in session.Targets) {
                sb.AppendLine($"### 🎯 Target: {target.TargetName}");
                if (!string.IsNullOrWhiteSpace(target.TargetCoordinates)) sb.AppendLine($"- **Coordinates:** {target.TargetCoordinates}");
                if (!string.IsNullOrWhiteSpace(target.RotatorAngle)) sb.AppendLine($"- **Rotator Position Angle:** {target.RotatorAngle}");
                sb.AppendLine($"- **Duration:** {FormatTimeSpan(target.Duration)} ({target.StartTime:HH:mm} - {target.EndTime:HH:mm})");
                sb.AppendLine($"- **Integration:** {FormatTimeSpan(TimeSpan.FromSeconds(target.TotalIntegrationSeconds))}");
                sb.AppendLine($"- **Filters Used:** {target.FiltersSummary}");
                sb.AppendLine($"- **Sub-frame Health:** {target.TotalLightFrames} frames total (**{target.GoodFrameCount} Good/Accepted [{target.AcceptanceRatePercent:F1}%]**, **{target.BadFrameCount} Sub-optimal [{100.0 - target.AcceptanceRatePercent:F1}%]**)");
                if (target.BadFrameCount > 0) {
                    if (target.BadHfrCount > 0) sb.AppendLine($"  - 🌫️ **HFR Spikes (Focus / Seeing):** {target.BadHfrCount} frames");
                    if (target.BadStarCount > 0) sb.AppendLine($"  - ☁️ **Star Count Drops (Cloud / Obstruction):** {target.BadStarCount} frames");
                    if (target.BadRmsCount > 0) sb.AppendLine($"  - 🌬️ **Guiding RMS Spikes (Wind / Tracking):** {target.BadRmsCount} frames");
                    if (target.ExplicitRejectedCount > 0) sb.AppendLine($"  - 🛑 **Explicit N.I.N.A Rejections:** {target.ExplicitRejectedCount} frames");
                }

                if (target.UnguidedFrameCount > 0) {
                    sb.AppendLine($"- **Unguided Light Frames:** {target.UnguidedFrameCount} frames (no active guiding log)");
                }
                sb.AppendLine($"- **Target Quality Score:** 🌟 **{target.QualityScore:F0} / 100**");
                sb.AppendLine();

                sb.AppendLine("#### Optical & Guiding Performance Summary");
                sb.AppendLine("| Metric | Min | Max | Mean | Median | StdDev (σ) |");
                sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");
                sb.AppendLine($"| **HFR (px)** | {target.HfrMin:F2} | {target.HfrMax:F2} | {target.HfrAvg:F2} | {target.HfrMedian:F2} | {target.HfrStdDev:F2} |");
                sb.AppendLine($"| **Star Count** | {target.StarCountMin} | {target.StarCountMax} | {target.StarCountAvg:F0} | {target.StarCountMedian:F0} | {target.StarCountStdDev:F1} |");

                if (target.GuideRaRmsAvg > 0) {
                    sb.AppendLine($"| **RA RMS (arcsec)** | -- | -- | {target.GuideRaRmsAvg:F2}\" | -- | -- |");
                }
                if (target.GuideDecRmsAvg > 0) {
                    sb.AppendLine($"| **DEC RMS (arcsec)** | -- | -- | {target.GuideDecRmsAvg:F2}\" | -- | -- |");
                }
                if (target.GuideTotalRmsAvg > 0) {
                    sb.AppendLine($"| **Total RMS (arcsec)** | {target.GuideRmsMin:F2}\" | {target.GuideRmsMax:F2}\" | **{target.GuideTotalRmsAvg:F2}\"** | {target.GuideRmsMedian:F2}\" | {target.GuideRmsStdDev:F2}\" |");
                }
                sb.AppendLine();

                if (target.MeridianFlips.Any()) {
                    sb.AppendLine("#### 🔄 Meridian Flip Diagnostics");
                    sb.AppendLine("| Timestamp | Duration | HFR (Pre → Post) | Star Count (Pre → Post) | Guiding RMS (Pre → Post) | Status |");
                    sb.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");
                    foreach (var flip in target.MeridianFlips) {
                        string durStr = FormatTimeSpan(TimeSpan.FromSeconds(flip.DurationSeconds));
                        string statusStr = flip.Successful ? "Completed Successfully" : "Failed";

                        string hfrTrend = (flip.PreFlipHfr > 0 && flip.PostFlipHfr > 0)
                            ? $"{flip.PreFlipHfr:F2} px → {flip.PostFlipHfr:F2} px"
                            : (flip.PreFlipHfr > 0 ? $"{flip.PreFlipHfr:F2} px → --" : $"-- → {flip.PostFlipHfr:F2} px");

                        string starTrend = (flip.PreFlipStarCount > 0 && flip.PostFlipStarCount > 0)
                            ? $"{flip.PreFlipStarCount:F0} → {flip.PostFlipStarCount:F0}"
                            : (flip.PreFlipStarCount > 0 ? $"{flip.PreFlipStarCount:F0} → --" : $"-- → {flip.PostFlipStarCount:F0}");

                        string rmsTrend = (flip.PreFlipRms > 0 && flip.PostFlipRms > 0)
                            ? $"{flip.PreFlipRms:F2}\" → {flip.PostFlipRms:F2}\""
                            : (flip.PreFlipRms > 0 ? $"{flip.PreFlipRms:F2}\" → --" : $"-- → {flip.PostFlipRms:F2}\"");

                        sb.AppendLine($"| {flip.Timestamp:HH:mm:ss} | {durStr} | {hfrTrend} | {starTrend} | {rmsTrend} | ✅ **{statusStr}** |");
                    }
                    sb.AppendLine();
                }

                if (target.Anomalies.Any()) {
                    sb.AppendLine("#### 🚨 Detected Target Anomalies & Sub-frame Health Warnings");

                    // Group anomalies by their primary reason type extracted from Description
                    var reasonOrder = new[] { "Guiding RMS Spike", "Star Count Drop", "HFR Spike", "Explicitly Rejected" };
                    var groups = target.Anomalies
                        .GroupBy(a => {
                            foreach (var key in reasonOrder)
                                if (a.Description.Contains(key, StringComparison.OrdinalIgnoreCase)) return key;
                            return "Other";
                        })
                        .OrderBy(g => {
                            int idx = System.Array.IndexOf(reasonOrder, g.Key);
                            return idx < 0 ? reasonOrder.Length : idx;
                        });

                    var icons = new System.Collections.Generic.Dictionary<string, string> {
                        { "Guiding RMS Spike",   "🌬️ Guiding RMS Spikes" },
                        { "Star Count Drop",     "☁️ Star Count Drops" },
                        { "HFR Spike",           "🔴 HFR Spikes" },
                        { "Explicitly Rejected", "❌ Explicitly Rejected" },
                        { "Other",               "⚠️ Other Anomalies" }
                    };

                    bool hasCritical = target.Anomalies.Any(a => a.Severity == AnomalySeverity.Critical);
                    string topAlertType = hasCritical ? "CAUTION" : "WARNING";

                    foreach (var grp in groups) {
                        string label = icons.TryGetValue(grp.Key, out var l) ? l : $"⚠️ {grp.Key}";
                        var entries = grp.OrderBy(a => a.Timestamp).ToList();
                        sb.AppendLine($"> [!{topAlertType}]");
                        sb.AppendLine($"> **{label}** — {entries.Count} frame(s):");
                        foreach (var a in entries) {
                            // Extract just the value clause from the description for compactness
                            string brief = a.Description
                                .Replace("Frame flagged as sub-optimal: ", "")
                                .Replace(a.Category + ": ", "");
                            // If multiple reasons are joined by "; ", keep only the one matching this group
                            var parts = brief.Split(';');
                            string match = parts.FirstOrDefault(p => p.Contains(grp.Key, StringComparison.OrdinalIgnoreCase))?.Trim() ?? brief.Trim();
                            sb.AppendLine($"> - `{a.Timestamp:HH:mm}` — {match}");
                        }
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        private static List<PolarAlignmentRecord> GetDisplayPolarAlignments(List<PolarAlignmentRecord> records) {
            if (records == null || !records.Any()) {
                return new List<PolarAlignmentRecord>();
            }

            var result = new List<PolarAlignmentRecord>();
            var initial = records.First();
            var final = records.Last();

            result.Add(new PolarAlignmentRecord {
                Timestamp = initial.Timestamp,
                SourcePlugin = initial.SourcePlugin + " (Initial)",
                AltitudeErrorArcmin = initial.AltitudeErrorArcmin,
                AzimuthErrorArcmin = initial.AzimuthErrorArcmin,
                TotalErrorArcmin = initial.TotalErrorArcmin,
                AltitudeErrorFormatted = initial.AltitudeErrorFormatted,
                AzimuthErrorFormatted = initial.AzimuthErrorFormatted,
                TotalErrorFormatted = initial.TotalErrorFormatted
            });

            if (final.Timestamp != initial.Timestamp) {
                result.Add(new PolarAlignmentRecord {
                    Timestamp = final.Timestamp,
                    SourcePlugin = final.SourcePlugin + " (Final)",
                    AltitudeErrorArcmin = final.AltitudeErrorArcmin,
                    AzimuthErrorArcmin = final.AzimuthErrorArcmin,
                    TotalErrorArcmin = final.TotalErrorArcmin,
                    AltitudeErrorFormatted = final.AltitudeErrorFormatted,
                    AzimuthErrorFormatted = final.AzimuthErrorFormatted,
                    TotalErrorFormatted = final.TotalErrorFormatted
                });
            }

            return result;
        }

        private static string FormatTimeSpan(TimeSpan ts) {
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        }
    }
}
