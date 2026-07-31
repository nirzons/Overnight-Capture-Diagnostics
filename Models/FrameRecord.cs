using System;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Models {
    public class FrameRecord {
        public DateTime Timestamp { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string Filter { get; set; } = "No Filter";
        public double ExposureSeconds { get; set; }
        public double HFR { get; set; }
        public int StarCount { get; set; }
        public double? CameraTemperature { get; set; }
        public double AmbientTemperature { get; set; }
        public double Altitude { get; set; }
        public double Azimuth { get; set; }
        public double RaDegrees { get; set; }
        public double DecDegrees { get; set; }
        public double GuideTotalRms { get; set; }
        public double GuideRaRms { get; set; }
        public double GuideDecRms { get; set; }
        public bool IsCalibrationFrame { get; set; }
        public string CalibrationType { get; set; } = string.Empty; // Dark, Flat, Bias, DarkFlat
        public bool Rejected { get; set; }

        public bool IsBadFrame { get; set; }
        public string BadFrameReason { get; set; } = string.Empty;
    }
}
