using System;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Models {
    public class HardwareErrorRecord {
        public DateTime Timestamp { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string ErrorType { get; set; } = string.Empty; // Disconnect, Exception, Communication Error
        public string Message { get; set; } = string.Empty;
    }
}
