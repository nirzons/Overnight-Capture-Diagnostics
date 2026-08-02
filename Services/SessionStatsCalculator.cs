using System;
using System.Collections.Generic;
using System.Linq;
using NirZonshine.NINA.OvernightCaptureDiagnostics.Models;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Services {
    public class SessionStatsCalculator {

        public void CalculateStatistics(SessionData session) {
            double totalNightIntegration = 0;

            foreach (var target in session.Targets) {
                double pixelScale = session.Equipment.PixelScaleArcsec > 0 ? session.Equipment.PixelScaleArcsec : 2.15;
                CalculateTargetStatistics(target, pixelScale, session.Equipment.CameraTempSetpoint);
                totalNightIntegration += target.TotalIntegrationSeconds;
            }

            session.TotalNightIntegrationSeconds = totalNightIntegration;

            if (session.SessionEnd > session.SessionStart) {
                double totalSecs = (session.SessionEnd - session.SessionStart).TotalSeconds;
                session.TotalOverheadSeconds = Math.Max(0, totalSecs - totalNightIntegration);
            }

            // Calculate Thermal Focus Slope (Steps / °C) across all AF runs
            var allAfRuns = session.Targets.SelectMany(t => t.AutofocusRuns).OrderBy(a => a.Timestamp).ToList();
            if (allAfRuns.Count >= 2) {
                double minTemp = allAfRuns.Min(a => a.Temperature);
                double maxTemp = allAfRuns.Max(a => a.Temperature);
                if (Math.Abs(maxTemp - minTemp) >= 0.5) {
                    var minRun = allAfRuns.First(a => a.Temperature == minTemp);
                    var maxRun = allAfRuns.First(a => a.Temperature == maxTemp);
                    session.ThermalFocusSlopeStepsPerDegree = Math.Abs(maxRun.BestPosition - minRun.BestPosition) / (maxTemp - minTemp);
                }
            }

            // Calculate Master Night Score (0 - 100)
            if (session.Targets.Any()) {
                session.MasterQualityScore = session.Targets.Average(t => t.QualityScore);
            } else {
                session.MasterQualityScore = 100.0;
            }

            // Calculate Storage Size
            long storage = 0;
            foreach (var target in session.Targets) {
                foreach (var frame in target.Frames) {
                    try {
                        var fi = new System.IO.FileInfo(frame.FileName);
                        if (fi.Exists) {
                            storage += fi.Length;
                        }
                    } catch { }
                }
            }
            session.TotalStorageBytes = storage;

            // Calculate Environmental & Weather Summaries
            if (session.WeatherSamples.Any()) {
                session.AmbientTempMin = session.WeatherSamples.Min(w => w.AmbientTemperature);
                session.AmbientTempMax = session.WeatherSamples.Max(w => w.AmbientTemperature);
                session.AmbientTempAvg = session.WeatherSamples.Average(w => w.AmbientTemperature);

                session.HumidityMin = session.WeatherSamples.Min(w => w.Humidity);
                session.HumidityMax = session.WeatherSamples.Max(w => w.Humidity);
                session.HumidityAvg = session.WeatherSamples.Average(w => w.Humidity);

                session.DewPointMin = session.WeatherSamples.Min(w => w.DewPoint);
                session.DewPointMax = session.WeatherSamples.Max(w => w.DewPoint);
                session.DewPointAvg = session.WeatherSamples.Average(w => w.DewPoint);

                var sqmSamples = session.WeatherSamples.Where(w => w.SkyQuality > 0).ToList();
                if (sqmSamples.Any()) {
                    session.SqmAvg = sqmSamples.Average(w => w.SkyQuality);
                }

                session.MinDewPointMargin = session.WeatherSamples.Min(w => w.AmbientTemperature - w.DewPoint);

                if (session.MinDewPointMargin <= 2.0) {
                    session.MasterAnomalies.Add(new AnomalyRecord {
                        Severity = AnomalySeverity.Warning,
                        Category = "Environmental",
                        Description = $"Ambient temperature approached within {session.MinDewPointMargin:F1}°C of the dew point during the session. Risk of optical dew formation."
                    });
                }
            }
        }

        private static void CalculateTargetStatistics(TargetSessionData target, double pixelScaleArcsec, double setpoint) {
            var lights = target.Frames.Where(f => !f.IsCalibrationFrame).ToList();
            target.TotalLightFrames = lights.Count;
            target.TotalIntegrationSeconds = lights.Sum(f => f.ExposureSeconds);
            target.RejectedFrames = lights.Count(f => f.Rejected);

            if (!lights.Any()) {
                target.QualityScore = 100.0;
                return;
            }

            // HFR Stats
            var hfrs = lights.Select(f => f.HFR).Where(h => h > 0).ToList();
            if (hfrs.Any()) {
                target.HfrMin = hfrs.Min();
                target.HfrMax = hfrs.Max();
                target.HfrAvg = hfrs.Average();
                target.HfrMedian = GetMedian(hfrs);
                target.HfrStdDev = GetStdDev(hfrs, target.HfrAvg);
            }

            // Star Count Stats (Full Min, Max, Mean, Median, StdDev)
            var stars = lights.Select(f => (double)f.StarCount).Where(s => s > 0).ToList();
            if (stars.Any()) {
                target.StarCountMin = (int)stars.Min();
                target.StarCountMax = (int)stars.Max();
                target.StarCountAvg = stars.Average();
                target.StarCountMedian = GetMedian(stars);
                target.StarCountStdDev = GetStdDev(stars, target.StarCountAvg);
            }

            // Guiding RMS Stats (Converted from guide pixels to arcseconds, excluding RMS = 0)
            var unguidedFrames = lights.Where(f => f.GuideTotalRms <= 0).ToList();
            target.UnguidedFrameCount = unguidedFrames.Count;

            var guidedFrames = lights.Where(f => f.GuideTotalRms > 0).ToList();
            if (guidedFrames.Any()) {
                var rmsArcsecList = guidedFrames.Select(f => f.GuideTotalRms).OrderBy(v => v).ToList();
                target.GuideRmsMin = rmsArcsecList.Min();
                target.GuideRmsMax = rmsArcsecList.Max();
                target.GuideTotalRmsAvg = rmsArcsecList.Average();
                target.GuideRmsMedian = GetMedian(rmsArcsecList);
                target.GuideRmsStdDev = GetStdDev(rmsArcsecList, target.GuideTotalRmsAvg);
                target.GuideMaxRmsSpike = target.GuideRmsMax;

                var raList = guidedFrames.Select(f => f.GuideRaRms).Where(r => r > 0).ToList();
                if (raList.Any()) target.GuideRaRmsAvg = raList.Average();

                var decList = guidedFrames.Select(f => f.GuideDecRms).Where(r => r > 0).ToList();
                if (decList.Any()) target.GuideDecRmsAvg = decList.Average();
            }

            // Sensor Temp Stats
            var validSensorTemps = lights.Where(f => f.CameraTemperature.HasValue).Select(f => f.CameraTemperature.Value).OrderBy(t => t).ToList();
            if (validSensorTemps.Any()) {
                target.SensorTempMin = validSensorTemps.First();
                target.SensorTempMax = validSensorTemps.Last();
                target.SensorTempAvg = validSensorTemps.Average();
                target.SensorTempMedian = validSensorTemps[validSensorTemps.Count / 2];

                double referenceTemp = target.SensorTempMedian.Value;

                foreach (var frame in lights.Where(f => f.CameraTemperature.HasValue)) {
                    if (Math.Abs(frame.CameraTemperature.Value - referenceTemp) > 2.0) {
                        target.AbnormalSensorTempFrames++;
                        // We will just flag it in Anomalies during the evaluation loop below, or right here.
                        // Actually, let's flag it here, and the evaluation loop can pick it up or we just add it to Anomalies.
                    }
                }
            }

            // Sub-Frame Rejection & Health Engine Evaluation
            target.GoodFrameCount = 0;
            target.BadFrameCount = 0;
            target.BadHfrCount = 0;
            target.BadStarCount = 0;
            target.BadRmsCount = 0;
            target.ExplicitRejectedCount = 0;
            target.Anomalies.Clear();

            foreach (var f in lights) {
                bool isBad = false;
                var reasons = new List<string>();

                if (f.Rejected) {
                    isBad = true;
                    reasons.Add("Explicitly Rejected by N.I.N.A");
                    target.ExplicitRejectedCount++;
                }

                if (f.HFR > 0 && target.HfrMedian > 0) {
                    if (f.HFR > target.HfrMedian + (2.0 * target.HfrStdDev) || f.HFR > target.HfrMedian * 1.30) {
                        isBad = true;
                        reasons.Add($"HFR Spike ({f.HFR:F2} px vs Median {target.HfrMedian:F2} px)");
                        target.BadHfrCount++;
                    }
                }

                if (f.StarCount > 0 && target.StarCountMedian > 0) {
                    if (f.StarCount < target.StarCountMedian * 0.50) {
                        isBad = true;
                        reasons.Add($"Star Count Drop ({f.StarCount} stars vs Median {target.StarCountMedian:F0} stars)");
                        target.BadStarCount++;
                    }
                }

                if (f.GuideTotalRms > 0 && target.GuideRmsMedian > 0) {
                    double rmsArcsec = f.GuideTotalRms;
                    double maxAllowedRms = Math.Max(2.5, pixelScaleArcsec * 1.5);
                    if (rmsArcsec > target.GuideRmsMedian + (2.5 * target.GuideRmsStdDev) || (rmsArcsec > target.GuideRmsMedian * 2.0 && rmsArcsec > maxAllowedRms)) {
                        isBad = true;
                        reasons.Add($"Guiding RMS Spike ({rmsArcsec:F2}\" vs Median {target.GuideRmsMedian:F2}\")");
                        target.BadRmsCount++;
                    }
                }

                if (f.CameraTemperature.HasValue && target.SensorTempMedian.HasValue) {
                    double referenceTemp = target.SensorTempMedian.Value;
                    if (Math.Abs(f.CameraTemperature.Value - referenceTemp) > 2.0) {
                        target.Anomalies.Add(new AnomalyRecord {
                            Timestamp = f.Timestamp,
                            TargetName = target.TargetName,
                            Category = "Sensor Temp Deviation",
                            Description = $"Sensor Temp Deviation ({f.CameraTemperature.Value:F1}°C vs Median {referenceTemp:F1}°C)",
                            Severity = AnomalySeverity.Warning,
                            Value = f.CameraTemperature.Value,
                            ExpectedValue = referenceTemp
                        });
                        // target.AbnormalSensorTempFrames is already counted above.
                    }
                }

                if (isBad) {
                    f.IsBadFrame = true;
                    f.BadFrameReason = string.Join("; ", reasons);
                    target.BadFrameCount++;

                    // Log Anomaly Record
                    target.Anomalies.Add(new AnomalyRecord {
                        Timestamp = f.Timestamp,
                        TargetName = target.TargetName,
                        Category = "Sub-Frame Health Rejection",
                        Description = $"Frame flagged as sub-optimal: {f.BadFrameReason}",
                        Severity = AnomalySeverity.Warning,
                        Value = f.HFR,
                        ExpectedValue = target.HfrMedian
                    });
                } else {
                    f.IsBadFrame = false;
                    f.BadFrameReason = string.Empty;
                    target.GoodFrameCount++;
                }
            }

            // Calculate Pre-Flip and Post-Flip performance metrics (30 min window before & after each flip)
            foreach (var flip in target.MeridianFlips) {
                DateTime flipTime = flip.Timestamp;

                var preFrames = lights.Where(f => f.Timestamp >= flipTime.AddMinutes(-30) && f.Timestamp < flipTime).ToList();
                var postFrames = lights.Where(f => f.Timestamp > flipTime && f.Timestamp <= flipTime.AddMinutes(30)).ToList();

                flip.PreFlipFrameCount = preFrames.Count;
                flip.PostFlipFrameCount = postFrames.Count;

                if (preFrames.Any()) {
                    var preHfrs = preFrames.Where(f => f.HFR > 0).Select(f => f.HFR).ToList();
                    if (preHfrs.Any()) flip.PreFlipHfr = preHfrs.Average();

                    var preStars = preFrames.Where(f => f.StarCount > 0).Select(f => (double)f.StarCount).ToList();
                    if (preStars.Any()) flip.PreFlipStarCount = preStars.Average();

                    var preRms = preFrames.Where(f => f.GuideTotalRms > 0).Select(f => f.GuideTotalRms).ToList();
                    if (preRms.Any()) flip.PreFlipRms = preRms.Average();
                }

                if (postFrames.Any()) {
                    var postHfrs = postFrames.Where(f => f.HFR > 0).Select(f => f.HFR).ToList();
                    if (postHfrs.Any()) flip.PostFlipHfr = postHfrs.Average();

                    var postStars = postFrames.Where(f => f.StarCount > 0).Select(f => (double)f.StarCount).ToList();
                    if (postStars.Any()) flip.PostFlipStarCount = postStars.Average();

                    var postRms = postFrames.Where(f => f.GuideTotalRms > 0).Select(f => f.GuideTotalRms).ToList();
                    if (postRms.Any()) flip.PostFlipRms = postRms.Average();
                }
            }

            // Compute Target Quality Score (0 - 100)
            double guideScore = target.GuideTotalRmsAvg > 0 ? Math.Max(0, 100 - (target.GuideTotalRmsAvg * 35.0)) : 90.0;
            double hfrScore = target.HfrStdDev > 0 ? Math.Max(0, 100 - (target.HfrStdDev * 80.0)) : 90.0;
            double rejectPenalty = target.TotalLightFrames > 0 ? ((double)target.BadFrameCount / target.TotalLightFrames) * 100.0 : 0;

            target.QualityScore = Math.Min(100.0, Math.Max(0.0, (guideScore * 0.45) + (hfrScore * 0.45) - rejectPenalty + 10.0));
        }

        // Downsampling / Data Decimation algorithm for smooth vector SVG charts
        public static List<T> DecimateSamples<T>(List<T> source, int maxPoints, Func<T, DateTime> timeSelector) {
            if (source == null || source.Count <= maxPoints || maxPoints <= 2) {
                return source ?? new List<T>();
            }

            int bucketSize = (int)Math.Ceiling((double)source.Count / maxPoints);
            var result = new List<T>();

            for (int i = 0; i < source.Count; i += bucketSize) {
                var chunk = source.Skip(i).Take(bucketSize).ToList();
                result.Add(chunk[chunk.Count / 2]); // Pick median sample of bucket
            }

            return result;
        }

        private static double GetMedian(List<double> numbers) {
            if (!numbers.Any()) return 0.0;
            var sorted = numbers.OrderBy(n => n).ToList();
            int count = sorted.Count;
            if (count % 2 == 0) {
                return (sorted[(count / 2) - 1] + sorted[count / 2]) / 2.0;
            }
            return sorted[count / 2];
        }

        private static double GetStdDev(List<double> numbers, double mean) {
            if (numbers.Count <= 1) return 0.0;
            double sumSquares = numbers.Sum(n => Math.Pow(n - mean, 2));
            return Math.Sqrt(sumSquares / (numbers.Count - 1));
        }
    }
}
