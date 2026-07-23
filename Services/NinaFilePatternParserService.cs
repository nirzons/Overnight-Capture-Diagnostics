using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Services {
    public class ParsedTelemetry {
        public string TargetName { get; set; } = string.Empty;
        public string Filter { get; set; } = string.Empty;
        public double ExposureSeconds { get; set; }
        public double HFR { get; set; }
        public int StarCount { get; set; }
        public double RMS { get; set; }
        public double SensorTemp { get; set; }
        public double Gain { get; set; }
        public bool IsSuccess { get; set; }
    }

    public static class NinaFilePatternParserService {

        public static Regex CompilePatternRegex(string pattern) {
            if (string.IsNullOrWhiteSpace(pattern)) return new Regex("$^");

            // Normalize path separators
            string p = pattern.Replace('/', '\\').Trim('\\');

            // Escape special regex characters
            string escaped = Regex.Escape(p);

            // Replace escaped $$TAG$$ with named regex capture groups
            string patternRegex = escaped
                .Replace(@"\$\$TARGETNAME\$\$", @"(?<TargetName>[^\\/_]+?)")
                .Replace(@"\$\$IMAGETYPE\$\$", @"(?<ImageType>LIGHT|DARK|FLAT|BIAS)")
                .Replace(@"\$\$DATETIME\$\$", @"(?<DateTime>\d{4}[-_]\d{2}[-_]\d{2}[-_]\d{2}[-_]\d{2}[-_]\d{2})")
                .Replace(@"\$\$DATE\$\$", @"(?<Date>\d{4}[-_]\d{2}[-_]\d{2})")
                .Replace(@"\$\$DATEMINUS12\$\$", @"(?<DateMinus12>\d{4}[-_]\d{2}[-_]\d{2})")
                .Replace(@"\$\$TIME\$\$", @"(?<Time>\d{2}[-_]\d{2}[-_]\d{2})")
                .Replace(@"\$\$FILTER\$\$", @"(?<Filter>[^\\/_]+?)")
                .Replace(@"\$\$EXPOSURETIME\$\$", @"(?<Exposure>[\d\.,]+)")
                .Replace(@"\$\$GAIN\$\$", @"(?<Gain>[\d\.,]+)")
                .Replace(@"\$\$OFFSET\$\$", @"(?<Offset>[\d\.,]+)")
                .Replace(@"\$\$SENSORTEMP\$\$", @"(?<SensorTemp>[-\d\.,]+)")
                .Replace(@"\$\$HFR\$\$", @"(?<HFR>[\d\.,]+)")
                .Replace(@"\$\$STARCOUNT\$\$", @"(?<StarCount>\d+)")
                .Replace(@"\$\$RMS\$\$", @"(?<RMS>[\d\.,]+)")
                .Replace(@"\$\$FRAMENR\$\$", @"(?<FrameNr>\d+)")
                .Replace(@"\$\$NUMBER\$\$", @"(?<FrameNr>\d+)")
                .Replace(@"\$\$FWHM\$\$", @"(?<FWHM>[\d\.,]+)");

            return new Regex(patternRegex + @"(?:\.fits|\.fit|\.xisf|\.tif)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public static ParsedTelemetry ParsePathWithPattern(string fullPath, string ninaPattern) {
            var result = new ParsedTelemetry();
            if (string.IsNullOrWhiteSpace(fullPath) || string.IsNullOrWhiteSpace(ninaPattern)) return result;

            try {
                var regex = CompilePatternRegex(ninaPattern);
                var match = regex.Match(fullPath);

                if (match.Success) {
                    result.IsSuccess = true;
                    if (match.Groups["TargetName"].Success) result.TargetName = match.Groups["TargetName"].Value.Trim();
                    if (match.Groups["Filter"].Success) result.Filter = match.Groups["Filter"].Value.Trim();

                    if (match.Groups["Exposure"].Success && double.TryParse(match.Groups["Exposure"].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var expVal)) {
                        result.ExposureSeconds = expVal;
                    }
                    if (match.Groups["HFR"].Success && double.TryParse(match.Groups["HFR"].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var hfrVal)) {
                        result.HFR = hfrVal;
                    }
                    if (match.Groups["StarCount"].Success && int.TryParse(match.Groups["StarCount"].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var starVal)) {
                        result.StarCount = starVal;
                    }
                    if (match.Groups["RMS"].Success && double.TryParse(match.Groups["RMS"].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var rmsVal)) {
                        result.RMS = rmsVal;
                    }
                    if (match.Groups["SensorTemp"].Success && double.TryParse(match.Groups["SensorTemp"].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var tempVal)) {
                        result.SensorTemp = tempVal;
                    }
                    if (match.Groups["Gain"].Success && double.TryParse(match.Groups["Gain"].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var gainVal)) {
                        result.Gain = gainVal;
                    }
                }
            } catch { }

            return result;
        }

        /// <summary>
        /// Attempts to discover active pattern from N.I.N.A profile files on disk if not provided via live plugin profile service.
        /// </summary>
        public static string DiscoverPatternFromDisk() {
            try {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string profilesDir = Path.Combine(localAppData, "NINA", "Profiles");
                if (Directory.Exists(profilesDir)) {
                    var profileFiles = new DirectoryInfo(profilesDir).GetFiles("*.profile");
                    DateTime latestTime = DateTime.MinValue;
                    string activePattern = string.Empty;

                    foreach (var file in profileFiles) {
                        if (file.LastWriteTime > latestTime) {
                            string text = File.ReadAllText(file.FullName);
                            var m = Regex.Match(text, @"<FilePattern>(?<Pattern>[^<]+)</FilePattern>", RegexOptions.IgnoreCase);
                            if (m.Success) {
                                activePattern = m.Groups["Pattern"].Value;
                                latestTime = file.LastWriteTime;
                            }
                        }
                    }
                    return activePattern;
                }
            } catch { }
            return string.Empty;
        }
    }
}
