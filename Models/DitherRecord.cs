using System;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Models {
    public class DitherRecord {
        public DateTime StartTime { get; set; }
        public DateTime? SettleTime { get; set; }
        public double DurationSeconds => SettleTime.HasValue ? (SettleTime.Value - StartTime).TotalSeconds : 0;
    }
}
