namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Models {
    public class EquipmentDetails {
        public string CameraName { get; set; } = "Not Connected";
        public int CameraWidth { get; set; }
        public int CameraHeight { get; set; }
        public double PixelSizeMicrons { get; set; }
        public double CameraTempSetpoint { get; set; }
        public string TelescopeName { get; set; } = "Not Connected";
        public double FocalLengthMm { get; set; }
        public double ApertureMm { get; set; }
        public double FocalRatio => ApertureMm > 0 ? FocalLengthMm / ApertureMm : 0;
        public double PixelScaleArcsec => (FocalLengthMm > 0 && PixelSizeMicrons > 0) ? (PixelSizeMicrons * 206.265) / FocalLengthMm : 0;
        public double FovWidthArcmin => (PixelScaleArcsec * CameraWidth) / 60.0;
        public double FovHeightArcmin => (PixelScaleArcsec * CameraHeight) / 60.0;

        public string FocuserName { get; set; } = "Not Connected";
        public int FocuserPosition { get; set; }

        public string FilterWheelName { get; set; } = "Not Connected";
        public string MountName { get; set; } = "Not Connected";
        public string GuiderName { get; set; } = "Not Connected";
        public string SwitchName { get; set; } = "Not Connected";
        public string WeatherName { get; set; } = "Not Connected";

        public double SiteLatitude { get; set; }
        public double SiteLongitude { get; set; }
        public double SiteElevation { get; set; }
        public string SiteName { get; set; } = "Observatory Site";
    }
}
