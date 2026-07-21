using System;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Models {
    public enum AnomalySeverity {
        Info,
        Warning,
        Critical
    }

    public class AnomalyRecord {
        public DateTime Timestamp { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public string Category { get; set; } = "General"; // HFR Spike, Star Drop, Guiding Spike, Temp Drift, Safety Abort
        public string Description { get; set; } = string.Empty;
        public AnomalySeverity Severity { get; set; } = AnomalySeverity.Warning;
        public double Value { get; set; }
        public double ExpectedValue { get; set; }
        public double ZScore { get; set; }
    }
}
