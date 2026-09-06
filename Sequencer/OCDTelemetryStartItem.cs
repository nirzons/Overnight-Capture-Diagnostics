#nullable enable
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Sequencer.SequenceItem;
using NINA.WPF.Base.Interfaces.Mediator;
using NirZonshine.NINA.OvernightCaptureDiagnostics.Services;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Sequencer {

    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Start OCD Telemetry")]
    [ExportMetadata("Description", "Starts background polling of power and environmental telemetry (temperature, humidity, dew point, voltage, current) for overnight diagnostics.")]
    [ExportMetadata("Icon", "OvernightCaptureDiagnosticsSVG")]
    [ExportMetadata("Category", "Utility")]
    [JsonObject(MemberSerialization.OptIn)]
    public class OCDTelemetryStartItem : SequenceItem {

        [Import]
        public ISwitchMediator? SwitchMediator { get; set; }

        [Import]
        public IWeatherDataMediator? WeatherDataMediator { get; set; }

        private int intervalSeconds = 60;
        [JsonProperty]
        public int IntervalSeconds {
            get => intervalSeconds;
            set {
                intervalSeconds = Math.Max(5, value);
                RaisePropertyChanged(nameof(IntervalSeconds));
            }
        }

        private string currentReadout = "--";
        public string CurrentReadout {
            get => currentReadout;
            set {
                currentReadout = value;
                RaisePropertyChanged(nameof(CurrentReadout));
            }
        }

        public OCDTelemetryStartItem() {
            Name = "Start OCD Telemetry";
            Description = "Starts background polling of power and environmental telemetry (temperature, humidity, dew point, voltage, current) for overnight diagnostics.";
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

        public override Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            token.ThrowIfCancellationRequested();

            progress.Report(new ApplicationStatus { Status = "OCD: Initializing Telemetry Monitor..." });

            // Start background monitor
            TelemetryMonitorService.Instance.Start(SwitchMediator, WeatherDataMediator, IntervalSeconds);

            // Register cancellation hook so if sequence is aborted, the background timer disarms
            token.Register(() => {
                TelemetryMonitorService.Instance.Stop();
            });

            CurrentReadout = "Monitoring Active";
            progress.Report(new ApplicationStatus { Status = "OCD: Telemetry Monitor Active" });

            return Task.CompletedTask;
        }

        public override object Clone() {
            return new OCDTelemetryStartItem {
                Name = this.Name,
                Description = this.Description,
                Icon = this.Icon,
                Category = this.Category,
                IntervalSeconds = this.IntervalSeconds,
                CurrentReadout = "--",
                SwitchMediator = this.SwitchMediator,
                WeatherDataMediator = this.WeatherDataMediator
            };
        }

        public override void ResetProgress() {
            base.ResetProgress();
            CurrentReadout = "--";
        }
    }
}
