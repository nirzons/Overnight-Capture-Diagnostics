#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using NirZonshine.NINA.OvernightCaptureDiagnostics.Models;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Services {
    public class SvgChartGeneratorService {
        private static readonly XNamespace SvgNs = "http://www.w3.org/2000/svg";

        public string GenerateTimelineSvg(SessionData session, int width = 900, int height = 180) {
            double paddingLeft = 40;
            double paddingRight = 30;
            double drawWidth = width - paddingLeft - paddingRight;

            var svg = new XElement(SvgNs + "svg",
                new XAttribute("width", "100%"),
                new XAttribute("height", height),
                new XAttribute("viewBox", $"0 0 {width} {height}"),
                new XAttribute("style", "background-color: #121824; border-radius: 8px;")
            );

            // Title
            svg.Add(new XElement(SvgNs + "text",
                new XAttribute("x", paddingLeft),
                new XAttribute("y", 25),
                new XAttribute("fill", "#E0E6ED"),
                new XAttribute("font-size", "14"),
                new XAttribute("font-weight", "bold"),
                new XAttribute("font-family", "Segoe UI, sans-serif"),
                "Session Execution Timeline"
            ));

            var allFrames = session.Targets.SelectMany(t => t.Frames).Where(f => !f.IsCalibrationFrame).OrderBy(f => f.Timestamp).ToList();
            
            DateTime timelineStart = session.SessionStart;
            DateTime timelineEnd = session.SessionEnd;

            if (allFrames.Any()) {
                timelineStart = allFrames.Min(f => f.Timestamp);
                timelineEnd = allFrames.Max(f => f.Timestamp);
            }

            if (timelineEnd <= timelineStart) {
                return svg.ToString();
            }

            double totalSeconds = (timelineEnd - timelineStart).TotalSeconds;
            if (totalSeconds <= 0) totalSeconds = 1;

            double barY = 50;
            double barHeight = 45;

            // Base Background Bar
            svg.Add(new XElement(SvgNs + "rect",
                new XAttribute("x", paddingLeft),
                new XAttribute("y", barY),
                new XAttribute("width", drawWidth),
                new XAttribute("height", barHeight),
                new XAttribute("fill", "#1A2332"),
                new XAttribute("rx", "4")
            ));

            // Render Light Frame Exposure Blocks (Green for Good, Red for Bad/Rejected)
            foreach (var f in allFrames) {
                double startOffsetSec = (f.Timestamp - timelineStart).TotalSeconds;
                double x = paddingLeft + ((startOffsetSec / totalSeconds) * drawWidth);
                double w = Math.Max(2, (f.ExposureSeconds / totalSeconds) * drawWidth);

                string fillColor = (f.IsBadFrame || f.Rejected) ? "#EF4444" : "#00D26A";

                svg.Add(new XElement(SvgNs + "rect",
                    new XAttribute("x", x.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y", barY),
                    new XAttribute("width", w.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("height", barHeight),
                    new XAttribute("fill", fillColor),
                    new XAttribute("opacity", "0.85")
                ));
            }

            // Render Dithering Markers (Amber Circles)
            foreach (var dither in session.DitherEvents) {
                double startOffsetSec = (dither.StartTime - timelineStart).TotalSeconds;
                double x = paddingLeft + ((startOffsetSec / totalSeconds) * drawWidth);

                svg.Add(new XElement(SvgNs + "circle",
                    new XAttribute("cx", x.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("cy", (barY + barHeight / 2).ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("r", "3.0"),
                    new XAttribute("fill", "#F59E0B")
                ));
            }

            // Render Hardware Disconnect / Error Lines (Pink/Red Lines)
            foreach (var err in session.HardwareErrors) {
                double startOffsetSec = (err.Timestamp - timelineStart).TotalSeconds;
                double x = paddingLeft + ((startOffsetSec / totalSeconds) * drawWidth);

                svg.Add(new XElement(SvgNs + "rect",
                    new XAttribute("x", (x - 1.5).ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y", (barY - 6).ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("width", "3"),
                    new XAttribute("height", (barHeight + 12).ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("fill", "#FF0055")
                ));
            }

            // Render AF Blocks (Bright Yellow #FFD166)
            var allAf = session.Targets.SelectMany(t => t.AutofocusRuns).ToList();
            foreach (var af in allAf) {
                double startOffsetSec = (af.Timestamp - timelineStart).TotalSeconds;
                double x = paddingLeft + ((startOffsetSec / totalSeconds) * drawWidth);

                svg.Add(new XElement(SvgNs + "rect",
                    new XAttribute("x", x.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y", barY - 5),
                    new XAttribute("width", "6"),
                    new XAttribute("height", barHeight + 10),
                    new XAttribute("fill", af.Successful ? "#FFD166" : "#EF4444"),
                    new XAttribute("rx", "2")
                ));
            }

            // Render Meridian Flip Blocks (Purple)
            var allFlips = session.Targets.SelectMany(t => t.MeridianFlips).ToList();
            foreach (var flip in allFlips) {
                double startOffsetSec = (flip.Timestamp - timelineStart).TotalSeconds;
                double x = paddingLeft + ((startOffsetSec / totalSeconds) * drawWidth);

                svg.Add(new XElement(SvgNs + "rect",
                    new XAttribute("x", x.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y", barY - 5),
                    new XAttribute("width", "8"),
                    new XAttribute("height", barHeight + 10),
                    new XAttribute("fill", "#9B59B6"),
                    new XAttribute("rx", "2")
                ));
            }

            // Render Terminal Failures (Red Line)
            foreach (var err in session.HardwareErrors.Where(e => e.IsTerminal)) {
                double errSec = (err.Timestamp - timelineStart).TotalSeconds;
                if (errSec >= 0 && errSec <= totalSeconds) {
                    double x = paddingLeft + ((errSec / totalSeconds) * drawWidth);
                    svg.Add(new XElement(SvgNs + "line",
                        new XAttribute("x1", x.ToString("F1", CultureInfo.InvariantCulture)),
                        new XAttribute("y1", 20),
                        new XAttribute("x2", x.ToString("F1", CultureInfo.InvariantCulture)),
                        new XAttribute("y2", barY + barHeight + 10),
                        new XAttribute("stroke", "#EF4444"),
                        new XAttribute("stroke-width", "3"),
                        new XAttribute("stroke-dasharray", "4,2")
                    ));
                    svg.Add(new XElement(SvgNs + "text",
                        new XAttribute("x", x.ToString("F1", CultureInfo.InvariantCulture)),
                        new XAttribute("y", 15),
                        new XAttribute("fill", "#EF4444"),
                        new XAttribute("font-size", "11"),
                        new XAttribute("font-weight", "bold"),
                        new XAttribute("font-family", "Segoe UI, sans-serif"),
                        new XAttribute("text-anchor", "middle"),
                        "TERMINAL FAILURE"
                    ));
                }
            }

            // X-Axis Timestamps & Ticks under Timeline Bar
            int numTicks = 6;
            for (int i = 0; i <= numTicks; i++) {
                double frac = (double)i / numTicks;
                double x = paddingLeft + (frac * drawWidth);
                DateTime tickTime = timelineStart.AddSeconds(frac * totalSeconds);
                string timeLabel = tickTime.ToString("HH:mm");

                svg.Add(new XElement(SvgNs + "line",
                    new XAttribute("x1", x.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y1", (barY + barHeight).ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("x2", x.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y2", (barY + barHeight + 4).ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("stroke", "#475569"),
                    new XAttribute("stroke-width", "1")
                ));

                svg.Add(new XElement(SvgNs + "text",
                    new XAttribute("x", x.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y", (barY + barHeight + 16).ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("fill", "#94A3B8"),
                    new XAttribute("font-size", "11"),
                    new XAttribute("font-family", "Segoe UI, sans-serif"),
                    new XAttribute("text-anchor", "middle"),
                    timeLabel
                ));
            }

            // Legend
            double legendY = 135;
            AddLegendItem(svg, 40, legendY, "#00D26A", "Good Light Exposure");
            AddLegendItem(svg, 185, legendY, "#EF4444", "Sub-optimal / AF Fail");
            AddLegendItem(svg, 340, legendY, "#FFD166", "Autofocus");
            AddLegendItem(svg, 435, legendY, "#9B59B6", "Meridian Flip");
            AddLegendItem(svg, 545, legendY, "#F59E0B", "Dither");
            AddLegendItem(svg, 625, legendY, "#FF0055", "Hardware Event");

            return svg.ToString();
        }

        public string GenerateHfrStarSvg(List<FrameRecord> rawFrames, List<MeridianFlipRecord>? flips = null, int width = 900, int height = 300) {
            var frames = SessionStatsCalculator.DecimateSamples(rawFrames.Where(f => !f.IsCalibrationFrame && f.HFR > 0).OrderBy(f => f.Timestamp).ToList(), 80, f => f.Timestamp);

            var svg = new XElement(SvgNs + "svg",
                new XAttribute("width", "100%"),
                new XAttribute("height", height),
                new XAttribute("viewBox", $"0 0 {width} {height}"),
                new XAttribute("style", "background-color: #121824; border-radius: 8px;")
            );

            // Title
            svg.Add(new XElement(SvgNs + "text",
                new XAttribute("x", 40),
                new XAttribute("y", 25),
                new XAttribute("fill", "#E0E6ED"),
                new XAttribute("font-size", "14"),
                new XAttribute("font-weight", "bold"),
                new XAttribute("font-family", "Segoe UI, sans-serif"),
                "HFR & Star Count Profile"
            ));

            if (!frames.Any()) return svg.ToString();

            DateTime startTime = frames.Min(f => f.Timestamp);
            DateTime endTime = frames.Max(f => f.Timestamp);
            double totalSeconds = (endTime - startTime).TotalSeconds;
            if (totalSeconds <= 0) totalSeconds = 1;

            double pLeft = 60, pRight = 60, pTop = 45, pBottom = 65;
            double drawW = width - pLeft - pRight;
            double drawH = height - pTop - pBottom;

            double maxHfr = frames.Max(f => f.HFR) * 1.15;
            double minHfr = Math.Max(0, frames.Min(f => f.HFR) * 0.85);

            double maxStar = frames.Max(f => f.StarCount) * 1.15;
            double minStar = Math.Max(0, frames.Min(f => f.StarCount) * 0.85);

            // Render Time Ticks & Vertical Grid Lines along X-Axis
            int numTicks = 5;
            for (int t = 0; t <= numTicks; t++) {
                double fraction = (double)t / numTicks;
                double xTick = pLeft + (fraction * drawW);
                DateTime tickTime = startTime.AddSeconds(fraction * totalSeconds);

                svg.Add(new XElement(SvgNs + "line",
                    new XAttribute("x1", xTick.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y1", pTop),
                    new XAttribute("x2", xTick.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y2", pTop + drawH),
                    new XAttribute("stroke", "#1E293B"),
                    new XAttribute("stroke-width", "1"),
                    new XAttribute("stroke-dasharray", "3,3")
                ));

                svg.Add(new XElement(SvgNs + "text",
                    new XAttribute("x", xTick.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y", pTop + drawH + 18),
                    new XAttribute("fill", "#94A3B8"),
                    new XAttribute("font-size", "11"),
                    new XAttribute("font-family", "Segoe UI, sans-serif"),
                    new XAttribute("text-anchor", "middle"),
                    tickTime.ToString("HH:mm")
                ));
            }

            // Render Meridian Flip Vertical Marker Lines (Purple)
            if (flips != null) {
                foreach (var flip in flips) {
                    if (flip.Timestamp >= startTime && flip.Timestamp <= endTime) {
                        double xFlip = pLeft + (((flip.Timestamp - startTime).TotalSeconds / totalSeconds) * drawW);

                        svg.Add(new XElement(SvgNs + "line",
                            new XAttribute("x1", xFlip.ToString("F1", CultureInfo.InvariantCulture)),
                            new XAttribute("y1", pTop - 12),
                            new XAttribute("x2", xFlip.ToString("F1", CultureInfo.InvariantCulture)),
                            new XAttribute("y2", pTop + drawH),
                            new XAttribute("stroke", "#9B59B6"),
                            new XAttribute("stroke-width", "2.0"),
                            new XAttribute("stroke-dasharray", "4,3")
                        ));

                        svg.Add(new XElement(SvgNs + "text",
                            new XAttribute("x", xFlip.ToString("F1", CultureInfo.InvariantCulture)),
                            new XAttribute("y", pTop - 15),
                            new XAttribute("fill", "#C084FC"),
                            new XAttribute("font-size", "11"),
                            new XAttribute("font-weight", "bold"),
                            new XAttribute("font-family", "Segoe UI, sans-serif"),
                            new XAttribute("text-anchor", "middle"),
                            $"🔄 Flip ({flip.Timestamp:HH:mm})"
                        ));
                    }
                }
            }

            var hfrPoints = new List<string>();
            var starPoints = new List<string>();

            for (int i = 0; i < frames.Count; i++) {
                double offsetSec = (frames[i].Timestamp - startTime).TotalSeconds;
                double x = pLeft + ((offsetSec / totalSeconds) * drawW);

                // HFR Calculation
                double hfr = frames[i].HFR;
                double yHfr = pTop + drawH - (((hfr - minHfr) / Math.Max(0.1, maxHfr - minHfr)) * drawH);
                hfrPoints.Add($"{x.ToString("F1", CultureInfo.InvariantCulture)},{yHfr.ToString("F1", CultureInfo.InvariantCulture)}");

                // Star Count Calculation
                double star = frames[i].StarCount;
                double yStar = pTop + drawH - (((star - minStar) / Math.Max(1.0, maxStar - minStar)) * drawH);
                starPoints.Add($"{x.ToString("F1", CultureInfo.InvariantCulture)},{yStar.ToString("F1", CultureInfo.InvariantCulture)}");

                string dotColor = frames[i].IsBadFrame ? "#EF4444" : "#00D26A";
                double dotRadius = frames[i].IsBadFrame ? 4.5 : 3.0;

                // HFR Node Dot (Green for Good, Red for Bad)
                svg.Add(new XElement(SvgNs + "circle",
                    new XAttribute("cx", x.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("cy", yHfr.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("r", dotRadius.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("fill", dotColor)
                ));

                // Star Count Node Dot (Cyan)
                svg.Add(new XElement(SvgNs + "circle",
                    new XAttribute("cx", x.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("cy", yStar.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("r", "2.5"),
                    new XAttribute("fill", "#38BDF8")
                ));
            }

            // HFR Polyline (Green)
            svg.Add(new XElement(SvgNs + "polyline",
                new XAttribute("points", string.Join(" ", hfrPoints)),
                new XAttribute("fill", "none"),
                new XAttribute("stroke", "#00D26A"),
                new XAttribute("stroke-width", "2.5")
            ));

            // Star Count Polyline (Cyan Dashed)
            svg.Add(new XElement(SvgNs + "polyline",
                new XAttribute("points", string.Join(" ", starPoints)),
                new XAttribute("fill", "none"),
                new XAttribute("stroke", "#38BDF8"),
                new XAttribute("stroke-width", "2.0"),
                new XAttribute("stroke-dasharray", "4,3")
            ));

            // Left Y-Axis Labels (HFR)
            svg.Add(new XElement(SvgNs + "text",
                new XAttribute("x", pLeft - 8),
                new XAttribute("y", pTop + 10),
                new XAttribute("fill", "#00D26A"),
                new XAttribute("font-size", "11"),
                new XAttribute("font-weight", "bold"),
                new XAttribute("font-family", "Segoe UI, sans-serif"),
                new XAttribute("text-anchor", "end"),
                $"{maxHfr:F2} px"
            ));

            svg.Add(new XElement(SvgNs + "text",
                new XAttribute("x", pLeft - 8),
                new XAttribute("y", pTop + drawH),
                new XAttribute("fill", "#00D26A"),
                new XAttribute("font-size", "11"),
                new XAttribute("font-weight", "bold"),
                new XAttribute("font-family", "Segoe UI, sans-serif"),
                new XAttribute("text-anchor", "end"),
                $"{minHfr:F2} px"
            ));

            // Right Y-Axis Labels (Star Count)
            svg.Add(new XElement(SvgNs + "text",
                new XAttribute("x", width - pRight + 8),
                new XAttribute("y", pTop + 10),
                new XAttribute("fill", "#38BDF8"),
                new XAttribute("font-size", "11"),
                new XAttribute("font-weight", "bold"),
                new XAttribute("font-family", "Segoe UI, sans-serif"),
                new XAttribute("text-anchor", "start"),
                $"{maxStar:F0} stars"
            ));

            svg.Add(new XElement(SvgNs + "text",
                new XAttribute("x", width - pRight + 8),
                new XAttribute("y", pTop + drawH),
                new XAttribute("fill", "#38BDF8"),
                new XAttribute("font-size", "11"),
                new XAttribute("font-weight", "bold"),
                new XAttribute("font-family", "Segoe UI, sans-serif"),
                new XAttribute("text-anchor", "start"),
                $"{minStar:F0} stars"
            ));

            // Legend
            double legendY = height - 12;
            AddLegendItem(svg, 60, legendY, "#00D26A", "HFR (px) - Left Axis");
            AddLegendItem(svg, 230, legendY, "#38BDF8", "Star Count - Right Axis (Dashed)");
            AddLegendItem(svg, 450, legendY, "#EF4444", "Sub-optimal Frame 🔴");
            AddLegendItem(svg, 620, legendY, "#9B59B6", "Meridian Flip 🔄");

            return svg.ToString();
        }

        public string GenerateEnvironmentalSvg(List<WeatherSample> rawSamples, DateTime sessionStart, DateTime sessionEnd, int width = 900, int height = 280) {
            var validSamples = rawSamples
                .Where(s => (!double.IsNaN(s.AmbientTemperature) && s.AmbientTemperature > -60 && s.AmbientTemperature < 80) ||
                            (!double.IsNaN(s.Humidity) && s.Humidity > 0 && s.Humidity <= 100) ||
                            (!double.IsNaN(s.DewPoint) && s.DewPoint > -60 && s.DewPoint < 80) ||
                            (s.PowerWatts.HasValue && s.PowerWatts.Value >= 0) ||
                            (s.DewHeaterDuty.HasValue && s.DewHeaterDuty.Value >= 0))
                .OrderBy(s => s.Timestamp)
                .ToList();

            if (validSamples.Count < 2) return string.Empty;

            var samples = SessionStatsCalculator.DecimateSamples(validSamples, 80, s => s.Timestamp);
            if (samples.Count < 2) return string.Empty;

            bool hasPower = samples.Any(s => s.PowerWatts.HasValue && s.PowerWatts.Value > 0);

            // Compute Temperature Limits (encompassing both Ambient and Dew Point)
            var allTemps = samples
                .SelectMany(s => new[] { s.AmbientTemperature, s.DewPoint })
                .Where(t => !double.IsNaN(t) && t > -60 && t < 80)
                .ToList();

            bool hasTemps = allTemps.Any();

            var svg = new XElement(SvgNs + "svg",
                new XAttribute("xmlns", SvgNs.NamespaceName),
                new XAttribute("width", "100%"),
                new XAttribute("height", height),
                new XAttribute("viewBox", $"0 0 {width} {height}"),
                new XAttribute("style", "background-color: #121824; border-radius: 8px;")
            );

            // Title
            string titleText = hasPower && hasTemps
                ? "🌡️⚡ Environmental & Power Profile (Temperature, Dew Point, Humidity & Power Draw)"
                : (hasPower
                    ? "⚡ Power & Energy Profile (Instantaneous Power Draw in Watts)"
                    : "🌡️ Environmental Profile (Temperature vs. Humidity & Dew Point)");

            svg.Add(new XElement(SvgNs + "text",
                new XAttribute("x", 40),
                new XAttribute("y", 25),
                new XAttribute("fill", "#E0E6ED"),
                new XAttribute("font-size", "14"),
                new XAttribute("font-weight", "bold"),
                new XAttribute("font-family", "Segoe UI, sans-serif"),
                titleText
            ));

            DateTime startTime = samples.Min(s => s.Timestamp);
            DateTime endTime = samples.Max(s => s.Timestamp);
            double totalSeconds = (endTime - startTime).TotalSeconds;
            if (totalSeconds <= 0) totalSeconds = 1;

            double pLeft = 60, pRight = 70, pTop = 45, pBottom = 65;
            double drawW = width - pLeft - pRight;
            double drawH = height - pTop - pBottom;

            double minTemp = hasTemps ? Math.Floor(allTemps.Min() - 1.5) : 0;
            double maxTemp = hasTemps ? Math.Ceiling(allTemps.Max() + 1.5) : 30;
            if (maxTemp <= minTemp) maxTemp = minTemp + 5;
            double tempRange = maxTemp - minTemp;

            // Humidity & Power Range: 0 to 100 (% / Watts)
            double minRightScale = 0;
            double rightScaleRange = 100;

            // Render Time Ticks & Grid Lines along X-Axis
            int numTicks = 5;
            for (int t = 0; t <= numTicks; t++) {
                double fraction = (double)t / numTicks;
                double xTick = pLeft + (fraction * drawW);
                DateTime tickTime = startTime.AddSeconds(fraction * totalSeconds);

                svg.Add(new XElement(SvgNs + "line",
                    new XAttribute("x1", xTick.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y1", pTop),
                    new XAttribute("x2", xTick.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y2", pTop + drawH),
                    new XAttribute("stroke", "#1E293B"),
                    new XAttribute("stroke-width", "1"),
                    new XAttribute("stroke-dasharray", "3,3")
                ));

                svg.Add(new XElement(SvgNs + "text",
                    new XAttribute("x", xTick.ToString("F1", CultureInfo.InvariantCulture)),
                    new XAttribute("y", pTop + drawH + 18),
                    new XAttribute("fill", "#94A3B8"),
                    new XAttribute("font-size", "11"),
                    new XAttribute("font-family", "Segoe UI, sans-serif"),
                    new XAttribute("text-anchor", "middle"),
                    tickTime.ToString("HH:mm")
                ));
            }

            var tempPoints = new List<string>();
            var dewPoints = new List<string>();
            var humPoints = new List<string>();
            var powerPoints = new List<string>();
            var heaterPoints = new List<string>();

            var heaterSamples = samples.Where(s => s.DewHeaterDuty.HasValue).Select(s => s.DewHeaterDuty!.Value).ToList();
            bool hasDynamicHeater = heaterSamples.Any() && (heaterSamples.Max() - heaterSamples.Min() > 0.01) && (heaterSamples.Max() > 0);

            foreach (var s in samples) {
                double sec = (s.Timestamp - startTime).TotalSeconds;
                double x = pLeft + ((sec / totalSeconds) * drawW);

                // Ambient Temp Point
                if (!double.IsNaN(s.AmbientTemperature) && s.AmbientTemperature > -60 && s.AmbientTemperature < 80) {
                    double yTemp = pTop + drawH - (((s.AmbientTemperature - minTemp) / tempRange) * drawH);
                    tempPoints.Add($"{x.ToString("F1", CultureInfo.InvariantCulture)},{yTemp.ToString("F1", CultureInfo.InvariantCulture)}");
                }

                // Dew Point
                if (!double.IsNaN(s.DewPoint) && s.DewPoint > -60 && s.DewPoint < 80) {
                    double yDew = pTop + drawH - (((s.DewPoint - minTemp) / tempRange) * drawH);
                    dewPoints.Add($"{x.ToString("F1", CultureInfo.InvariantCulture)},{yDew.ToString("F1", CultureInfo.InvariantCulture)}");
                }

                // Humidity Point
                if (!double.IsNaN(s.Humidity) && s.Humidity > 0 && s.Humidity <= 100) {
                    double yHum = pTop + drawH - (((s.Humidity - minRightScale) / rightScaleRange) * drawH);
                    humPoints.Add($"{x.ToString("F1", CultureInfo.InvariantCulture)},{yHum.ToString("F1", CultureInfo.InvariantCulture)}");
                }

                // Power Point (Watts)
                if (s.PowerWatts.HasValue && s.PowerWatts.Value >= 0) {
                    double clampedPower = Math.Min(100.0, s.PowerWatts.Value);
                    double yPower = pTop + drawH - (((clampedPower - minRightScale) / rightScaleRange) * drawH);
                    powerPoints.Add($"{x.ToString("F1", CultureInfo.InvariantCulture)},{yPower.ToString("F1", CultureInfo.InvariantCulture)}");
                }

                // Dew Heater Duty Point (Pink / Coral)
                if (hasDynamicHeater && s.DewHeaterDuty.HasValue) {
                    double clampedHeater = Math.Clamp(s.DewHeaterDuty.Value, 0.0, 100.0);
                    double yHeater = pTop + drawH - (((clampedHeater - minRightScale) / rightScaleRange) * drawH);
                    heaterPoints.Add($"{x.ToString("F1", CultureInfo.InvariantCulture)},{yHeater.ToString("F1", CultureInfo.InvariantCulture)}");
                }

                // Dew Risk Highlight (Circle if Ambient - DewPoint < 2.5)
                if (!double.IsNaN(s.AmbientTemperature) && !double.IsNaN(s.DewPoint)) {
                    double margin = s.AmbientTemperature - s.DewPoint;
                    if (margin < 2.5 && margin >= -20) {
                        double yTemp = pTop + drawH - (((s.AmbientTemperature - minTemp) / tempRange) * drawH);
                        svg.Add(new XElement(SvgNs + "circle",
                            new XAttribute("cx", x.ToString("F1", CultureInfo.InvariantCulture)),
                            new XAttribute("cy", yTemp.ToString("F1", CultureInfo.InvariantCulture)),
                            new XAttribute("r", "3.5"),
                            new XAttribute("fill", "#EF4444"),
                            new XAttribute("opacity", "0.85")
                        ));
                    }
                }
            }

            // Power Draw Polyline (Gold Dash-Dot)
            if (powerPoints.Count >= 2) {
                svg.Add(new XElement(SvgNs + "polyline",
                    new XAttribute("points", string.Join(" ", powerPoints)),
                    new XAttribute("fill", "none"),
                    new XAttribute("stroke", "#FACC15"),
                    new XAttribute("stroke-width", "2.0"),
                    new XAttribute("stroke-dasharray", "6,2,2,2")
                ));
            }

            // Dew Heater Polyline (Pink / Coral Dotted - if dynamic)
            if (heaterPoints.Count >= 2) {
                svg.Add(new XElement(SvgNs + "polyline",
                    new XAttribute("points", string.Join(" ", heaterPoints)),
                    new XAttribute("fill", "none"),
                    new XAttribute("stroke", "#EC4899"),
                    new XAttribute("stroke-width", "2.0"),
                    new XAttribute("stroke-dasharray", "3,3")
                ));
            }

            // Relative Humidity Polyline (Purple Dashed)
            if (humPoints.Count >= 2) {
                svg.Add(new XElement(SvgNs + "polyline",
                    new XAttribute("points", string.Join(" ", humPoints)),
                    new XAttribute("fill", "none"),
                    new XAttribute("stroke", "#818CF8"),
                    new XAttribute("stroke-width", "2.0"),
                    new XAttribute("stroke-dasharray", "4,3")
                ));
            }

            // Dew Point Polyline (Teal / Cyan)
            if (dewPoints.Count >= 2) {
                svg.Add(new XElement(SvgNs + "polyline",
                    new XAttribute("points", string.Join(" ", dewPoints)),
                    new XAttribute("fill", "none"),
                    new XAttribute("stroke", "#06B6D4"),
                    new XAttribute("stroke-width", "2.0")
                ));
            }

            // Ambient Temperature Polyline (Amber / Orange)
            if (tempPoints.Count >= 2) {
                svg.Add(new XElement(SvgNs + "polyline",
                    new XAttribute("points", string.Join(" ", tempPoints)),
                    new XAttribute("fill", "none"),
                    new XAttribute("stroke", "#F59E0B"),
                    new XAttribute("stroke-width", "2.5")
                ));
            }

            // Left Y-Axis Labels (Temperature °C - only if temperature telemetry is present)
            if (hasTemps) {
                svg.Add(new XElement(SvgNs + "text",
                    new XAttribute("x", pLeft - 8),
                    new XAttribute("y", pTop + 10),
                    new XAttribute("fill", "#F59E0B"),
                    new XAttribute("font-size", "11"),
                    new XAttribute("font-weight", "bold"),
                    new XAttribute("font-family", "Segoe UI, sans-serif"),
                    new XAttribute("text-anchor", "end"),
                    $"{maxTemp:F1}°C"
                ));

                svg.Add(new XElement(SvgNs + "text",
                    new XAttribute("x", pLeft - 8),
                    new XAttribute("y", pTop + drawH),
                    new XAttribute("fill", "#F59E0B"),
                    new XAttribute("font-size", "11"),
                    new XAttribute("font-weight", "bold"),
                    new XAttribute("font-family", "Segoe UI, sans-serif"),
                    new XAttribute("text-anchor", "end"),
                    $"{minTemp:F1}°C"
                ));
            }

            // Right Y-Axis Labels (Humidity % / Power W)
            string topLabel = (hasTemps && hasPower) ? "100% / 100W" : (hasPower ? "100W" : "100%");
            string bottomLabel = (hasTemps && hasPower) ? "0% / 0W" : (hasPower ? "0W" : "0%");

            svg.Add(new XElement(SvgNs + "text",
                new XAttribute("x", width - pRight + 8),
                new XAttribute("y", pTop + 10),
                new XAttribute("fill", "#818CF8"),
                new XAttribute("font-size", "11"),
                new XAttribute("font-weight", "bold"),
                new XAttribute("font-family", "Segoe UI, sans-serif"),
                new XAttribute("text-anchor", "start"),
                topLabel
            ));

            svg.Add(new XElement(SvgNs + "text",
                new XAttribute("x", width - pRight + 8),
                new XAttribute("y", pTop + drawH),
                new XAttribute("fill", "#818CF8"),
                new XAttribute("font-size", "11"),
                new XAttribute("font-weight", "bold"),
                new XAttribute("font-family", "Segoe UI, sans-serif"),
                new XAttribute("text-anchor", "start"),
                bottomLabel
            ));

            // Legend
            double legendY = height - 12;
            if (hasTemps) {
                if (hasDynamicHeater) {
                    AddLegendItem(svg, 10, legendY, "#F59E0B", "Ambient Temp (°C)");
                    AddLegendItem(svg, 150, legendY, "#06B6D4", "Dew Point (°C)");
                    AddLegendItem(svg, 275, legendY, "#818CF8", "Humidity (%)");
                    if (hasPower) {
                        AddLegendItem(svg, 395, legendY, "#FACC15", "Power (W)");
                        AddLegendItem(svg, 495, legendY, "#EC4899", "Dew Heater (%)");
                        AddLegendItem(svg, 640, legendY, "#EF4444", "Dew Risk (<2.5°C)");
                    } else {
                        AddLegendItem(svg, 400, legendY, "#EC4899", "Dew Heater (%)");
                        AddLegendItem(svg, 560, legendY, "#EF4444", "Dew Risk (<2.5°C)");
                    }
                } else if (hasPower) {
                    AddLegendItem(svg, 20, legendY, "#F59E0B", "Ambient Temp (°C)");
                    AddLegendItem(svg, 180, legendY, "#06B6D4", "Dew Point (°C)");
                    AddLegendItem(svg, 330, legendY, "#818CF8", "Humidity (%)");
                    AddLegendItem(svg, 480, legendY, "#FACC15", "Power Draw (W)");
                    AddLegendItem(svg, 660, legendY, "#EF4444", "Dew Risk (<2.5°C)");
                } else {
                    AddLegendItem(svg, 40, legendY, "#F59E0B", "Ambient Temp (°C) - Left Axis");
                    AddLegendItem(svg, 250, legendY, "#06B6D4", "Dew Point (°C) - Left Axis");
                    AddLegendItem(svg, 430, legendY, "#818CF8", "Humidity (%) - Right Axis (Dashed)");
                    AddLegendItem(svg, 670, legendY, "#EF4444", "Dew Risk Zone (<2.5°C)");
                }
            } else if (hasPower) {
                AddLegendItem(svg, 200, legendY, "#FACC15", "⚡ Instantaneous Power Draw (Watts)");
                if (hasDynamicHeater) {
                    AddLegendItem(svg, 480, legendY, "#EC4899", "Dew Heater Duty (%)");
                }
            }

            return svg.ToString();
        }

        private static void AddLegendItem(XElement svg, double x, double y, string color, string label) {
            svg.Add(new XElement(SvgNs + "rect",
                new XAttribute("x", x),
                new XAttribute("y", y - 10),
                new XAttribute("width", "12"),
                new XAttribute("height", "12"),
                new XAttribute("fill", color),
                new XAttribute("rx", "2")
            ));

            svg.Add(new XElement(SvgNs + "text",
                new XAttribute("x", x + 18),
                new XAttribute("y", y),
                new XAttribute("fill", "#A0AEC0"),
                new XAttribute("font-size", "12"),
                new XAttribute("font-family", "Segoe UI, sans-serif"),
                label
            ));
        }
    }
}
