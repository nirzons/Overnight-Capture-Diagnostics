#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.Mediator;
using NirZonshine.NINA.OvernightCaptureDiagnostics.Models;

namespace NirZonshine.NINA.OvernightCaptureDiagnostics.Services {
    public class TelemetryMonitorService {
        private static readonly Lazy<TelemetryMonitorService> _instance = new Lazy<TelemetryMonitorService>(() => new TelemetryMonitorService());
        public static TelemetryMonitorService Instance => _instance.Value;

        private readonly ConcurrentQueue<TelemetrySample> _samples = new ConcurrentQueue<TelemetrySample>();
        private readonly object _lock = new object();

        private Timer? _pollingTimer;
        private ISwitchMediator? _switchMediator;
        private IWeatherDataMediator? _weatherMediator;
        private volatile bool _isRunning;
        private int _isPolling = 0;

        public bool IsRunning => _isRunning;

        // Regex patterns with word-boundaries for switch channels
        private static readonly Regex RegexVoltage = new Regex(@"\b(VOLTAGE|VOLTS|VOLT)\b|\bV\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RegexCurrentMilliAmps = new Regex(@"\b(MILLIAMP|MILLIAMPS|MILLIAMPERE|MILLIAMPERES|MA)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RegexCurrentAmps = new Regex(@"\b(CURRENT|AMPERE|AMPERES|AMPS|AMP)\b|\bA\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RegexPower = new Regex(@"\b(POWER|WATTS|WATT)\b|\bW\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RegexDewPoint = new Regex(@"\b(DEW\s*POINT|DEWPOINT)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RegexDewHeater = new Regex(@"\b(DEW\s*HEATER|HEATER\w*|DEWHEATER|DEW\d*|PWM\w*|HEATING)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RegexTemperature = new Regex(@"\b(TEMP|TEMPERATURE)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RegexHumidity = new Regex(@"\b(HUMIDITY|HUM)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public void Start(ISwitchMediator? switchMediator, IWeatherDataMediator? weatherMediator, int intervalSeconds = 60) {
            _switchMediator = switchMediator;
            _weatherMediator = weatherMediator;

            lock (_lock) {
                if (_isRunning && _pollingTimer != null) {
                    Logger.Info("[OCD Telemetry] Start called while telemetry monitor is already running. Updated hardware mediators.");
                    return;
                }

                _isRunning = true;
                _pollingTimer = new Timer(PollCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(Math.Max(5, intervalSeconds)));
                Logger.Info($"[OCD Telemetry] Started background telemetry monitor (Polling every {intervalSeconds}s).");
            }
        }

        private void PollCallback(object? state) {
            if (!_isRunning) return;

            // Re-entrancy guard to prevent overlapping queries if hardware polling is slow
            if (Interlocked.CompareExchange(ref _isPolling, 1, 0) != 0) {
                return;
            }

            try {
                var sample = new TelemetrySample {
                    Timestamp = DateTime.Now
                };

                bool hasAnyData = false;

                // 1. Poll Weather / Observing Conditions
                try {
                    var weatherInfo = _weatherMediator?.GetInfo();
                    if (weatherInfo != null && weatherInfo.Connected) {
                        if (!double.IsNaN(weatherInfo.Temperature) && weatherInfo.Temperature > -60 && weatherInfo.Temperature < 80) {
                            sample.AmbientTemperature = weatherInfo.Temperature;
                            hasAnyData = true;
                        }

                        if (!double.IsNaN(weatherInfo.Humidity) && weatherInfo.Humidity > 0 && weatherInfo.Humidity <= 100) {
                            sample.Humidity = weatherInfo.Humidity;
                            hasAnyData = true;
                        }

                        if (!double.IsNaN(weatherInfo.DewPoint) && weatherInfo.DewPoint > -60 && weatherInfo.DewPoint < 80) {
                            sample.DewPoint = weatherInfo.DewPoint;
                            hasAnyData = true;
                        }

                        if (!double.IsNaN(weatherInfo.SkyQuality) && weatherInfo.SkyQuality > 0) {
                            sample.SkyQuality = weatherInfo.SkyQuality;
                            hasAnyData = true;
                        }

                        if (!double.IsNaN(weatherInfo.CloudCover) && weatherInfo.CloudCover >= 0) {
                            sample.CloudCover = weatherInfo.CloudCover;
                            hasAnyData = true;
                        }
                    }
                } catch (Exception ex) {
                    Logger.Debug($"[OCD Telemetry] WeatherMediator query exception: {ex.Message}");
                }

                // 2. Poll ASCOM Switch / Powerbox
                try {
                    var switchInfo = _switchMediator?.GetInfo();
                    if (switchInfo != null && switchInfo.Connected) {
                        var allSwitches = new List<ISwitch>();
                        if (switchInfo.ReadonlySwitches != null) allSwitches.AddRange(switchInfo.ReadonlySwitches);
                        if (switchInfo.WritableSwitches != null) allSwitches.AddRange(switchInfo.WritableSwitches);

                        foreach (var sw in allSwitches) {
                            if (sw == null) continue;
                            string name = sw.Name ?? string.Empty;
                            string desc = sw.Description ?? string.Empty;
                            string combined = $"{name} {desc}";
                            double val = sw.Value;

                            if (double.IsNaN(val) || double.IsInfinity(val)) continue;

                            // Check Voltage
                            if (!sample.Voltage.HasValue && RegexVoltage.IsMatch(combined) && !RegexPower.IsMatch(name) && !RegexCurrentAmps.IsMatch(name)) {
                                if (val > 0 && val < 50) {
                                    sample.Voltage = val;
                                    hasAnyData = true;
                                }
                            }
                            // Check Current (mA vs A)
                            else if (!sample.CurrentAmps.HasValue && RegexCurrentMilliAmps.IsMatch(combined)) {
                                if (val >= 0 && val < 100000) {
                                    sample.CurrentAmps = val / 1000.0;
                                    hasAnyData = true;
                                }
                            } else if (!sample.CurrentAmps.HasValue && RegexCurrentAmps.IsMatch(combined)) {
                                if (val >= 0 && val < 100) {
                                    sample.CurrentAmps = val;
                                    hasAnyData = true;
                                }
                            }
                            // Check Power
                            else if (!sample.PowerWatts.HasValue && RegexPower.IsMatch(combined)) {
                                if (val >= 0 && val < 5000) {
                                    sample.PowerWatts = val;
                                    hasAnyData = true;
                                }
                            }
                            // Check Dew Point Sensor on Switch
                            else if (!sample.DewPoint.HasValue && RegexDewPoint.IsMatch(combined)) {
                                if (val > -60 && val < 80) {
                                    sample.DewPoint = val;
                                    hasAnyData = true;
                                }
                            }
                            // Check Dew Heater Duty (PWM / Switch)
                            else if (RegexDewHeater.IsMatch(combined) && !RegexDewPoint.IsMatch(combined)) {
                                double dutyPct = val;
                                if (sw is IWritableSwitch ws) {
                                    if (ws.Maximum <= 1.0) {
                                        dutyPct = val > 0 ? 100.0 : 0.0;
                                    } else if (ws.Maximum > 1.0) {
                                        dutyPct = (val / ws.Maximum) * 100.0;
                                    }
                                } else if (val > 100.0) {
                                    dutyPct = (val / 255.0) * 100.0;
                                }
                                dutyPct = Math.Clamp(dutyPct, 0.0, 100.0);
                                sample.DewHeaterDuty = sample.DewHeaterDuty.HasValue
                                    ? Math.Max(sample.DewHeaterDuty.Value, dutyPct)
                                    : dutyPct;
                                hasAnyData = true;
                            }
                            // Check Switch Temperature
                            else if (!sample.AmbientTemperature.HasValue && RegexTemperature.IsMatch(combined)) {
                                if (val > -60 && val < 80) {
                                    sample.AmbientTemperature = val;
                                    hasAnyData = true;
                                }
                            }
                            // Check Switch Humidity
                            else if (!sample.Humidity.HasValue && RegexHumidity.IsMatch(combined)) {
                                if (val > 0 && val <= 100) {
                                    sample.Humidity = val;
                                    hasAnyData = true;
                                }
                            }
                        }
                    }
                } catch (Exception ex) {
                    Logger.Debug($"[OCD Telemetry] SwitchMediator query exception: {ex.Message}");
                }

                // 3. Fallbacks & Derived Metrics
                if (!sample.PowerWatts.HasValue && sample.Voltage.HasValue && sample.CurrentAmps.HasValue) {
                    sample.PowerWatts = sample.Voltage.Value * sample.CurrentAmps.Value;
                }

                if (!sample.CurrentAmps.HasValue && sample.PowerWatts.HasValue && sample.Voltage.HasValue && sample.Voltage.Value > 0) {
                    sample.CurrentAmps = sample.PowerWatts.Value / sample.Voltage.Value;
                }

                // Guarded Magnus-Tetens formula for Dew Point if temp & humidity are present
                if (!sample.DewPoint.HasValue && sample.AmbientTemperature.HasValue && sample.Humidity.HasValue) {
                    double t = sample.AmbientTemperature.Value;
                    double rh = sample.Humidity.Value;
                    if (rh > 0 && rh <= 100 && t >= -50 && t <= 70) {
                        double a = 17.27;
                        double b = 237.7;
                        double alpha = ((a * t) / (b + t)) + Math.Log(rh / 100.0);
                        double dew = (b * alpha) / (a - alpha);
                        if (!double.IsNaN(dew) && !double.IsInfinity(dew)) {
                            sample.DewPoint = dew;
                        }
                    }
                }

                if (hasAnyData) {
                    _samples.Enqueue(sample);
                }
            } catch (Exception ex) {
                Logger.Error($"[OCD Telemetry] Unhandled exception in PollCallback: {ex.Message}");
            } finally {
                Interlocked.Exchange(ref _isPolling, 0);
            }
        }

        public List<TelemetrySample> Stop(DateTime? sessionStart = null, DateTime? sessionEnd = null) {
            lock (_lock) {
                _isRunning = false;
                if (_pollingTimer != null) {
                    try {
                        _pollingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        _pollingTimer.Dispose();
                    } catch { }
                    _pollingTimer = null;
                }
            }

            var allSamples = new List<TelemetrySample>();
            while (_samples.TryDequeue(out var sample)) {
                allSamples.Add(sample);
            }

            if (allSamples.Count == 0) {
                return allSamples;
            }

            // Window filtering: Include warm-up power starting from the earliest sample or session start
            DateTime start = sessionStart.HasValue
                ? (allSamples[0].Timestamp < sessionStart.Value ? allSamples[0].Timestamp : sessionStart.Value)
                : DateTime.MinValue;

            DateTime end = sessionEnd ?? DateTime.MaxValue;

            var filtered = allSamples.Where(s => s.Timestamp >= start && s.Timestamp <= end).ToList();
            Logger.Info($"[OCD Telemetry] Stopped monitor. Filtered {filtered.Count} samples inside session window [{start:HH:mm:ss} - {end:HH:mm:ss}] out of {allSamples.Count} total.");
            return filtered;
        }

        public void Reset() {
            while (_samples.TryDequeue(out _)) { }
        }
    }
}
