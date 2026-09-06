# Telemetry Monitoring & Environmental Profiling Implementation Plan

## 1. Overview & Objectives
This feature introduces continuous background telemetry and environmental monitoring into Overnight Capture Diagnostics (OCD).

A new lightweight sequence instruction, **`Start OCD Telemetry`**, will be added to N.I.N.A. to initiate non-blocking background polling of ASCOM power switches and environmental/weather sensors at regular intervals (default: 60 seconds). At the end of the night, the standard **`Overnight Capture Diagnostics (OCD)`** sequence item stops polling, performs trapezoidal energy integration (calculating total Watt-hours and Amp-hours), derives dew-point convergence metrics, merges environmental samples into the session dataset, saves an `OCD_Telemetry.csv` snapshot, and renders a dual-axis SVG environmental chart (Temperature vs. Humidity/Dew Point) in both Markdown and HTML reports.

---

## 2. Key Architecture & Design Decisions

### A. In-Memory Thread-Safe Buffering (No File-Lock Race Conditions)
- Rather than continually opening, locking, and appending to a CSV file on disk during sequence execution (which risks `IOException` file sharing violations when the report step runs), `TelemetryMonitorService` maintains an in-memory thread-safe `ConcurrentQueue<TelemetrySample>`.
- When `OCDSequenceItem` completes at the end of the session, it flushes the in-memory sample buffer, performs analytical calculations, and exports the final `OCD_Telemetry.csv` into the session report folder.

### B. Fail-Safe Background Polling & N.I.N.A. Cached State
- In .NET, an unhandled exception inside a `System.Threading.Timer` callback terminates the entire host process (`AppDomain.UnhandledException`).
- Hardware polling calls `IWeatherDataMediator.GetInfo()` and `ISwitchMediator.GetInfo()`. These mediator calls return N.I.N.A.'s cached device state DTOs (`WeatherDataInfo` and `SwitchInfo`), which N.I.N.A. already polls on its own device timers. We do **not** invoke raw ASCOM COM objects directly from worker threads, avoiding COM STA thread affinity violations.
- **Disconnected Device Plausibility Gate:**
  - Before recording weather data, verify `weatherInfo.Connected == true` (or ensure `Temperature` and `Humidity` are not default zero placeholders) to prevent recording false 0°C / 0% measurements when no weather device is connected.
- Polling callbacks are wrapped in comprehensive `try-catch` blocks with throttled diagnostic warnings so intermittent driver faults or device disconnects will never terminate N.I.N.A. or disrupt an active imaging session.
- **Timer Re-entrancy & Thread-Safety:**
  - An `Interlocked.CompareExchange` re-entrancy guard ensures slow driver queries (> 60s) cannot overlap or enqueue duplicate/racing samples.
  - The `_isRunning` state is marked `volatile` for cross-thread visibility.

### C. Session-Scoped Lifecycle, Zombie Timer Prevention & Multi-Target Safety
- `TelemetryMonitorService.Instance.Start()` is **idempotent**:
  - If already running (e.g. if `Start OCD Telemetry` is placed inside a per-target loop or executed across multiple target containers), it updates mediator references and keeps polling without clearing existing data.
  - Telemetry samples record UTC timestamps (`Timestamp`).
- **Cancellation / Sequence Abort Guard:**
  - In `OCDTelemetryStartItem.Execute()`, if a `token` cancellation occurs, `token.Register(() => TelemetryMonitorService.Instance.Stop())` ensures that a sequence aborted before reaching the final OCD report step disarms the timer and leaves no orphan background thread.
- **Session Filtering & Warm-Up Power Retention in `Stop()`:**
  - `Stop(DateTime? sessionStart = null, DateTime? sessionEnd = null)` disarms the timer (`_timer.Change(Timeout.Infinite, Timeout.Infinite)`), drains the collected samples, and returns samples within the session window.
  - To capture pre-exposure sensor cooldown and mount slew power consumption, the window start is anchored at `min(sessionStart, firstSample.Timestamp)`.
- **Fail-Safe Cleanup in `OCDSequenceItem`:**
  - In `OCDSequenceItem.Execute()`, `Stop()` is called inside a `finally` block (or early in execution) so that aborts, sequence cancellations, or `!hasData` early exits never leave a zombie timer polling indefinitely.
- **Historic Session Protection:**
  - When analyzing historic logs (`!session.IsLiveSession`), live monitor samples are ignored/cleared to prevent cross-session data contamination.

### D. Dual Hardware Support (`ISwitchMediator` & `IWeatherDataMediator`)
- **Weather / Observing Conditions (`IWeatherDataMediator`):**
  - Reads `IWeatherDataMediator.GetInfo()` $\rightarrow$ `WeatherDataInfo`.
  - Captures `Temperature`, `Humidity`, `DewPoint`, `SkyQuality`, `CloudCover`, `Pressure`, `WindSpeed`.
- **ASCOM Switches & Power Boxes (`ISwitchMediator`):**
  - Reads `ISwitchMediator.GetInfo()` $\rightarrow$ `SwitchInfo`.
  - Iterates **both** `SwitchInfo.ReadonlySwitches` (sensor gauges) and `SwitchInfo.WritableSwitches` (dew heater controls, power ports).
  - Inspects `ISwitch.Name` and `ISwitch.Description` using case-insensitive (`StringComparison.OrdinalIgnoreCase`) token matching with strict precedence:
    - **Voltage:** `VOLTAGE`, `VOLTS`, `VOLT`, `V` (word boundary)
    - **Current:** `CURRENT`, `AMPERE`, `AMPS`, `AMP`, `MA` (milli-amps $\rightarrow$ divide by 1000.0)
    - **Power:** `POWER`, `WATTS`, `WATT`, `W` (word boundary)
    - **Dew Heaters:** `DEW`, `HEATER`, `DUTY` (0/1 digital $\rightarrow$ 0/100%; analog $\rightarrow$ 0–100%)
    - **Switch Environmentals:** `TEMP`, `TEMPERATURE`, `HUMIDITY`, `HUM`
  - Analog vs Digital differentiation: digital switches typically have `Minimum == 0, Maximum == 1, StepSize == 1` or binary values 0.0 / 1.0; gauges have continuous ranges or `Maximum > 1`.

### E. Mathematical Energy Integration & Dew Point Derivation
- **Trapezoidal Energy & Capacity Integration:**
  - **Capacity (Ah):** Primary integration performed directly on `CurrentAmps`:
    $$\text{Capacity (Ah)} = \sum_{i=1}^{n-1} \left(\frac{I_i + I_{i+1}}{2}\right) \times \frac{(t_{i+1} - t_i)_{\text{seconds}}}{3600}$$
  - **Energy (Wh):** Primary integration performed on `PowerWatts`:
    $$\text{Energy (Wh)} = \sum_{i=1}^{n-1} \left(\frac{P_i + P_{i+1}}{2}\right) \times \frac{(t_{i+1} - t_i)_{\text{seconds}}}{3600}$$
  - **Fallback Calculations:**
    - If `PowerWatts` is not directly exposed by the gauge, but `Voltage` and `CurrentAmps` are present: compute $P_i = V_i \times I_i$ per sample before integrating Wh.
    - If `CurrentAmps` is not exposed, derive capacity via $\text{Ah} = \frac{\text{Energy (Wh)}}{\bar{V}}$ (or nominal 12.0V).
- **Guarded Magnus-Tetens Dew Point Derivation:**
  - If Temperature and Relative Humidity are present but Dew Point is missing:
    - Validate domain: $RH \in (0, 100]$ and $T \in [-50, 70]^\circ\text{C}$. If outside range, skip derivation (leave Dew Point `null`).
    - Formula:
      $$\gamma(T, RH) = \frac{17.27 \cdot T}{237.7 + T} + \ln\left(\frac{RH}{100}\right), \quad T_{\text{dew}} = \frac{237.7 \cdot \gamma(T, RH)}{17.27 - \gamma(T, RH)}$$
      $$\text{Dew Margin} = T_{\text{ambient}} - T_{\text{dew}}$$

### F. Null-Safe Model Integration & Threshold Alignment
- Missing telemetry channels (e.g. device without humidity sensor) store `null` in `TelemetrySample` rather than default `0.0`, preventing corrupted statistics (`HumidityMin = 0%`) and false chart baselines.
- `session.WeatherSamples` is populated only with valid, measured channels.
- Deprecate/reconcile the redundant `MaxDewPointMargin` field in `SessionData.cs` in favor of `MinDewPointMargin`.
- **Harmonized Dew Risk Threshold:** Standardize on **`< 2.5°C`** across both anomaly warnings and SVG chart risk zones.

---

## 3. Proposed Changes

### Models
#### [NEW] `Models/TelemetrySample.cs`
```csharp
public class TelemetrySample {
    public DateTime Timestamp { get; set; }
    public double? Voltage { get; set; }
    public double? CurrentAmps { get; set; }
    public double? PowerWatts { get; set; }
    public double? AmbientTemperature { get; set; }
    public double? Humidity { get; set; }
    public double? DewPoint { get; set; }
    public double? DewHeaterDuty { get; set; }
    public double? SkyQuality { get; set; }
    public double? CloudCover { get; set; }
}
```

#### [MODIFY] [Models/SessionData.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Models/SessionData.cs)
- Add power-related telemetry properties:
  ```csharp
  public double TotalPowerConsumedWh { get; set; }
  public double TotalPowerConsumedAh { get; set; }
  public double AverageVoltage { get; set; }
  public double AverageCurrentAmps { get; set; }
  public double PeakPowerWatts { get; set; }
  public List<TelemetrySample> TelemetrySamples { get; set; } = new List<TelemetrySample>();
  ```
- Remove/reconcile redundant `MaxDewPointMargin`.

---

### Telemetry Subsystem
#### [NEW] `Services/TelemetryMonitorService.cs`
- Thread-safe singleton service (`TelemetryMonitorService.Instance`).
- **State & Fields:**
  - `private readonly ConcurrentQueue<TelemetrySample> _samples = new();`
  - `private System.Threading.Timer? _pollingTimer;`
  - `private IWeatherDataMediator? _weatherMediator;`
  - `private ISwitchMediator? _switchMediator;`
  - `private volatile bool _isRunning;`
  - `private int _isPolling = 0;`
- **Methods:**
  - `void Start(ISwitchMediator? switchMediator, IWeatherDataMediator? weatherMediator, int intervalSeconds = 60)`:
    - Thread-safe, idempotent initialization.
    - Updates mediator references; starts `_pollingTimer` if not already running.
  - `void PollCallback(object? state)`:
    - Interlocked re-entrancy guard: `if (Interlocked.CompareExchange(ref _isPolling, 1, 0) != 0) return;`
    - In `try-finally`, polls `_weatherMediator?.GetInfo()` (verifying `Connected`) and `_switchMediator?.GetInfo()`.
    - Iterates `ReadonlySwitches` and `WritableSwitches` using `OrdinalIgnoreCase` token matching with strict precedence.
    - Normalizes units (mA $\rightarrow$ A, compute $P = V \times I$ if power gauge missing).
    - Applies guarded Magnus-Tetens formula if Dew Point missing.
    - Enqueues `TelemetrySample`.
  - `List<TelemetrySample> Stop(DateTime? sessionStart = null, DateTime? sessionEnd = null)`:
    - Disarms timer (`_pollingTimer?.Change(Timeout.Infinite, Timeout.Infinite)`), sets `_isRunning = false`.
    - Drains `_samples` into a snapshot list filtered to `[min(sessionStart, firstSample.Timestamp), sessionEnd]`.
  - `void Reset()`: Clears in-memory buffer.

---

### Sequencer Instructions
#### [NEW] `Sequencer/OCDTelemetryStartItem.cs`
- N.I.N.A. Sequence Item `Start OCD Telemetry`:
  - `[Export(typeof(ISequenceItem))]`
  - Attributes: Name = `"Start OCD Telemetry"`, Category = `"Utility"`, Icon = `"OvernightCaptureDiagnosticsSVG"`.
  - Imports: `[Import] public ISwitchMediator SwitchMediator`, `[Import] public IWeatherDataMediator WeatherDataMediator`.
  - Implements: `public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token)`:
    - Calls `TelemetryMonitorService.Instance.Start(SwitchMediator, WeatherDataMediator)`.
    - Registers token cancellation hook to disarm timer on sequence abort: `token.Register(() => TelemetryMonitorService.Instance.Stop());`.
    - Reports status `"OCD: Telemetry Monitor Active"`.
    - Returns `Task.CompletedTask` immediately.
  - Implements `public override object Clone()`.

#### [MODIFY] [Sequencer/OCDSequenceItem.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Sequencer/OCDSequenceItem.cs)
- Import `IWeatherDataMediator` alongside existing mediators.
- In `Execute()`:
  - Inside a `try-finally` block, call `var liveSamples = session.IsLiveSession ? TelemetryMonitorService.Instance.Stop(session.SessionStart, session.SessionEnd) : new List<TelemetrySample>();`.
  - If not live, ensure `TelemetryMonitorService.Instance.Stop()` is invoked to prevent orphan timers.
  - Pass `liveSamples` to `SessionStatsCalculator.CalculateStatistics()`.
  - Write `OCD_Telemetry.csv` (`Timestamp, Voltage_V, Current_A, Power_W, Temp_C, Humidity_Pct, DewPoint_C, DewHeater_Pct, SQM, CloudCover_Pct`) into `targetDir` if any telemetry samples were collected.

---

### Calculations & Chart Generation
#### [MODIFY] [Services/SessionStatsCalculator.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Services/SessionStatsCalculator.cs)
- **Power Integration:**
  - Compute `TotalPowerConsumedAh` via trapezoidal integration on `CurrentAmps`.
  - Compute `TotalPowerConsumedWh` via trapezoidal integration on `PowerWatts` (or $V \times I$).
  - Calculate `AverageVoltage`, `AverageCurrentAmps`, `PeakPowerWatts`.
- **Environmental Stats Calculation:**
  - Populate `session.WeatherSamples` from `liveSamples` (merging with any log-parsed samples).
  - Compute `AmbientTempMin/Max/Avg`, `HumidityMin/Max/Avg`, `DewPointMin/Max/Avg`, `SqmAvg`, and `MinDewPointMargin` using only valid non-null / positive readings.
  - If `MinDewPointMargin < 2.5°C`, generate an Environmental Anomaly Warning.

#### [MODIFY] [Services/SvgChartGeneratorService.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Services/SvgChartGeneratorService.cs)
- Add `GenerateEnvironmentalChart(List<WeatherSample> samples, DateTime sessionStart, DateTime sessionEnd)`:
  - Responsive SVG matching OCD dark-mode aesthetic (#121824 background).
  - Decimates points (`DecimateSamples`) to keep SVG lightweight.
  - Dual Y-Axes:
    - **Left Axis:** Ambient Temperature (°C) and Dew Point (°C) line graphs.
    - **Right Axis:** Relative Humidity (%) line graph (0–100%).
  - Highlight dew risk convergence zones where margin $< 2.5^\circ\text{C}$.
  - Return empty string if fewer than 2 valid samples exist.

---

### Report Generation
#### [MODIFY] [Services/MarkdownReportWriter.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Services/MarkdownReportWriter.cs)
- **Power Summary:**
  - Gate on `session.TelemetrySamples.Any(s => s.PowerWatts.HasValue || s.CurrentAmps.HasValue || s.Voltage.HasValue)`.
  - Output: `- **⚡ Total Power Consumed:** 145.2 Wh (~12.1 Ah @ 12.1V avg | Peak: 24.5 W)`.
- **Environmental Section:**
  - Display summary table with Min, Max, Mean, Dew Margin.
  - Embed the environmental SVG chart.

#### [MODIFY] [Services/HtmlReportWriter.cs](file:///c:/Users/Nir/repos/Overnight%20Capture%20Diagnostics/Services/HtmlReportWriter.cs)
- **Power Consumption Card:**
  - Render a dedicated Summary Card for Power (Total Wh, Ah, Avg Voltage, Peak Draw).
- **Environmental Card:**
  - Display environmental metrics table and embed the interactive dual-axis Environmental SVG Chart.
  - Display Dew Risk Alert badge if `MinDewPointMargin < 2.5°C`.

---

## 4. Release & Versioning Plumbing
- Per repository rules:
  1. Bump plugin version to `1.0.6.0` in `manifest.json`.
  2. Bump `AssemblyVersion` and `AssemblyFileVersion` in `Properties\AssemblyInfo.cs`.
  3. Bump `<Version>` in `Overnight Capture Diagnostics.csproj`.
  4. Update `CHANGELOG.md` with telemetry monitoring and environmental profiling release notes.
  5. Run `dotnet build` to ensure binaries are updated.

---

## 5. Verification Plan

### Automated / Build Verification
- Run `dotnet build` to verify clean compilation with `IWeatherDataMediator`, `ISwitchMediator`, and .NET 8.
- Verify zero compiler warnings on MEF exports and nullability.

### Rig & Session Simulation Verification
1. **Sequencer UI:**
   - Verify `Start OCD Telemetry` appears under **Utility** category with custom icon.
   - Verify properties and cloning work in sequence containers.
2. **Live Execution Flow:**
   - Sequence: `Start OCD Telemetry` $\rightarrow$ `Wait 2 Minutes` $\rightarrow$ `Overnight Capture Diagnostics`.
   - Confirm non-blocking start.
   - Confirm `OCD_Telemetry.csv` written to output folder with valid columns.
   - Confirm HTML and Markdown reports render power stats and the dual-axis environmental chart.
3. **Resilience & Edge-Case Testing:**
   - **No Hardware Connected / Weather Absent:** Ensure graceful fallback (no false 0°C/0% recorded) and clean report generation.
   - **Sequence Abort / `!hasData`:** Confirm `Stop()` runs in `finally` and cancellation token disarms timer (no zombie background polling).
   - **Multi-Target Loop:** Place `Start OCD Telemetry` in a target loop $\rightarrow$ confirm telemetry accumulates across all targets without resetting.
   - **Warm-Up Power Capture:** Verify cooldown/slew energy draw before first light is included in total Wh/Ah.
   - **Historic Mode:** Run OCD on an older session date $\rightarrow$ confirm live monitor telemetry is not mixed into the historic report.
