using System;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Models {
    public class AutofocusRecord {
        public DateTime Timestamp { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public string TriggerReason { get; set; } = "Unknown";
        public int BestPosition { get; set; }
        public double HfrBefore { get; set; }
        public double HfrAfter { get; set; }
        public double ImprovementPercent => HfrBefore > 0 ? ((HfrAfter - HfrBefore) / HfrBefore) * 100.0 : 0.0;
        public double HfrDelta => HfrAfter - HfrBefore;
        public string HfrDeltaString {
            get {
                double delta = HfrAfter - HfrBefore;
                if (Math.Abs(delta) < 0.005) return "0.00 px";
                return delta > 0 ? $"+{delta:F2} px" : $"{delta:F2} px";
            }
        }
        public double Temperature { get; set; }
        public double RSquared { get; set; }
        public bool Successful { get; set; } = true;
    }
}
