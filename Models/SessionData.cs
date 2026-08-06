using System;
using System.Collections.Generic;
using System.Linq;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Models {
    public class WeatherSample {
        public DateTime Timestamp { get; set; }
        public double AmbientTemperature { get; set; }
        public double Humidity { get; set; }
        public double DewPoint { get; set; }
        public double SkyQuality { get; set; } // SQM mag/arcsec^2
        public double CloudCover { get; set; }
    }

    public class CalibrationSummary {
        public string FrameType { get; set; } = string.Empty; // Dark, Flat, DarkFlat, Bias
        public int Count { get; set; }
        public double ExposureSeconds { get; set; }
        public double TotalSeconds => Count * ExposureSeconds;
    }

    public class PlateSolveRecord {
        public DateTime Timestamp { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public double SolveTimeSeconds { get; set; }
        public double PointingErrorArcmin { get; set; }
        public bool Successful { get; set; } = true;
    }

    public class PolarAlignmentRecord {
        public DateTime Timestamp { get; set; }
        public string SourcePlugin { get; set; } = "2-Point Polar Alignment";
        public double TotalErrorArcmin { get; set; }
        public double AltitudeErrorArcmin { get; set; }
        public double AzimuthErrorArcmin { get; set; }
        public string TotalErrorFormatted { get; set; } = string.Empty;
        public string AltitudeErrorFormatted { get; set; } = string.Empty;
        public string AzimuthErrorFormatted { get; set; } = string.Empty;
    }

    public class EquipmentProfileRecord {
        public string ProfileName { get; set; } = "Equipment Profile";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public EquipmentDetails Equipment { get; set; } = new EquipmentDetails();
    }

    public class SessionData {
        public DateTime SessionStart { get; set; }
        public DateTime? SequenceStart { get; set; }
        public DateTime SessionEnd { get; set; }
        public TimeSpan TotalSessionDuration => SessionEnd > SessionStart ? SessionEnd - SessionStart : TimeSpan.Zero;

        public DateTime? FirstLightTimestamp { get; set; }
        public DateTime? LastLightTimestamp { get; set; }

        public bool IsLiveSession { get; set; } = true;

        public EquipmentDetails Equipment { get; set; } = new EquipmentDetails();
        public List<EquipmentProfileRecord> EquipmentProfiles { get; set; } = new List<EquipmentProfileRecord>();

        public List<TargetSessionData> Targets { get; set; } = new List<TargetSessionData>();
        public List<CalibrationSummary> CalibrationFrames { get; set; } = new List<CalibrationSummary>();
        
        public double MaxDewPointMargin { get; set; } = double.MaxValue;
        
        public List<WeatherSample> WeatherSamples { get; set; } = new List<WeatherSample>();
        public List<PlateSolveRecord> PlateSolves { get; set; } = new List<PlateSolveRecord>();
        public List<PolarAlignmentRecord> PolarAlignments { get; set; } = new List<PolarAlignmentRecord>();
        public List<AnomalyRecord> MasterAnomalies { get; set; } = new List<AnomalyRecord>();
        public List<DitherRecord> DitherEvents { get; set; } = new List<DitherRecord>();
        public List<HardwareErrorRecord> HardwareErrors { get; set; } = new List<HardwareErrorRecord>();
        public long TotalStorageBytes { get; set; }

        public bool EmergencySafetyAbort { get; set; }
        public string SafetyAbortReason { get; set; } = string.Empty;
        public DateTime? SafetyAbortTimestamp { get; set; }

        public double TotalNightIntegrationSeconds { get; set; }
        public double TotalOverheadSeconds { get; set; }
        public double ImagingEfficiencyPercent => TotalSessionDuration.TotalSeconds > 0 ? (TotalNightIntegrationSeconds / TotalSessionDuration.TotalSeconds) * 100.0 : 0;

        public DateTime? AstroDusk { get; set; }
        public DateTime? AstroDawn { get; set; }
        public TimeSpan AstroDarknessDuration { get; set; }
        public double? AstroDarknessEfficiency { get; set; }
        public double ImagingDutyCycle { get; set; }
        public bool UsedNauticalFallback { get; set; }

        public double MasterQualityScore { get; set; }
        public double ThermalFocusSlopeStepsPerDegree { get; set; } // Focuser steps / °C

        // Environmental & Dew Heater Telemetry Summaries
        public double AmbientTempMin { get; set; }
        public double AmbientTempMax { get; set; }
        public double AmbientTempAvg { get; set; }
        public double HumidityMin { get; set; }
        public double HumidityMax { get; set; }
        public double HumidityAvg { get; set; }
        public double DewPointMin { get; set; }
        public double DewPointMax { get; set; }
        public double DewPointAvg { get; set; }
        public double SqmAvg { get; set; }
        public double MinDewPointMargin { get; set; } = 99.0;
        public string DewHeaterStatus { get; set; } = "Not Monitored";

        public List<string> FiltersUsed => Targets
            .SelectMany(t => t.Frames.Where(f => !f.IsCalibrationFrame).Select(f => f.Filter))
            .Where(f => !string.IsNullOrWhiteSpace(f) && f != "No Filter")
            .Distinct()
            .ToList();

        public string FiltersUsedFormatted => FiltersUsed.Any() ? string.Join(", ", FiltersUsed) : "No Filter / None";
    }
}
