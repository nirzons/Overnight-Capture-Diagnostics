using System;
using System.Collections.Generic;
using System.Linq;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Models {
    public class TargetSessionData {
        public string TargetName { get; set; } = "Unknown Target";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime > StartTime ? EndTime - StartTime : TimeSpan.Zero;

        public List<FrameRecord> Frames { get; set; } = new List<FrameRecord>();
        public List<AutofocusRecord> AutofocusRuns { get; set; } = new List<AutofocusRecord>();
        public List<MeridianFlipRecord> MeridianFlips { get; set; } = new List<MeridianFlipRecord>();
        public List<AnomalyRecord> Anomalies { get; set; } = new List<AnomalyRecord>();

        public string TargetCoordinates { get; set; } = string.Empty; // e.g. RA: 21:48:51, Dec: 47° 21' 58"
        public string RotatorAngle { get; set; } = string.Empty; // e.g. 17.89°

        // HFR Statistics
        public double HfrMin { get; set; }
        public double HfrMax { get; set; }
        public double HfrAvg { get; set; }
        public double HfrMedian { get; set; }
        public double HfrStdDev { get; set; }

        // Star Count Statistics
        public int StarCountMin { get; set; }
        public int StarCountMax { get; set; }
        public double StarCountAvg { get; set; }
        
        // Sensor Temp Statistics
        public double? SensorTempMin { get; set; }
        public double? SensorTempMax { get; set; }
        public double? SensorTempAvg { get; set; }
        public double? SensorTempMedian { get; set; }
        public int AbnormalSensorTempFrames { get; set; }
        public double StarCountMedian { get; set; }
        public double StarCountStdDev { get; set; }

        // Guiding RMS Statistics (in arcsec)
        public double GuideRmsMin { get; set; }
        public double GuideRmsMax { get; set; }
        public double GuideTotalRmsAvg { get; set; }
        public double GuideRmsMedian { get; set; }
        public double GuideRmsStdDev { get; set; }
        public double GuideRaRmsAvg { get; set; }
        public double GuideDecRmsAvg { get; set; }
        public double GuideMaxRmsSpike { get; set; }

        public int UnguidedFrameCount { get; set; }
        public double TotalIntegrationSeconds { get; set; }
        public int TotalLightFrames { get; set; }
        public int RejectedFrames { get; set; }

        // Sub-Frame Rejection & Health Engine Metrics
        public int GoodFrameCount { get; set; }
        public int BadFrameCount { get; set; }
        public double AcceptanceRatePercent => TotalLightFrames > 0 ? ((double)GoodFrameCount / TotalLightFrames) * 100.0 : 100.0;

        public int BadHfrCount { get; set; }
        public int BadStarCount { get; set; }
        public int BadRmsCount { get; set; }
        public int ExplicitRejectedCount { get; set; }

        public string FiltersSummary {
            get {
                var groups = Frames.Where(f => !f.IsCalibrationFrame)
                    .GroupBy(f => f.Filter)
                    .Select(g => $"{g.Key} ({g.Count()} frames)");
                return groups.Any() ? string.Join(", ", groups) : "No Filter";
            }
        }

        public double QualityScore { get; set; } // 0 - 100
    }
}
