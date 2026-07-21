#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NirZonshine.NINA.OvernightCaptureDiagnostics.Models;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Services {
    public class HtmlReportWriter {
        public string GenerateHtmlReport(SessionData session, SvgChartGeneratorService chartService) {
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

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine($"  <title>Overnight Capture Diagnostics — {session.SessionStart:yyyy-MM-dd}</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #0F172A; color: #F8FAFC; margin: 0; padding: 20px; }");
            sb.AppendLine("    .container { max-width: 1200px; margin: 0 auto; }");
            sb.AppendLine("    .header { background: linear-gradient(135deg, #1E293B, #0F172A); border: 1px solid #334155; border-radius: 12px; padding: 24px; margin-bottom: 24px; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.5); }");
            sb.AppendLine("    h1 { color: #38BDF8; margin: 0 0 10px 0; font-size: 28px; }");
            sb.AppendLine("    .meta { color: #94A3B8; font-size: 14px; margin-bottom: 15px; line-height: 1.6; }");
            sb.AppendLine("    .mode-tag { display: inline-block; background-color: #3B82F6; color: #FFFFFF; font-size: 12px; font-weight: 600; padding: 2px 8px; border-radius: 4px; margin-right: 8px; vertical-align: middle; }");
            sb.AppendLine("    .score-badge { display: inline-block; background-color: #0284C7; color: white; padding: 6px 16px; border-radius: 20px; font-weight: bold; font-size: 16px; margin-right: 12px; }");
            sb.AppendLine("    .card { background-color: #1E293B; border: 1px solid #334155; border-radius: 12px; padding: 20px; margin-bottom: 24px; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.3); }");
            sb.AppendLine("    h2 { color: #F1F5F9; font-size: 20px; border-bottom: 2px solid #334155; padding-bottom: 8px; margin-top: 0; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 12px; font-size: 14px; }");
            sb.AppendLine("    th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid #334155; }");
            sb.AppendLine("    th { background-color: #0F172A; color: #94A3B8; font-weight: 600; }");
            sb.AppendLine("    tr:hover { background-color: #26334D; }");
            sb.AppendLine("    .anomaly-card { background-color: #451A03; border-left: 4px solid #F59E0B; padding: 12px; margin-top: 8px; border-radius: 4px; color: #FEF3C7; }");
            sb.AppendLine("    .anomaly-critical { background-color: #450A0A; border-left-color: #EF4444; color: #FEE2E2; }");
            sb.AppendLine("    .chart-container { background-color: #0F172A; border-radius: 8px; padding: 16px; border: 1px solid #334155; }");
            sb.AppendLine("    .health-pill { display: inline-block; padding: 4px 12px; border-radius: 16px; font-weight: bold; font-size: 13px; margin-right: 8px; }");
            sb.AppendLine("    .health-good { background-color: #064E3B; color: #34D399; border: 1px solid #059669; }");
            sb.AppendLine("    .health-bad { background-color: #7F1D1D; color: #FCA5A5; border: 1px solid #DC2626; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class=\"container\">");

            // Header Section
            sb.AppendLine("    <div class=\"header\">");
            sb.AppendLine($"      <h1>🔭 Overnight Capture Diagnostics <span class=\"mode-tag\">{analysisType}</span></h1>");
            sb.AppendLine($"      <div class=\"meta\">");
            sb.AppendLine($"        <strong>Session Date:</strong> {session.SessionStart:yyyy-MM-dd} &nbsp;|&nbsp; <strong>Session Window:</strong> {startStr} — {endStr} (Span: {elapsedStr})<br>");
            sb.AppendLine($"        <strong>First Light Captured:</strong> {firstLightStr} &nbsp;|&nbsp; <strong>Last Light Captured:</strong> {lastLightStr} &nbsp;|&nbsp; <strong>Site:</strong> {siteInfo}");
            sb.AppendLine($"      </div>");
            sb.AppendLine("      <div>");
            sb.AppendLine($"        <span class=\"score-badge\">🌟 Night Score: {session.MasterQualityScore:F0} / 100</span>");
            sb.AppendLine($"        <span style=\"color: #10B981; font-weight: bold; margin-right: 16px;\">Integration: {integrationStr}</span>");
            sb.AppendLine($"        <span style=\"color: #38BDF8; font-weight: bold;\">Total Elapsed: {elapsedStr}</span>");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");

            // Timeline Chart Card
            sb.AppendLine("    <div class=\"card\">");
            sb.AppendLine("      <h2>⏱️ Session Execution Timeline</h2>");
            sb.AppendLine(timelineSvg);
            sb.AppendLine("    </div>");

            // Polar Alignment Card (if present)
            var displayPolar = GetDisplayPolarAlignments(session.PolarAlignments);
            if (displayPolar.Any()) {
                sb.AppendLine("    <div class=\"card\">");
                sb.AppendLine("      <h2>🧭 Polar Alignment Diagnostics</h2>");
                sb.AppendLine("      <table>");
                sb.AppendLine("        <thead><tr><th>Timestamp</th><th>Stage / Source</th><th>Altitude Error</th><th>Azimuth Error</th><th>Total Polar Error</th></tr></thead>");
                sb.AppendLine("        <tbody>");
                foreach (var pa in displayPolar) {
                    sb.AppendLine($"          <tr><td>{pa.Timestamp:HH:mm:ss}</td><td><strong>{pa.SourcePlugin}</strong></td><td>{pa.AltitudeErrorFormatted}</td><td>{pa.AzimuthErrorFormatted}</td><td><strong style=\"color: #10B981;\">{pa.TotalErrorFormatted}</strong> ({pa.TotalErrorArcmin:F1}')</td></tr>");
                }
                sb.AppendLine("        </tbody>");
                sb.AppendLine("      </table>");
                sb.AppendLine("    </div>");
            }

            // Equipment Profile Card(s)
            sb.AppendLine("    <div class=\"card\">");
            sb.AppendLine("      <h2>⚙️ Equipment & Optical Profile</h2>");
            if (session.EquipmentProfiles.Count > 1) {
                foreach (var prof in session.EquipmentProfiles) {
                    var eq = prof.Equipment;
                    sb.AppendLine($"      <h3 style=\"color: #60A5FA; font-size: 16px; margin-top: 16px;\">⚙️ {prof.ProfileName} ({prof.StartTime:HH:mm} - {prof.EndTime:HH:mm})</h3>");
                    sb.AppendLine("      <table>");
                    sb.AppendLine("        <thead><tr><th>Category</th><th>Device / Property</th><th>Details</th></tr></thead>");
                    sb.AppendLine("        <tbody>");
                    sb.AppendLine($"          <tr><td><strong>Camera</strong></td><td>{eq.CameraName}</td><td>Resolution: {eq.CameraWidth} x {eq.CameraHeight} | Pixel Size: {eq.PixelSizeMicrons:F2} µm</td></tr>");
                    sb.AppendLine($"          <tr><td><strong>Optics</strong></td><td>{eq.TelescopeName}</td><td>Focal Length: {eq.FocalLengthMm:F0} mm | Aperture: {eq.ApertureMm:F0} mm | f/{eq.FocalRatio:F1}</td></tr>");
                    sb.AppendLine($"          <tr><td><strong>Pixel Scale</strong></td><td><strong style=\"color: #60A5FA;\">{eq.PixelScaleArcsec:F2} arcsec/px</strong></td><td>Field of View: {eq.FovWidthArcmin:F2}' x {eq.FovHeightArcmin:F2}'</td></tr>");
                    sb.AppendLine($"          <tr><td><strong>Mount & Guider</strong></td><td>{eq.MountName}</td><td>Guider: {eq.GuiderName}</td></tr>");
                    sb.AppendLine($"          <tr><td><strong>Filter Wheel</strong></td><td>{eq.FilterWheelName}</td><td>Active Filters: {session.FiltersUsedFormatted}</td></tr>");
                    sb.AppendLine($"          <tr><td><strong>Focuser</strong></td><td>{eq.FocuserName}</td><td>Thermal Slope: {session.ThermalFocusSlopeStepsPerDegree:F1} steps/°C</td></tr>");
                    sb.AppendLine("        </tbody>");
                    sb.AppendLine("      </table>");
                }
            } else {
                var eq = session.Equipment;
                sb.AppendLine("      <table>");
                sb.AppendLine("        <thead><tr><th>Category</th><th>Device / Property</th><th>Details</th></tr></thead>");
                sb.AppendLine("        <tbody>");
                sb.AppendLine($"          <tr><td><strong>Camera</strong></td><td>{eq.CameraName}</td><td>Resolution: {eq.CameraWidth} x {eq.CameraHeight} | Pixel Size: {eq.PixelSizeMicrons:F2} µm</td></tr>");
                sb.AppendLine($"          <tr><td><strong>Optics</strong></td><td>{eq.TelescopeName}</td><td>Focal Length: {eq.FocalLengthMm:F0} mm | Aperture: {eq.ApertureMm:F0} mm | f/{eq.FocalRatio:F1}</td></tr>");
                sb.AppendLine($"          <tr><td><strong>Pixel Scale</strong></td><td><strong style=\"color: #60A5FA;\">{eq.PixelScaleArcsec:F2} arcsec/px</strong></td><td>Field of View: {eq.FovWidthArcmin:F2}' x {eq.FovHeightArcmin:F2}'</td></tr>");
                sb.AppendLine($"          <tr><td><strong>Mount & Guider</strong></td><td>{eq.MountName}</td><td>Guider: {eq.GuiderName}</td></tr>");
                sb.AppendLine($"          <tr><td><strong>Filter Wheel</strong></td><td>{eq.FilterWheelName}</td><td>Active Filters: {session.FiltersUsedFormatted}</td></tr>");
                sb.AppendLine($"          <tr><td><strong>Focuser</strong></td><td>{eq.FocuserName}</td><td>Thermal Slope: {session.ThermalFocusSlopeStepsPerDegree:F1} steps/°C</td></tr>");
                sb.AppendLine("        </tbody>");
                sb.AppendLine("      </table>");
            }
            sb.AppendLine("    </div>");

            // HFR & Star Profile Chart Card
            sb.AppendLine("    <div class=\"card\">");
            sb.AppendLine("      <h2>📈 HFR & Star Count Profile</h2>");
            sb.AppendLine("      <div class=\"chart-container\">");
            sb.AppendLine(hfrChartSvg);
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");

            // Target Breakdown Cards
            foreach (var target in session.Targets) {
                sb.AppendLine("    <div class=\"card\">");
                sb.AppendLine($"      <h2>🎯 Target Diagnostics: {target.TargetName}</h2>");
                sb.AppendLine($"      <p><strong>Duration:</strong> {(int)target.Duration.TotalHours}h {target.Duration.Minutes}m | <strong>Filters Used:</strong> {target.FiltersSummary} | <strong>Quality Score:</strong> 🌟 {target.QualityScore:F0}/100</p>");

                sb.AppendLine("      <div style=\"margin-bottom: 16px;\">");
                sb.AppendLine($"        <span class=\"health-pill health-good\">Good/Accepted: {target.GoodFrameCount} ({target.AcceptanceRatePercent:F1}%)</span>");
                if (target.BadFrameCount > 0) {
                    sb.AppendLine($"        <span class=\"health-pill health-bad\">Sub-optimal/Bad: {target.BadFrameCount} ({100.0 - target.AcceptanceRatePercent:F1}%)</span>");
                    sb.AppendLine("<ul style=\"color: #CBD5E1; font-size: 13px; margin-top: 8px;\">");
                    if (target.BadHfrCount > 0) sb.AppendLine($"  <li>🌫️ <strong>HFR Spikes (Focus / Seeing):</strong> {target.BadHfrCount} frames</li>");
                    if (target.BadStarCount > 0) sb.AppendLine($"  <li>☁️ <strong>Star Count Drops (Cloud / Obstruction):</strong> {target.BadStarCount} frames</li>");
                    if (target.BadRmsCount > 0) sb.AppendLine($"  <li>🌬️ <strong>Guiding RMS Spikes (Wind / Tracking):</strong> {target.BadRmsCount} frames</li>");
                    if (target.ExplicitRejectedCount > 0) sb.AppendLine($"  <li>🛑 <strong>Explicit N.I.N.A Rejections:</strong> {target.ExplicitRejectedCount} frames</li>");
                    sb.AppendLine("</ul>");
                }
                sb.AppendLine("      </div>");

                if (target.UnguidedFrameCount > 0) {
                    sb.AppendLine($"      <p style=\"color: #F59E0B;\"><strong>Unguided Light Frames:</strong> {target.UnguidedFrameCount} frames (no active guiding log)</p>");
                }

                sb.AppendLine("      <table>");
                sb.AppendLine("        <thead><tr><th>Metric</th><th>Min</th><th>Max</th><th>Mean</th><th>Median</th><th>StdDev (σ)</th></tr></thead>");
                sb.AppendLine("        <tbody>");
                sb.AppendLine($"          <tr><td><strong>HFR (px)</strong></td><td>{target.HfrMin:F2}</td><td>{target.HfrMax:F2}</td><td>{target.HfrAvg:F2}</td><td>{target.HfrMedian:F2}</td><td>{target.HfrStdDev:F2}</td></tr>");
                sb.AppendLine($"          <tr><td><strong>Star Count</strong></td><td>{target.StarCountMin}</td><td>{target.StarCountMax}</td><td>{target.StarCountAvg:F0}</td><td>{target.StarCountMedian:F0}</td><td>{target.StarCountStdDev:F1}</td></tr>");

                if (target.GuideRaRmsAvg > 0) {
                    sb.AppendLine($"          <tr><td><strong>RA RMS (arcsec)</strong></td><td>--</td><td>--</td><td>{target.GuideRaRmsAvg:F2}\"</td><td>--</td><td>--</td></tr>");
                }
                if (target.GuideDecRmsAvg > 0) {
                    sb.AppendLine($"          <tr><td><strong>DEC RMS (arcsec)</strong></td><td>--</td><td>--</td><td>{target.GuideDecRmsAvg:F2}\"</td><td>--</td><td>--</td></tr>");
                }
                if (target.GuideTotalRmsAvg > 0) {
                    sb.AppendLine($"          <tr><td><strong>Total RMS (arcsec)</strong></td><td>{target.GuideRmsMin:F2}\"</td><td>{target.GuideRmsMax:F2}\"</td><td><strong style=\"color: #38BDF8;\">{target.GuideTotalRmsAvg:F2}\"</strong></td><td>{target.GuideRmsMedian:F2}\"</td><td>{target.GuideRmsStdDev:F2}\"</td></tr>");
                }
                sb.AppendLine("        </tbody>");
                sb.AppendLine("      </table>");

                if (target.MeridianFlips.Any()) {
                    sb.AppendLine("      <h3>🔄 Meridian Flip Diagnostics</h3>");
                    sb.AppendLine("      <table>");
                    sb.AppendLine("        <thead><tr><th>Timestamp</th><th>Duration</th><th>HFR (Pre → Post)</th><th>Star Count (Pre → Post)</th><th>Guiding RMS (Pre → Post)</th><th>Status</th></tr></thead>");
                    sb.AppendLine("        <tbody>");
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

                        sb.AppendLine($"          <tr><td>{flip.Timestamp:HH:mm:ss}</td><td>{durStr}</td><td>{hfrTrend}</td><td>{starTrend}</td><td>{rmsTrend}</td><td><strong style=\"color: #10B981;\">✅ {statusStr}</strong></td></tr>");
                    }
                    sb.AppendLine("        </tbody>");
                    sb.AppendLine("      </table>");
                }

                if (target.Anomalies.Any()) {
                    sb.AppendLine("      <h3>🚨 Detected Target Anomalies & Sub-frame Warnings</h3>");
                    foreach (var a in target.Anomalies.Take(15)) {
                        string cssClass = a.Severity == AnomalySeverity.Critical ? "anomaly-card anomaly-critical" : "anomaly-card";
                        sb.AppendLine($"      <div class=\"{cssClass}\"><strong>{a.Timestamp:HH:mm:ss} — {a.Category}:</strong> {a.Description}</div>");
                    }
                }
                sb.AppendLine("    </div>");
            }

            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

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
