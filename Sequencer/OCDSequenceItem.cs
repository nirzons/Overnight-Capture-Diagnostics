using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Profile.Interfaces;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using NirZonshine.NINA.OvernightCaptureDiagnostics.Services;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Sequencer {

    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Overnight Capture Diagnostics (OCD)")]
    [ExportMetadata("Description", "Gathers session telemetry from N.I.N.A. logs and hardware mediators, generating a visual MD and HTML diagnostic report upon sequence completion.")]
    [ExportMetadata("Icon", "OvernightCaptureDiagnosticsSVG")]
    [ExportMetadata("Category", "Utility")]
    [JsonObject(MemberSerialization.OptIn)]
    public class OCDSequenceItem : SequenceItem {

        [Import]
        public ICameraMediator CameraMediator { get; set; }

        [Import]
        public ITelescopeMediator TelescopeMediator { get; set; }

        [Import]
        public IFocuserMediator FocuserMediator { get; set; }

        [Import]
        public IFilterWheelMediator FilterWheelMediator { get; set; }

        [Import]
        public IGuiderMediator GuiderMediator { get; set; }

        [Import]
        public ISwitchMediator SwitchMediator { get; set; }

        [Import]
        public IProfileService ProfileService { get; set; }

        // Settings Properties
        private string targetSessionDate = string.Empty;
        [JsonProperty]
        public string TargetSessionDate {
            get => targetSessionDate;
            set {
                targetSessionDate = value;
                RaisePropertyChanged(nameof(TargetSessionDate));
            }
        }

        private string reportOutputPath = string.Empty;
        [JsonProperty]
        public string ReportOutputPath {
            get => reportOutputPath;
            set {
                reportOutputPath = value;
                RaisePropertyChanged(nameof(ReportOutputPath));
            }
        }

        private string reportTitle = "OCD Session Report";
        [JsonProperty]
        public string ReportTitle {
            get => reportTitle;
            set {
                reportTitle = value;
                RaisePropertyChanged(nameof(ReportTitle));
            }
        }

        private bool generateMarkdown = true;
        [JsonProperty]
        public bool GenerateMarkdown {
            get => generateMarkdown;
            set {
                generateMarkdown = value;
                RaisePropertyChanged(nameof(GenerateMarkdown));
            }
        }

        private bool generateHtml = true;
        [JsonProperty]
        public bool GenerateHtml {
            get => generateHtml;
            set {
                generateHtml = value;
                RaisePropertyChanged(nameof(GenerateHtml));
            }
        }

        private bool autoOpenHtmlReport = true;
        [JsonProperty]
        public bool AutoOpenHtmlReport {
            get => autoOpenHtmlReport;
            set {
                autoOpenHtmlReport = value;
                RaisePropertyChanged(nameof(AutoOpenHtmlReport));
            }
        }

        private bool enableDiscordWebhook = false;
        [JsonProperty]
        public bool EnableDiscordWebhook {
            get => enableDiscordWebhook;
            set {
                enableDiscordWebhook = value;
                RaisePropertyChanged(nameof(EnableDiscordWebhook));
            }
        }

        private bool enableDebugLogging = false;
        [JsonProperty]
        public bool EnableDebugLogging {
            get => enableDebugLogging;
            set {
                enableDebugLogging = value;
                RaisePropertyChanged(nameof(EnableDebugLogging));
            }
        }

        private string discordWebhookUrl = string.Empty;
        [JsonProperty]
        public string DiscordWebhookUrl {
            get => discordWebhookUrl;
            set {
                discordWebhookUrl = value;
                RaisePropertyChanged(nameof(DiscordWebhookUrl));
            }
        }

        private string currentReadout = "--";
        public string CurrentReadout {
            get => currentReadout;
            set {
                currentReadout = value;
                if (System.Windows.Application.Current != null && System.Windows.Application.Current.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess()) {
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                        RaisePropertyChanged(nameof(CurrentReadout));
                        Name = (!string.IsNullOrEmpty(value) && value != "--") ? $"OCD Report ({value})" : "Overnight Capture Diagnostics (OCD)";
                        RaisePropertyChanged(nameof(Name));
                    });
                } else {
                    RaisePropertyChanged(nameof(CurrentReadout));
                    Name = (!string.IsNullOrEmpty(value) && value != "--") ? $"OCD Report ({value})" : "Overnight Capture Diagnostics (OCD)";
                    RaisePropertyChanged(nameof(Name));
                }
            }
        }

        public OCDSequenceItem() {
            Name = "Overnight Capture Diagnostics (OCD)";
            Description = "Gathers session telemetry from N.I.N.A. logs and hardware mediators, generating a visual MD and HTML diagnostic report upon sequence completion.";
            Category = "Utility";

            RegisterCustomIcon();
        }

        private void RegisterCustomIcon() {
            try {
                if (System.Windows.Application.Current != null) {
                    var resource = System.Windows.Application.Current.TryFindResource("OvernightCaptureDiagnosticsSVG");
                    if (resource is System.Windows.Media.GeometryGroup geoGroup) {
                        Icon = geoGroup;
                    } else {
                        var group = new System.Windows.Media.GeometryGroup();
                        group.Children.Add(System.Windows.Media.Geometry.Parse("M2,18 H22 V20 H2 Z"));
                        group.Children.Add(System.Windows.Media.Geometry.Parse("M4,17 V12 H7 V17 Z"));
                        group.Children.Add(System.Windows.Media.Geometry.Parse("M9,17 V8 H12 V17 Z"));
                        group.Children.Add(System.Windows.Media.Geometry.Parse("M14,17 V4 H17 V17 Z"));
                        group.Children.Add(System.Windows.Media.Geometry.Parse("M4,10 L9,6 L14,7 L20,2"));
                        group.Freeze();

                        if (!System.Windows.Application.Current.Resources.Contains("OvernightCaptureDiagnosticsSVG")) {
                            System.Windows.Application.Current.Resources.Add("OvernightCaptureDiagnosticsSVG", group);
                        }
                        Icon = group;
                    }
                }
            } catch {
                try {
                    if (System.Windows.Application.Current != null) {
                        var resource = System.Windows.Application.Current.TryFindResource("TelescopeSVG");
                        if (resource is System.Windows.Media.GeometryGroup fallbackGroup) {
                            Icon = fallbackGroup;
                        }
                    }
                } catch { }
            }
        }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            progress.Report(new ApplicationStatus { Status = "OCD: Initializing Diagnostics..." });
            CurrentReadout = "Analyzing Logs";

            await Task.Run(async () => {
                token.ThrowIfCancellationRequested();

                // 1. Parse Logs by Session Date
                var parser = new LogParserService();
                var session = parser.ParseLogFiles(TargetSessionDate, token, null, EnableDebugLogging);

                // Auto-detect Live vs Historic session
                DateTime now = DateTime.Now;
                DateTime currentAstroDate = now.Hour < 12 ? now.Date.AddDays(-1) : now.Date;

                bool isLive = string.IsNullOrWhiteSpace(TargetSessionDate) || session.SessionStart.Date == currentAstroDate;
                session.IsLiveSession = isLive;

                if (isLive) {
                    session.SessionEnd = DateTime.Now;
                }

                bool hasData = session != null && session.Targets != null && session.Targets.Any(t => t.Frames != null && t.Frames.Count > 0);

                var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string versionStr = ver != null ? $"v{ver.Major}.{ver.Minor}.{ver.Build}.{ver.Revision}" : "v1.0.4.0";

                if (!hasData) {
                    string logsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA", "Logs");
                    string detailedError = isLive
                        ? $"Report generation skipped: No valid capture session telemetry found in log files inside '{logsFolder}'."
                        : $"Report generation skipped: No log files or valid capture telemetry found matching session date '{TargetSessionDate}' inside '{logsFolder}'.";

                    string debugState = EnableDebugLogging ? "Enabled" : "Disabled";
                    string sessionType = isLive ? "Live" : "Historic";
                    Logger.Info($"[Overnight Capture Diagnostics {versionStr}] Execution finished. No report was created (No session data found). Session Type: {sessionType}, Debug Mode: {debugState}.");

                    string userNotice = isLive
                        ? "Overnight Capture Diagnostics: No active or recent capture session logs found."
                        : $"Overnight Capture Diagnostics: No session logs found for date '{TargetSessionDate}'.";

                    Notification.ShowError(userNotice);

                    CurrentReadout = "No Session Found";
                    progress.Report(new ApplicationStatus { Status = "OCD: No Session Data Found" });
                    return;
                }

                // 2. Determine Output Directory
                string targetDir = ReportOutputPath;
                if (string.IsNullOrWhiteSpace(targetDir)) {
                    string defaultImageDir = ProfileService?.ActiveProfile?.ImageFileSettings?.FilePath;
                    if (!string.IsNullOrWhiteSpace(defaultImageDir) && Directory.Exists(defaultImageDir)) {
                        targetDir = Path.Combine(defaultImageDir, "OCD_Reports");
                    } else {
                        string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        targetDir = Path.Combine(docs, "OCD_Reports");
                    }
                }

                if (!Directory.Exists(targetDir)) {
                    Directory.CreateDirectory(targetDir);
                }

                // Populate Equipment info (merging live Mediators for live session vs log-parsed for historic)
                PopulateEquipmentDetails(session);

                if (session.Equipment != null && session.Equipment.SiteLatitude != 0 && session.Equipment.SiteLongitude != 0) {
                    string locationName = await ReverseGeocodingService.GetLocationNameAsync(session.Equipment.SiteLatitude, session.Equipment.SiteLongitude);
                    if (!string.IsNullOrWhiteSpace(locationName)) {
                        session.Equipment.SiteName = locationName;
                    }
                }

                var calculator = new SessionStatsCalculator();
                calculator.CalculateStatistics(session, EnableDebugLogging);

                var chartService = new SvgChartGeneratorService();
                string timestampStr = DateTime.Now.ToString("yyyy-MM-dd_HHmmss");

                // 3. Generate Markdown Report
                if (GenerateMarkdown) {
                    var mdWriter = new MarkdownReportWriter();
                    string mdContent = mdWriter.GenerateMarkdownReport(session, chartService);
                    string mdPath = Path.Combine(targetDir, $"OCD_Report_{timestampStr}.md");
                    File.WriteAllText(mdPath, mdContent);
                }

                // 4. Generate HTML Report
                string htmlPath = string.Empty;
                if (GenerateHtml) {
                    var htmlWriter = new HtmlReportWriter();
                    string htmlContent = htmlWriter.GenerateHtmlReport(session, chartService);
                    htmlPath = Path.Combine(targetDir, $"OCD_Report_{timestampStr}.html");
                    File.WriteAllText(htmlPath, htmlContent);
                }

                // 5. Post Discord Webhook
                if (EnableDiscordWebhook && !string.IsNullOrWhiteSpace(DiscordWebhookUrl)) {
                    var webhook = new WebhookService();
                    await webhook.PostDiscordSummary(DiscordWebhookUrl, session);
                }

                string reportFileName = !string.IsNullOrEmpty(htmlPath) 
                    ? Path.GetFileName(htmlPath) 
                    : (GenerateMarkdown ? $"OCD_Report_{timestampStr}.md" : $"OCD_Report_{timestampStr}");
                string activeDebugState = EnableDebugLogging ? "Enabled" : "Disabled";
                string activeSessionType = isLive ? "Live" : "Historic";

                Logger.Info($"[Overnight Capture Diagnostics {versionStr}] Execution finished. Created {activeSessionType} report '{reportFileName}' in '{targetDir}'. Debug Mode: {activeDebugState}.");

                CurrentReadout = "Complete";
                progress.Report(new ApplicationStatus { Status = "OCD: Report Generated Successfully!" });

                // 6. Auto-Open HTML Report
                if (GenerateHtml && AutoOpenHtmlReport && File.Exists(htmlPath)) {
                    try {
                        Process.Start(new ProcessStartInfo {
                            FileName = htmlPath,
                            UseShellExecute = true
                        });
                    } catch {
                        // Ignore browser opening failures
                    }
                }

            }, token);
        }

        private void PopulateEquipmentDetails(Models.SessionData session) {
            if (session.Equipment == null) session.Equipment = new Models.EquipmentDetails();

            var camera = CameraMediator?.GetInfo();
            if (camera != null && camera.Connected && !string.IsNullOrWhiteSpace(camera.Name)) {
                if (string.IsNullOrWhiteSpace(session.Equipment.CameraName) || session.Equipment.CameraName == "Not Connected") {
                    session.Equipment.CameraName = camera.Name;
                }
                if (session.Equipment.CameraWidth == 0) session.Equipment.CameraWidth = camera.XSize;
                if (session.Equipment.CameraHeight == 0) session.Equipment.CameraHeight = camera.YSize;
                if (session.Equipment.PixelSizeMicrons == 0) session.Equipment.PixelSizeMicrons = camera.PixelSize;
                if (session.Equipment.CameraTempSetpoint == 0) session.Equipment.CameraTempSetpoint = camera.TemperatureSetPoint;
            }

            var telescope = TelescopeMediator?.GetInfo();
            if (telescope != null && telescope.Connected && !string.IsNullOrWhiteSpace(telescope.Name)) {
                // In N.I.N.A, the TelescopeMediator refers to the Mount.
                if (string.IsNullOrWhiteSpace(session.Equipment.MountName) || session.Equipment.MountName == "Not Connected") {
                    session.Equipment.MountName = telescope.Name;
                }
            }

            var scopeSettings = ProfileService?.ActiveProfile?.TelescopeSettings;
            if (scopeSettings != null) {
                if (scopeSettings.FocalLength > 0 && !double.IsNaN(scopeSettings.FocalLength) && session.Equipment.FocalLengthMm == 0) {
                    session.Equipment.FocalLengthMm = scopeSettings.FocalLength;
                }
                
                if (scopeSettings.FocalRatio > 0 && !double.IsNaN(scopeSettings.FocalRatio) && session.Equipment.ApertureMm == 0) {
                    session.Equipment.ApertureMm = session.Equipment.FocalLengthMm / scopeSettings.FocalRatio;
                }

                if (!string.IsNullOrWhiteSpace(scopeSettings.Name) && (string.IsNullOrWhiteSpace(session.Equipment.TelescopeName) || session.Equipment.TelescopeName == "Not Connected")) {
                    session.Equipment.TelescopeName = scopeSettings.Name;
                }
                if (!string.IsNullOrWhiteSpace(scopeSettings.MountName)) {
                    session.Equipment.MountName = scopeSettings.MountName;
                }
            }

            var site = ProfileService?.ActiveProfile?.AstrometrySettings;
            if (site != null) {
                if (site.Latitude != 0) session.Equipment.SiteLatitude = site.Latitude;
                if (site.Longitude != 0) session.Equipment.SiteLongitude = site.Longitude;
                if (site.Elevation != 0) session.Equipment.SiteElevation = site.Elevation;
            }

            var focuser = FocuserMediator?.GetInfo();
            if (focuser != null && focuser.Connected && !string.IsNullOrWhiteSpace(focuser.Name)) {
                session.Equipment.FocuserName = focuser.Name;
                session.Equipment.FocuserPosition = focuser.Position;
            }

            var fw = FilterWheelMediator?.GetInfo();
            if (fw != null && fw.Connected && !string.IsNullOrWhiteSpace(fw.Name)) {
                session.Equipment.FilterWheelName = fw.Name;
            }

            var guider = GuiderMediator?.GetInfo();
            if (guider != null && guider.Connected && !string.IsNullOrWhiteSpace(guider.Name)) {
                session.Equipment.GuiderName = guider.Name;
            }
        }

        public override object Clone() {
            return new OCDSequenceItem {
                Name = this.Name,
                Description = this.Description,
                Icon = this.Icon,
                Category = this.Category,
                TargetSessionDate = this.TargetSessionDate,
                ReportOutputPath = this.ReportOutputPath,
                ReportTitle = this.ReportTitle,
                GenerateMarkdown = this.GenerateMarkdown,
                GenerateHtml = this.GenerateHtml,
                AutoOpenHtmlReport = this.AutoOpenHtmlReport,
                EnableDebugLogging = this.EnableDebugLogging,
                EnableDiscordWebhook = this.EnableDiscordWebhook,
                DiscordWebhookUrl = this.DiscordWebhookUrl,
                CurrentReadout = "--",
                CameraMediator = this.CameraMediator,
                TelescopeMediator = this.TelescopeMediator,
                FocuserMediator = this.FocuserMediator,
                FilterWheelMediator = this.FilterWheelMediator,
                GuiderMediator = this.GuiderMediator,
                SwitchMediator = this.SwitchMediator,
                ProfileService = this.ProfileService
            };
        }

        public override void ResetProgress() {
            base.ResetProgress();
            CurrentReadout = "--";
            Name = "Overnight Capture Diagnostics (OCD)";
            RaisePropertyChanged(nameof(Name));
        }
    }
}
