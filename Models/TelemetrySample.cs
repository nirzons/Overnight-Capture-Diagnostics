using System;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Models {
    public class TelemetrySample {
        public DateTime Timestamp { get; set; }
        public double? Voltage { get; set; }
        public double? CurrentAmps { get; set; }
        public double? PowerWatts { get; set; }
        public double? AmbientTemperature { get; set; }
        public double? Humidity { get; set; }
        public double? DewPoint { get; set; }
        public double? DewHeaterDuty { get; set; }
        public double? CoolerPower { get; set; }
        public double? SkyQuality { get; set; }
        public double? CloudCover { get; set; }
    }
}
