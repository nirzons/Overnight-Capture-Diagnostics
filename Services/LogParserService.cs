#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using NirZonshine.NINA.OvernightCaptureDiagnostics.Models;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Services {
    public class LogParserService {
        private static readonly Regex RegexTargetStart = new Regex(
            @"(?:Target:\s*|Target name:\s*|Target:\s+)(?<TargetName>[^;\r\n,|]+?)(?:\s+RA:|\s+Dec:|\s+Epoch:|,|;|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexImageSavedFile = new Regex(
            @"(?:Image Saved\.\s*File:|Saved image to|Successfully saved file at)\s*(?<Path>[^\r\n]+?\.(?:fits|fit|tif|xisf))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexFilterChange = new Regex(
            @"(?:Filter change to|Filter changed to|Changed filter to|Setting filter to|, Filter:\s*)(?<Filter>[^,\r\n;|]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexDitherStart = new Regex(
            @"Dither started",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexDitherSettle = new Regex(
            @"Dither settled",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexAutofocusCompleted = new Regex(
            @"AutoFocus completed with focuser starting at (?<Start>\d+) and ending at (?<Pos>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexAutofocusFinished = new Regex(
            @"AF finished\.\s*Best position:\s*(?<Pos>\d+),\s*HFR:\s*(?<HFR>[\d\.,]+)(?:,\s*Temperature:\s*(?<Temp>[-\d\.,]+)°C)?(?:,\s*R2:\s*(?<R2>[\d\.,]+))?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexAutofocusNotification = new Regex(
            @"Autofocus notification received - Temperature (?<Temp>[-\d\.,]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexAutofocusStarResult = new Regex(
            @"Average HFR:\s*(?<HFR>[\d\.,]+),\s*HFR MAD:\s*[\d\.,]+,\s*Detected Stars\s*(?<Stars>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexMeridianFlipStart = new Regex(
            @"(?:Starting meridian flip routine|Executing Meridian Flip|Meridian Flip - Start|Initiating Meridian Flip|Meridian Flip - Initializing)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexMeridianFlipFinished = new Regex(
            @"(?:Meridian flip finished successfully in (?<Duration>[\d\.,]+) seconds|Meridian flip finished|Meridian flip completed|Meridian Flip - Completed|Meridian Flip - Exiting)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexPierSideChange = new Regex(
            @"expected pier side (?<Pier>pierWest|pierEast)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexPlateSolveSuccess = new Regex(
            @"Platesolve successful:\s*Coordinates:\s*RA:\s*(?<RA>[^;]+);\s*Dec:\s*(?<Dec>[^;]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex Regex2PpaError = new Regex(
            @"Calculated Alignment Errors:\s*Alt\s*(?<AltStr>.+?),\s*Az\s*(?<AzStr>.+?)\s*\(Alt:\s*(?<AltErr>[-\d\.,]+)'?,\s*Az:\s*(?<AzErr>[-\d\.,]+)'?\),\s*Total:\s*(?<TotalStr>.+?)\s*\((?<TotalErr>[\d\.,]+)'?\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexTppaPattern = new Regex(
            @"Calculated alignment error\s*-\s*Altitude:\s*(?<altSign>[-+])?(?:(?<altDeg>\d+)[^0-9\'-]*)?\s*(?<altMin>\d+)\'\s*(?<altSec>\d+)?\"".*Azimuth:\s*(?<azSign>[-+])?(?:(?<azDeg>\d+)[^0-9\'-]*)?\s*(?<azMin>\d+)\'\s*(?<azSec>\d+)?\""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexTppaStandardPattern = new Regex(
            @"Calculated Error:\s*Az:\s*(?<azSign>[-+])?(?:(?<azDeg>\d+)[^0-9\'-]*)?(?<azMin>\d+)\'\s*(?<azSec>\d+)?\""\s*,\s*Alt:\s*(?<altSign>[-+])?(?:(?<altDeg>\d+)[^0-9\'-]*)?(?<altMin>\d+)\'\s*(?<altSec>\d+)?\""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexTppaAltFirstPattern = new Regex(
            @"Calculated Error:\s*Alt:\s*(?<altSign>[-+])?(?:(?<altDeg>\d+)[^0-9\'-]*)?(?<altMin>\d+)\'\s*(?<altSec>\d+)?\""\s*,\s*Az:\s*(?<azSign>[-+])?(?:(?<azDeg>\d+)[^0-9\'-]*)?(?<azMin>\d+)\'\s*(?<azSec>\d+)?\""",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexSafetyAbort = new Regex(
            @"Safety state changed to UNSAFE:\s*(?<Reason>.+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexWeatherTelemetry = new Regex(
            @"Temp:\s*(?<Temp>[-\d\.,]+)°C,\s*Humidity:\s*(?<Hum>[\d\.,]+)%,\s*DewPoint:\s*(?<Dew>[-\d\.,]+)°C(?:,\s*SQM:\s*(?<SQM>[\d\.,]+))?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Equipment Connection Regexes
        private static readonly Regex RegexConnectCamera = new Regex(
            @"Successfully connected Camera\..*?DisplayName:\s*(?<Name>[^|\r\n]+?)(?:\s*Driver Version|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexConnectMount = new Regex(
            @"Successfully connected mount\..*?DisplayName:\s*(?<Name>[^|\r\n]+?)(?:\s*Driver Version|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexConnectFocuser = new Regex(
            @"Successfully connected Focuser\..*?DisplayName:\s*(?<Name>[^|\r\n]+?)(?:\s*Driver Version|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexConnectFilterWheel = new Regex(
            @"Successfully connected Filter Wheel\..*?DisplayName:\s*(?<Name>[^|\r\n]+?)(?:\s*Driver Version|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexConnectSwitch = new Regex(
            @"Successfully connected Switch\..*?DisplayName:\s*(?<Name>[^|\r\n]+?)(?:\s*Driver Version|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexConnectPhd2 = new Regex(
            @"Connecting to PHD2 server",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex RegexPlateSolveParams = new Regex(
            @"Platesolving with parameters:\s*FocalLength:\s*(?<FocalLength>[\d\.,]+)\s*PixelSize:\s*(?<PixelSize>[\d\.,]+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public SessionData ParseLogFiles(string? sessionDateFilter, CancellationToken token) {
            var sessionData = new SessionData();

            var logDirectories = new List<string>();

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string ninaLogsDir = Path.Combine(localAppData, "NINA", "Logs");
            if (Directory.Exists(ninaLogsDir)) {
                logDirectories.Add(ninaLogsDir);
            }

            var allLogFiles = new List<FileInfo>();
            foreach (var dir in logDirectories) {
                try {
                    allLogFiles.AddRange(new DirectoryInfo(dir).GetFiles("*.log"));
                } catch { }
            }

            allLogFiles = allLogFiles
                .GroupBy(f => f.FullName)
                .Select(g => g.First())
                .OrderBy(f => GetLogFileStartTimestamp(f))
                .ToList();

            if (!allLogFiles.Any()) {
                return sessionData;
            }

            // Calculate Target Astronomical Observing Window (12:00 PM noon to 12:00 PM noon next day)
            DateTime targetAstroDate = GetTargetAstroDate(sessionDateFilter);
            DateTime astroWindowStart = targetAstroDate.AddHours(12);
            DateTime astroWindowEnd = targetAstroDate.AddDays(1).AddHours(12);

            var selectedLogFiles = SelectLogsForSession(allLogFiles, astroWindowStart, astroWindowEnd);
            if (!selectedLogFiles.Any()) {
                selectedLogFiles = allLogFiles;
            }

            string currentTarget = "Default Session Target";
            string currentFilter = "No Filter";
            DateTime? currentDitherStart = null;
            DateTime? currentDitherSettle = null;
            DateTime? flipStartTime = null;
            string? lastPierSide = null;
            double lastAfTemp = 0;
            double lastAfHfr = 0;

            var allFrames = new List<FrameRecord>();
            var allAutofocus = new List<AutofocusRecord>();
            var allFlips = new List<MeridianFlipRecord>();
            var allSolves = new List<PlateSolveRecord>();
            var allPolar = new List<PolarAlignmentRecord>();
            var rawProfiles = new List<EquipmentProfileRecord>();
            var seenFrameKeys = new HashSet<string>();

            foreach (var logFile in selectedLogFiles) {
                token.ThrowIfCancellationRequested();

                var currentLogEq = new EquipmentDetails();
                DateTime fileStart = default;
                DateTime fileEnd = default;

                try {
                    using var fs = new FileStream(logFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);
                    string? line;
                    while ((line = reader.ReadLine()) != null) {
                        token.ThrowIfCancellationRequested();

                        DateTime? dt = ParseTimestampFromLine(line);
                        if (!dt.HasValue) continue;
                        DateTime logTimestamp = dt.Value;

                        // Only ingest events inside the strict 24h astronomical observing window
                        if (logTimestamp < astroWindowStart || logTimestamp >= astroWindowEnd) {
                            continue;
                        }

                        if (fileStart == default || logTimestamp < fileStart) fileStart = logTimestamp;
                        if (fileEnd == default || logTimestamp > fileEnd) fileEnd = logTimestamp;

                        if (sessionData.SessionStart == default || logTimestamp < sessionData.SessionStart) {
                            sessionData.SessionStart = logTimestamp;
                        }
                        if (sessionData.SessionEnd == default || logTimestamp > sessionData.SessionEnd) {
                            sessionData.SessionEnd = logTimestamp;
                        }

                        // Equipment Detection Regexes
                        var mCam = RegexConnectCamera.Match(line);
                        if (mCam.Success) {
                            currentLogEq.CameraName = mCam.Groups["Name"].Value.Trim();
                            if (string.IsNullOrWhiteSpace(sessionData.Equipment.CameraName) || sessionData.Equipment.CameraName == "Not Connected")
                                sessionData.Equipment.CameraName = currentLogEq.CameraName;
                            continue;
                        }
                        var mMnt = RegexConnectMount.Match(line);
                        if (mMnt.Success) {
                            currentLogEq.MountName = mMnt.Groups["Name"].Value.Trim();
                            if (string.IsNullOrWhiteSpace(sessionData.Equipment.MountName) || sessionData.Equipment.MountName == "Not Connected")
                                sessionData.Equipment.MountName = currentLogEq.MountName;
                            continue;
                        }
                        var mFoc = RegexConnectFocuser.Match(line);
                        if (mFoc.Success) {
                            currentLogEq.FocuserName = mFoc.Groups["Name"].Value.Trim();
                            if (string.IsNullOrWhiteSpace(sessionData.Equipment.FocuserName) || sessionData.Equipment.FocuserName == "Not Connected")
                                sessionData.Equipment.FocuserName = currentLogEq.FocuserName;
                            continue;
                        }
                        var mFw = RegexConnectFilterWheel.Match(line);
                        if (mFw.Success) {
                            currentLogEq.FilterWheelName = mFw.Groups["Name"].Value.Trim();
                            if (string.IsNullOrWhiteSpace(sessionData.Equipment.FilterWheelName) || sessionData.Equipment.FilterWheelName == "Not Connected")
                                sessionData.Equipment.FilterWheelName = currentLogEq.FilterWheelName;
                            continue;
                        }
                        var mPhd = RegexConnectPhd2.Match(line);
                        if (mPhd.Success) {
                            currentLogEq.GuiderName = "PHD2 Guider";
                            sessionData.Equipment.GuiderName = currentLogEq.GuiderName;
                            continue;
                        }

                        var mOpt = RegexPlateSolveParams.Match(line);
                        if (mOpt.Success) {
                            double focal = ParseDouble(mOpt.Groups["FocalLength"].Value);
                            double pxSize = ParseDouble(mOpt.Groups["PixelSize"].Value);
                            if (focal > 0) currentLogEq.FocalLengthMm = focal;
                            if (pxSize > 0) currentLogEq.PixelSizeMicrons = pxSize;

                            if (sessionData.Equipment.FocalLengthMm == 0) sessionData.Equipment.FocalLengthMm = focal;
                            if (sessionData.Equipment.PixelSizeMicrons == 0) sessionData.Equipment.PixelSizeMicrons = pxSize;
                            continue;
                        }

                        // Filter Change Event
                        var mFiltChange = RegexFilterChange.Match(line);
                        if (mFiltChange.Success) {
                            string parsedFilt = mFiltChange.Groups["Filter"].Value.Trim();
                            if (!string.IsNullOrWhiteSpace(parsedFilt)) {
                                currentFilter = parsedFilt;
                            }
                            continue;
                        }

                        // 1. Target Context
                        var mTarget = RegexTargetStart.Match(line);
                        if (mTarget.Success) {
                            string tName = mTarget.Groups["TargetName"].Value.Trim();
                            if (!string.IsNullOrWhiteSpace(tName) && !tName.StartsWith("*")) {
                                currentTarget = tName;
                            }
                            continue;
                        }

                        // 2. Dither Window Tracking
                        var mDitherStart = RegexDitherStart.Match(line);
                        if (mDitherStart.Success) {
                            currentDitherStart = logTimestamp;
                            currentDitherSettle = null;
                            continue;
                        }

                        var mDitherSettle = RegexDitherSettle.Match(line);
                        if (mDitherSettle.Success) {
                            currentDitherSettle = logTimestamp;
                            continue;
                        }

                        // 3. Image Saved Event & Filename Telemetry Parsing
                        var mSave = RegexImageSavedFile.Match(line);
                        if (mSave.Success) {
                            string fullPath = mSave.Groups["Path"].Value.Trim();
                            string filename = Path.GetFileName(fullPath);

                            if (fullPath.Contains(@"\Temp\", StringComparison.OrdinalIgnoreCase) ||
                                fullPath.Contains(@"\AppData\Local\Temp", StringComparison.OrdinalIgnoreCase) ||
                                fullPath.Contains(@"\PlateSolver", StringComparison.OrdinalIgnoreCase) ||
                                filename.Contains("PlateSolver", StringComparison.OrdinalIgnoreCase)) {
                                continue;
                            }

                            if (!filename.EndsWith(".fits", StringComparison.OrdinalIgnoreCase) &&
                                !filename.EndsWith(".fit", StringComparison.OrdinalIgnoreCase) &&
                                !filename.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) &&
                                !filename.EndsWith(".xisf", StringComparison.OrdinalIgnoreCase)) {
                                continue;
                            }

                            if (filename.Length < 10 || seenFrameKeys.Contains(filename)) {
                                continue;
                            }

                            seenFrameKeys.Add(filename);

                            string pathTarget = ExtractTargetFromPath(fullPath);
                            string frameTarget = !string.IsNullOrWhiteSpace(pathTarget) ? pathTarget : currentTarget;

                            ParseNinaFilenameTelemetry(filename, out double expSecs, out double hfr, out int stars, out string parsedFilter, out double parsedRms);

                            string frameFilter = !string.IsNullOrWhiteSpace(parsedFilter) ? parsedFilter : currentFilter;

                            bool isCalib = IsCalibrationFile(filename, out string calType);

                            var frame = new FrameRecord {
                                Timestamp = logTimestamp,
                                FileName = filename,
                                TargetName = frameTarget,
                                Filter = filterNameSanitized(frameFilter),
                                ExposureSeconds = expSecs,
                                HFR = hfr,
                                StarCount = stars,
                                GuideTotalRms = parsedRms,
                                IsCalibrationFrame = isCalib,
                                CalibrationType = calType
                            };

                            allFrames.Add(frame);
                            Console.WriteLine($"[DEBUG] Added frame to allFrames: {filename}, Target: '{frameTarget}'");
                            continue;
                        }

                        // 4. Autofocus Events
                        var mAfTemp = RegexAutofocusNotification.Match(line);
                        if (mAfTemp.Success) {
                            lastAfTemp = ParseDouble(mAfTemp.Groups["Temp"].Value);
                            continue;
                        }

                        var mAfStars = RegexAutofocusStarResult.Match(line);
                        if (mAfStars.Success) {
                            lastAfHfr = ParseDouble(mAfStars.Groups["HFR"].Value);
                            continue;
                        }

                        var mAfDone = RegexAutofocusFinished.Match(line);
                        if (mAfDone.Success) {
                            int bestPos = ParseInt(mAfDone.Groups["Pos"].Value);
                            double hfrVal = ParseDouble(mAfDone.Groups["HFR"].Value);
                            if (hfrVal <= 0) hfrVal = lastAfHfr;
                            double tempVal = ParseDouble(mAfDone.Groups["Temp"].Value);
                            if (tempVal == 0) tempVal = lastAfTemp;

                            allAutofocus.Add(new AutofocusRecord {
                                Timestamp = logTimestamp,
                                BestPosition = bestPos,
                                HfrAfter = hfrVal,
                                Temperature = tempVal,
                                Successful = true
                            });
                            continue;
                        }

                        // 5. Meridian Flip Events
                        var mFlipStart = RegexMeridianFlipStart.Match(line);
                        if (mFlipStart.Success) {
                            flipStartTime = logTimestamp;
                            continue;
                        }

                        var mPier = RegexPierSideChange.Match(line);
                        if (mPier.Success) {
                            lastPierSide = mPier.Groups["Pier"].Value;
                            continue;
                        }

                        var mFlipDone = RegexMeridianFlipFinished.Match(line);
                        if (mFlipDone.Success) {
                            double durSecs = 0;
                            if (mFlipDone.Groups["Duration"].Success) {
                                durSecs = ParseDouble(mFlipDone.Groups["Duration"].Value);
                            } else if (flipStartTime.HasValue) {
                                durSecs = (logTimestamp - flipStartTime.Value).TotalSeconds;
                            }

                            allFlips.Add(new MeridianFlipRecord {
                                Timestamp = flipStartTime ?? logTimestamp,
                                DurationSeconds = durSecs,
                                Successful = true
                            });
                            flipStartTime = null;
                            continue;
                        }

                        // 6. Plate Solve Events
                        var mSolve = RegexPlateSolveSuccess.Match(line);
                        if (mSolve.Success) {
                            allSolves.Add(new PlateSolveRecord {
                                Timestamp = logTimestamp,
                                Successful = true
                            });
                            continue;
                        }

                        // 7. Polar Alignment Events (2PPA and TPPA)
                        var m2Ppa = Regex2PpaError.Match(line);
                        if (m2Ppa.Success) {
                            double altErr = ParseDouble(m2Ppa.Groups["AltErr"].Value);
                            double azErr = ParseDouble(m2Ppa.Groups["AzErr"].Value);
                            double totErr = ParseDouble(m2Ppa.Groups["TotalErr"].Value);

                            string altStr = SanitizeAngleDegreeString(m2Ppa.Groups["AltStr"].Value);
                            string azStr = SanitizeAngleDegreeString(m2Ppa.Groups["AzStr"].Value);
                            string totStr = SanitizeAngleDegreeString(m2Ppa.Groups["TotalStr"].Value);

                            allPolar.Add(new PolarAlignmentRecord {
                                Timestamp = logTimestamp,
                                AltitudeErrorArcmin = altErr,
                                AzimuthErrorArcmin = azErr,
                                TotalErrorArcmin = totErr,
                                AltitudeErrorFormatted = altStr,
                                AzimuthErrorFormatted = azStr,
                                TotalErrorFormatted = totStr,
                                SourcePlugin = "2-Point Polar Alignment"
                            });
                            continue;
                        }

                        var mTppa = RegexTppaPattern.Match(line);
                        if (mTppa.Success) {
                            int altDeg = ParseInt(mTppa.Groups["altDeg"].Value);
                            int altMin = ParseInt(mTppa.Groups["altMin"].Value);
                            int altSec = ParseInt(mTppa.Groups["altSec"].Value);
                            double altTotalArcmin = (altDeg * 60.0) + altMin + (altSec / 60.0);
                            if (mTppa.Groups["altSign"].Value == "-") altTotalArcmin = -altTotalArcmin;

                            int azDeg = ParseInt(mTppa.Groups["azDeg"].Value);
                            int azMin = ParseInt(mTppa.Groups["azMin"].Value);
                            int azSec = ParseInt(mTppa.Groups["azSec"].Value);
                            double azTotalArcmin = (azDeg * 60.0) + azMin + (azSec / 60.0);
                            if (mTppa.Groups["azSign"].Value == "-") azTotalArcmin = -azTotalArcmin;

                            double totalArcmin = Math.Sqrt((altTotalArcmin * altTotalArcmin) + (azTotalArcmin * azTotalArcmin));

                            allPolar.Add(new PolarAlignmentRecord {
                                Timestamp = logTimestamp,
                                AltitudeErrorArcmin = Math.Abs(altTotalArcmin),
                                AzimuthErrorArcmin = Math.Abs(azTotalArcmin),
                                TotalErrorArcmin = totalArcmin,
                                AltitudeErrorFormatted = $"{altTotalArcmin:F1}'",
                                AzimuthErrorFormatted = $"{azTotalArcmin:F1}'",
                                TotalErrorFormatted = $"{totalArcmin:F1}'",
                                SourcePlugin = "Three Point Polar Alignment"
                            });
                            continue;
                        }

                        var mTppaStd = RegexTppaStandardPattern.Match(line);
                        if (mTppaStd.Success) {
                            int altDeg = ParseInt(mTppaStd.Groups["altDeg"].Value);
                            int altMin = ParseInt(mTppaStd.Groups["altMin"].Value);
                            int altSec = ParseInt(mTppaStd.Groups["altSec"].Value);
                            double altTotalArcmin = (altDeg * 60.0) + altMin + (altSec / 60.0);
                            if (mTppaStd.Groups["altSign"].Value == "-") altTotalArcmin = -altTotalArcmin;

                            int azDeg = ParseInt(mTppaStd.Groups["azDeg"].Value);
                            int azMin = ParseInt(mTppaStd.Groups["azMin"].Value);
                            int azSec = ParseInt(mTppaStd.Groups["azSec"].Value);
                            double azTotalArcmin = (azDeg * 60.0) + azMin + (azSec / 60.0);
                            if (mTppaStd.Groups["azSign"].Value == "-") azTotalArcmin = -azTotalArcmin;

                            double totalArcmin = Math.Sqrt((altTotalArcmin * altTotalArcmin) + (azTotalArcmin * azTotalArcmin));

                            allPolar.Add(new PolarAlignmentRecord {
                                Timestamp = logTimestamp,
                                AltitudeErrorArcmin = Math.Abs(altTotalArcmin),
                                AzimuthErrorArcmin = Math.Abs(azTotalArcmin),
                                TotalErrorArcmin = totalArcmin,
                                AltitudeErrorFormatted = $"{altTotalArcmin:F1}'",
                                AzimuthErrorFormatted = $"{azTotalArcmin:F1}'",
                                TotalErrorFormatted = $"{totalArcmin:F1}'",
                                SourcePlugin = "Three Point Polar Alignment"
                            });
                            continue;
                        }

                        var mTppaAlt = RegexTppaAltFirstPattern.Match(line);
                        if (mTppaAlt.Success) {
                            int altDeg = ParseInt(mTppaAlt.Groups["altDeg"].Value);
                            int altMin = ParseInt(mTppaAlt.Groups["altMin"].Value);
                            int altSec = ParseInt(mTppaAlt.Groups["altSec"].Value);
                            double altTotalArcmin = (altDeg * 60.0) + altMin + (altSec / 60.0);
                            if (mTppaAlt.Groups["altSign"].Value == "-") altTotalArcmin = -altTotalArcmin;

                            int azDeg = ParseInt(mTppaAlt.Groups["azDeg"].Value);
                            int azMin = ParseInt(mTppaAlt.Groups["azMin"].Value);
                            int azSec = ParseInt(mTppaAlt.Groups["azSec"].Value);
                            double azTotalArcmin = (azDeg * 60.0) + azMin + (azSec / 60.0);
                            if (mTppaAlt.Groups["azSign"].Value == "-") azTotalArcmin = -azTotalArcmin;

                            double totalArcmin = Math.Sqrt((altTotalArcmin * altTotalArcmin) + (azTotalArcmin * azTotalArcmin));

                            allPolar.Add(new PolarAlignmentRecord {
                                Timestamp = logTimestamp,
                                AltitudeErrorArcmin = Math.Abs(altTotalArcmin),
                                AzimuthErrorArcmin = Math.Abs(azTotalArcmin),
                                TotalErrorArcmin = totalArcmin,
                                AltitudeErrorFormatted = $"{altTotalArcmin:F1}'",
                                AzimuthErrorFormatted = $"{azTotalArcmin:F1}'",
                                TotalErrorFormatted = $"{totalArcmin:F1}'",
                                SourcePlugin = "Three Point Polar Alignment"
                            });
                            continue;
                        }

                        // 8. Safety Abort Events
                        var mSafe = RegexSafetyAbort.Match(line);
                        if (mSafe.Success) {
                            sessionData.EmergencySafetyAbort = true;
                            sessionData.SafetyAbortReason = mSafe.Groups["Reason"].Value.Trim();
                            continue;
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine($"[EX-DEBUG] Exception while reading {logFile.Name}: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
                }

                rawProfiles.Add(new EquipmentProfileRecord {
                    ProfileName = "Raw Sub-Session",
                    StartTime = fileStart,
                    EndTime = fileEnd,
                    Equipment = currentLogEq
                });
            }

            // Ingestion of Guiding telemetry
            IngestPhd2GuidingData(sessionData, allLogFiles, allFrames);

            // Isolate active nightly session: if light frames exist, anchor session end to the end of sequence / imaging activity
            var activeLightFrames = allFrames.Where(f => !f.IsCalibrationFrame).OrderBy(f => f.Timestamp).ToList();
            if (activeLightFrames.Any()) {
                DateTime lastLightTime = activeLightFrames.Last().Timestamp;
                // Allow a 45-minute window after the last light frame for sequence completion / flat frames / parking
                DateTime sessionCutoff = lastLightTime.AddMinutes(45);

                // Filter out daytime / post-session simulator test runs or app restarts
                allFrames = allFrames.Where(f => f.Timestamp <= sessionCutoff).ToList();
                allPolar = allPolar.Where(p => p.Timestamp <= sessionCutoff).ToList();
                allAutofocus = allAutofocus.Where(a => a.Timestamp <= sessionCutoff).ToList();
                allFlips = allFlips.Where(m => m.Timestamp <= sessionCutoff).ToList();
                rawProfiles = rawProfiles.Where(p => p.StartTime <= sessionCutoff).ToList();

                // Recalculate true session end time bounded to active nightly imaging activity
                DateTime trueSessionEnd = allFrames.Any() ? allFrames.Max(f => f.Timestamp) : lastLightTime;
                if (allFlips.Any()) trueSessionEnd = new DateTime(Math.Max(trueSessionEnd.Ticks, allFlips.Max(m => m.Timestamp).Ticks));
                if (allAutofocus.Any()) trueSessionEnd = new DateTime(Math.Max(trueSessionEnd.Ticks, allAutofocus.Max(a => a.Timestamp).Ticks));

                sessionData.SessionEnd = trueSessionEnd;
            }

            // Filter sub-sessions to ONLY keep those with actual data (frames, polar alignments, AF, or flips)
            var activeProfiles = new List<EquipmentProfileRecord>();
            int subIndex = 1;
            foreach (var prof in rawProfiles) {
                bool hasData = allFrames.Any(f => f.Timestamp >= prof.StartTime && f.Timestamp <= prof.EndTime) ||
                               allPolar.Any(p => p.Timestamp >= prof.StartTime && p.Timestamp <= prof.EndTime) ||
                               allAutofocus.Any(a => a.Timestamp >= prof.StartTime && a.Timestamp <= prof.EndTime) ||
                               allFlips.Any(m => m.Timestamp >= prof.StartTime && m.Timestamp <= prof.EndTime);

                if (hasData && prof.Equipment.CameraName != "Not Connected") {
                    prof.ProfileName = $"Sub-Session {subIndex++}: {prof.Equipment.CameraName}";
                    activeProfiles.Add(prof);
                }
            }

            sessionData.EquipmentProfiles = activeProfiles;

            if (activeProfiles.Any()) {
                var mainProf = activeProfiles.FirstOrDefault(p => allFrames.Any(f => f.Timestamp >= p.StartTime && f.Timestamp <= p.EndTime)) ?? activeProfiles.First();
                sessionData.Equipment = mainProf.Equipment;
            }

            // Group Frames into Targets
            var targetGroups = allFrames.GroupBy(f => f.TargetName).ToList();
            foreach (var group in targetGroups) {
                var targetData = new TargetSessionData {
                    TargetName = group.Key,
                    StartTime = group.Min(f => f.Timestamp),
                    EndTime = group.Max(f => f.Timestamp),
                    Frames = group.OrderBy(f => f.Timestamp).ToList()
                };

                targetData.AutofocusRuns = allAutofocus
                    .Where(a => a.Timestamp >= targetData.StartTime && a.Timestamp <= targetData.EndTime)
                    .OrderBy(a => a.Timestamp)
                    .ToList();

                targetData.MeridianFlips = allFlips
                    .Where(m => m.Timestamp >= targetData.StartTime.AddMinutes(-5) && m.Timestamp <= targetData.EndTime.AddMinutes(5))
                    .OrderBy(m => m.Timestamp)
                    .ToList();

                sessionData.Targets.Add(targetData);
            }

            sessionData.PolarAlignments = allPolar.OrderBy(p => p.Timestamp).ToList();

            var lightFrames = allFrames.Where(f => !f.IsCalibrationFrame).OrderBy(f => f.Timestamp).ToList();
            if (lightFrames.Any()) {
                sessionData.FirstLightTimestamp = lightFrames.First().Timestamp;
                sessionData.LastLightTimestamp = lightFrames.Last().Timestamp;
            }

            var calibrationFrames = allFrames.Where(f => f.IsCalibrationFrame).ToList();
            if (calibrationFrames.Any()) {
                var calibSummary = calibrationFrames
                    .GroupBy(f => f.CalibrationType)
                    .Select(g => new CalibrationSummary {
                        FrameType = g.Key,
                        Count = g.Count(),
                        ExposureSeconds = g.First().ExposureSeconds
                    })
                    .ToList();
                sessionData.CalibrationFrames = calibSummary;
            }

            var calc = new SessionStatsCalculator();
            calc.CalculateStatistics(sessionData);

            return sessionData;
        }

        private static DateTime GetTargetAstroDate(string? dateFilter) {
            if (!string.IsNullOrWhiteSpace(dateFilter)) {
                string cleaned = dateFilter.Trim();
                if (DateTime.TryParseExact(cleaned, new[] { "yyyy-MM-dd", "yyyyMMdd", "MM/dd/yyyy", "dd/MM/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)) {
                    return parsed.Date;
                } else if (DateTime.TryParse(cleaned, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallbackParsed)) {
                    return fallbackParsed.Date;
                }
            }

            // Live Session: 12:00 PM previous day to 12:00 PM today (if current hour < 12) or 12:00 PM today to 12:00 PM tomorrow (if current hour >= 12)
            DateTime now = DateTime.Now;
            return now.Hour < 12 ? now.Date.AddDays(-1) : now.Date;
        }

        private static List<FileInfo> SelectLogsForSession(List<FileInfo> allLogFiles, DateTime astroWindowStart, DateTime astroWindowEnd) {
            var matched = allLogFiles
                .Where(f => {
                    DateTime st = GetLogFileStartTimestamp(f);
                    return (st >= astroWindowStart && st < astroWindowEnd) ||
                           (f.LastWriteTime > astroWindowStart && st < astroWindowEnd);
                })
                .OrderBy(f => f.Name)
                .ToList();

            if (matched.Any()) return matched;

            return allLogFiles.Where(f => f.LastWriteTime >= astroWindowStart.AddHours(-12)).OrderBy(f => f.Name).ToList();
        }

        private static string ExtractTargetFromPath(string fullPath) {
            try {
                string dir = Path.GetDirectoryName(fullPath) ?? "";
                string parent = Path.GetFileName(dir);
                if (parent.Equals("LIGHT", StringComparison.OrdinalIgnoreCase) ||
                    parent.Equals("DARK", StringComparison.OrdinalIgnoreCase) ||
                    parent.Equals("FLAT", StringComparison.OrdinalIgnoreCase) ||
                    parent.Equals("BIAS", StringComparison.OrdinalIgnoreCase)) {
                    string targetDir = Path.GetDirectoryName(dir) ?? "";
                    string targetName = Path.GetFileName(targetDir);
                    if (!string.IsNullOrWhiteSpace(targetName) && !targetName.Equals("N.I.N.A", StringComparison.OrdinalIgnoreCase)) {
                        return targetName;
                    }
                }
            } catch { }
            return string.Empty;
        }

        private static void ParseNinaFilenameTelemetry(string filename, out double expSecs, out double hfr, out int stars, out string filter, out double rms) {
            expSecs = 0;
            hfr = 0;
            stars = 0;
            filter = string.Empty;
            rms = 0;

            string nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
            string[] tokens = nameWithoutExt.Split('_');

            foreach (var token in tokens) {
                string t = token.Trim();
                if (string.IsNullOrEmpty(t)) continue;

                // Exposure: e.g. 180.00s or 180s
                if (t.EndsWith("s", StringComparison.OrdinalIgnoreCase) &&
                    double.TryParse(t.Substring(0, t.Length - 1).Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out double expVal)) {
                    expSecs = expVal;
                    continue;
                }

                // HFR: e.g. HFR2.17 or HFR_2.17
                if (t.StartsWith("HFR", StringComparison.OrdinalIgnoreCase)) {
                    string hfrNum = t.Substring(3).TrimStart('_').Replace(',', '.');
                    if (double.TryParse(hfrNum, NumberStyles.Any, CultureInfo.InvariantCulture, out double hfrVal)) {
                        hfr = hfrVal;
                        continue;
                    }
                }

                // Stars: e.g. 193STARS or 193
                if (t.EndsWith("STARS", StringComparison.OrdinalIgnoreCase)) {
                    string starNum = t.Substring(0, t.Length - 5);
                    if (int.TryParse(starNum, NumberStyles.Any, CultureInfo.InvariantCulture, out int starVal)) {
                        stars = starVal;
                        continue;
                    }
                }

                // RMS: e.g. RMS0.24 or RMS_0.24
                if (t.StartsWith("RMS", StringComparison.OrdinalIgnoreCase)) {
                    string rmsNum = t.Substring(3).TrimStart('_').Replace(',', '.');
                    if (double.TryParse(rmsNum, NumberStyles.Any, CultureInfo.InvariantCulture, out double rmsVal)) {
                        rms = rmsVal;
                        continue;
                    }
                }

                // Filter Token: e.g. IRUV, Ha, OIII, SII, L3, L2, L1, CLS, RGB, DualBand
                if (string.IsNullOrEmpty(filter) &&
                    !t.Equals("LIGHT", StringComparison.OrdinalIgnoreCase) &&
                    !t.Equals("DARK", StringComparison.OrdinalIgnoreCase) &&
                    !t.Equals("FLAT", StringComparison.OrdinalIgnoreCase) &&
                    !t.Equals("BIAS", StringComparison.OrdinalIgnoreCase) &&
                    !t.StartsWith("GAIN", StringComparison.OrdinalIgnoreCase) &&
                    !t.StartsWith("TEMP", StringComparison.OrdinalIgnoreCase) &&
                    !t.StartsWith("RMS", StringComparison.OrdinalIgnoreCase) &&
                    !t.Contains("-") && t.Length <= 10 && !t.All(char.IsDigit)) {
                    filter = t;
                }
            }
        }

        private static string filterNameSanitized(string val) {
            if (string.IsNullOrWhiteSpace(val)) return "No Filter";
            if (val.Equals("LIGHT", StringComparison.OrdinalIgnoreCase)) return "No Filter";
            return val;
        }

        private static void IngestPhd2GuidingData(SessionData sessionData, List<FileInfo> allLogFiles, List<FrameRecord> allFrames) {
            try {
                var phdFiles = new List<FileInfo>();
                foreach (var logDir in allLogFiles.Select(f => f.DirectoryName).Distinct()) {
                    if (string.IsNullOrWhiteSpace(logDir) || !Directory.Exists(logDir)) continue;
                    try {
                        var dirInfo = new DirectoryInfo(logDir);
                        phdFiles.AddRange(dirInfo.GetFiles("PHD2_Guide_*.txt"));
                        string phdAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PHD2");
                        if (Directory.Exists(phdAppData)) {
                            phdFiles.AddRange(new DirectoryInfo(phdAppData).GetFiles("PHD2_Guide_*.txt"));
                        }
                    } catch { }
                }

                phdFiles = phdFiles.GroupBy(f => f.FullName).Select(g => g.First()).OrderByDescending(f => GetLogFileStartTimestamp(f)).ToList();
                if (!phdFiles.Any()) return;

                var lightFrames = allFrames.Where(f => !f.IsCalibrationFrame && f.ExposureSeconds > 0).OrderBy(f => f.Timestamp).ToList();
                if (!lightFrames.Any()) return;

                foreach (var frame in lightFrames) {
                    DateTime frameStart = frame.Timestamp.AddSeconds(-frame.ExposureSeconds);
                    DateTime frameEnd = frame.Timestamp;

                    var matchingLog = phdFiles.FirstOrDefault(f => GetLogFileStartTimestamp(f) <= frameEnd && f.LastWriteTime >= frameStart);
                    if (matchingLog == null) matchingLog = phdFiles.First();

                    double raRmsSum = 0, decRmsSum = 0, totRmsSum = 0;
                    int guideCount = 0;

                    using var fs = new FileStream(matchingLog.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var sr = new StreamReader(fs);
                    string? line;
                    bool inGuideSection = false;

                    while ((line = sr.ReadLine()) != null) {
                        if (line.StartsWith("Guiding Begins", StringComparison.OrdinalIgnoreCase)) {
                            inGuideSection = true;
                            continue;
                        }
                        if (line.StartsWith("Guiding Ends", StringComparison.OrdinalIgnoreCase)) {
                            inGuideSection = false;
                            continue;
                        }
                        if (!inGuideSection || line.StartsWith("Frame") || line.StartsWith("#")) continue;

                        string[] parts = line.Split(',');
                        if (parts.Length >= 6) {
                            if (DateTime.TryParse(parts[1].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var gTime)) {
                                if (gTime >= frameStart && gTime <= frameEnd) {
                                    double dx = ParseDouble(parts[4]);
                                    double dy = ParseDouble(parts[5]);
                                    if (dx != 0 || dy != 0) {
                                        raRmsSum += dx * dx;
                                        decRmsSum += dy * dy;
                                        totRmsSum += (dx * dx) + (dy * dy);
                                        guideCount++;
                                    }
                                }
                            }
                        }
                    }

                    if (guideCount > 0) {
                        double pxScale = sessionData.Equipment.PixelScaleArcsec > 0 ? sessionData.Equipment.PixelScaleArcsec : 2.15;
                        frame.GuideRaRms = Math.Sqrt(raRmsSum / guideCount) * pxScale;
                        frame.GuideDecRms = Math.Sqrt(decRmsSum / guideCount) * pxScale;
                        frame.GuideTotalRms = Math.Sqrt(totRmsSum / guideCount) * pxScale;
                    }
                }
            } catch { }
        }

        private static DateTime GetLogFileStartTimestamp(FileInfo file) {
            string name = file.Name;
            if (name.Length >= 15 && name.Contains('-')) {
                string[] parts = name.Split('-');
                if (parts.Length >= 2 && parts[0].Length == 8 && parts[1].Length == 6) {
                    string datePart = parts[0];
                    string timePart = parts[1];
                    if (DateTime.TryParseExact(datePart + timePart, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileTime)) {
                        return fileTime;
                    }
                }
            }

            try {
                using var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                for (int i = 0; i < 5; i++) {
                    string? line;
                    if ((line = reader.ReadLine()) == null) break;
                    int pipeIdx = line.IndexOf('|');
                    if (pipeIdx > 0) {
                        string timeStr = line.Substring(0, pipeIdx).Trim();
                        if (DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) {
                            return dt;
                        }
                    }
                }
            } catch { }

            return file.LastWriteTime;
        }

        private static bool FalseGuideDuringDither(DateTime guideTime, DateTime? ditherStart, DateTime? ditherSettle) {
            if (ditherStart.HasValue && !ditherSettle.HasValue && guideTime >= ditherStart.Value) {
                return true;
            }
            if (ditherSettle.HasValue && guideTime >= ditherStart && guideTime <= ditherSettle.Value.AddSeconds(4)) {
                return true;
            }
            return false;
        }

        private static bool IsCalibrationFile(string filename, out string type) {
            string lower = filename.ToLowerInvariant();
            if (lower.Contains("dark_") || lower.Contains("_dark") || lower.Contains("darks")) { type = "Dark"; return true; }
            if (lower.Contains("flat_") || lower.Contains("_flat") || lower.Contains("flats")) { type = "Flat"; return true; }
            if (lower.Contains("bias_") || lower.Contains("_bias") || lower.Contains("biases")) { type = "Bias"; return true; }
            if (lower.Contains("darkflat") || lower.Contains("flatdark")) { type = "Dark Flat"; return true; }
            type = string.Empty;
            return false;
        }

        private static DateTime? ParseTimestampFromLine(string line) {
            int pipeIdx = line.IndexOf('|');
            if (pipeIdx > 0) {
                string timeStr = line.Substring(0, pipeIdx).Trim();
                if (DateTime.TryParse(timeStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) {
                    return dt;
                }
            }
            return null;
        }

        private static string SanitizeAngleDegreeString(string raw) {
            if (string.IsNullOrWhiteSpace(raw)) return raw;
            return raw.Replace("\uFFFD", "°");
        }

        private static double ParseDouble(string val) {
            if (string.IsNullOrWhiteSpace(val)) return 0.0;
            string cleaned = val.Trim().Replace(',', '.');
            if (double.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out double res)) {
                return res;
            }
            return 0.0;
        }

        private static int ParseInt(string val) {
            if (string.IsNullOrWhiteSpace(val)) return 0;
            if (int.TryParse(val.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out int res)) {
                return res;
            }
            return 0;
        }
    }
}
