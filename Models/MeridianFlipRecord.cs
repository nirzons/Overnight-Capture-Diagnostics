using System;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Models {
    public class MeridianFlipRecord {
        public DateTime Timestamp { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public double DurationSeconds { get; set; }
        public bool Successful { get; set; } = true;

        public int PreFlipFrameCount { get; set; }
        public int PostFlipFrameCount { get; set; }

        public double PreFlipHfr { get; set; }
        public double PostFlipHfr { get; set; }

        public double PreFlipStarCount { get; set; }
        public double PostFlipStarCount { get; set; }

        public double PreFlipRms { get; set; }
        public double PostFlipRms { get; set; }
    }
}
