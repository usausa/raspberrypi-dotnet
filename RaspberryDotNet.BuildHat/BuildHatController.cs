// ReSharper disable StringLiteralTypo
namespace RaspberryDotNet.BuildHat;

using System.Diagnostics;
using System.Globalization;
using System.Text;

public sealed class BuildHatController : IDisposable
{
    public const int PortCount = 4;

    private const string ActivePrefix = "connected to active ID ";

    private const string PassivePrefix = "connected to passive ID ";

    private const string PulseDoneMessage = "pulse done";

    private const string RampDoneMessage = "ramp done";

    private const string PortFaultMessage = "Port power fault";

    private const string MotorFaultMessage = "Motor power fault";

    private const string StartCommand = "port 0 ; select ; coast ; port 1 ; select ; coast ; port 2 ; select ; coast ; port 3 ; select ; coast ; echo 0";

    private const string DeselectCommand = "port 0 ; select ; port 1 ; select ; port 2 ; select ; port 3 ; select ; echo 0";

    private const string SpeedPid = "0 0 s1 1 0 0.003 0.01 0 100 0.01";

    private const string PositionPid = "0 1 s4 0.0027777778 0 5 0 .1 3 0.01";

    private const int MaxSpeed = 100;

    private const int MaxCommandLength = 250;

    private const double MinimumRampSeconds = 0.05;

    private const double RotationsPerSpeed = 0.05;

    private const double PowerScale = 1000;

    private static readonly TimeSpan ReadTimeout = TimeSpan.FromMilliseconds(500);

    private static readonly TimeSpan VoltageInterval = TimeSpan.FromSeconds(5);

    private static readonly TimeSpan SettleWait = TimeSpan.FromMilliseconds(200);

    public event EventHandler<BuildHatPortEventArgs>? PortChanged;

    public event EventHandler<BuildHatFaultEventArgs>? FaultDetected;

    public event EventHandler<ErrorEventArgs>? ConnectionLost;

#if NET9_0_OR_GREATER
    private readonly Lock sync = new();
#else
    private readonly object sync = new();
#endif

    private readonly BuildHatOptions options;

    private readonly PortState[] states = [new(), new(), new(), new()];

    private SerialDevice? device;

    private CancellationTokenSource? cts;

    private Thread? receiver;

    private bool lost;

    private bool disposed;

    private string firmware = string.Empty;

    private bool firmwareLoaded;

    private double? voltage;

    private bool powerFault;

    public IReadOnlyList<BuildHatPort> Ports { get; }

    public bool IsOpen
    {
        get
        {
            lock (sync)
            {
                return (device is not null) && !lost;
            }
        }
    }

    public BuildHatStatus Status
    {
        get
        {
            lock (sync)
            {
                return new BuildHatStatus(firmware, firmwareLoaded, voltage, powerFault);
            }
        }
    }

    public BuildHatController()
        : this(new BuildHatOptions())
    {
    }

    public BuildHatController(BuildHatOptions options)
    {
        this.options = options;
        Ports = [new BuildHatPort(this, 0), new BuildHatPort(this, 1), new BuildHatPort(this, 2), new BuildHatPort(this, 3)];
    }

    //------------------------------------------------------------------------
    // Open/Close
    //------------------------------------------------------------------------

    public void Dispose()
    {
        lock (sync)
        {
            disposed = true;
        }

        Close();
    }

    public void Open(CancellationToken token = default)
    {
        lock (sync)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (device is not null)
            {
                throw new InvalidOperationException("Build HAT is already open.");
            }
        }

        var opened = SerialDevice.Open(options.Device);
        try
        {
            var (version, loaded) = BuildHatLoader.Initialize(opened, options, token);
            opened.Write(StartCommand + "\r");
            lock (sync)
            {
                device = opened;
                firmware = version;
                firmwareLoaded = loaded;
                voltage = null;
                powerFault = false;
                lost = false;
                foreach (var state in states)
                {
                    state.Reset();
                }

                var source = new CancellationTokenSource();
                var current = opened;
                cts = source;
                receiver = new Thread(() => Receive(current, source.Token))
                {
                    IsBackground = true,
                    Name = "BuildHat"
                };
                receiver.Start();
            }

            opened = null;
        }
        finally
        {
            opened?.Dispose();
        }

        Send("list");
        Send("vin");
    }

    public void Close()
    {
        SerialDevice? current;
        Thread? thread;
        CancellationTokenSource? source;
        string stopCommand;
        lock (sync)
        {
            current = device;
            if (current is null)
            {
                return;
            }

            device = null;
            thread = receiver;
            source = cts;
            receiver = null;
            cts = null;
            source?.Cancel();
            stopCommand = CreateStopCommand();
            FailAll(new IOException("Build HAT is closed."));
        }

        if ((thread is not null) && (thread != Thread.CurrentThread))
        {
            thread.Join();
        }

        try
        {
            current.Write(stopCommand + "\r");
            current.Write(DeselectCommand + "\r");
        }
        catch (IOException)
        {
        }

        current.Dispose();

        lock (sync)
        {
            lost = false;
            foreach (var state in states)
            {
                state.Reset();
            }
        }

        source?.Dispose();
    }

    //------------------------------------------------------------------------
    // Build HAT
    //------------------------------------------------------------------------

    public void SetLedMode(BuildHatLedMode mode)
    {
        lock (sync)
        {
            RequireOpen();
            Write(String.Create(CultureInfo.InvariantCulture, $"ledmode {(int)mode}"));
        }
    }

    public void ClearFaults()
    {
        lock (sync)
        {
            RequireOpen();
            Write("clear_faults");
            powerFault = false;
        }
    }

    public void SendCommand(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        lock (sync)
        {
            RequireOpen();
            Write(command);
        }
    }

    public async Task<BuildHatDeviceType> WaitForDeviceAsync(int port, TimeSpan timeout, CancellationToken token = default)
    {
        ValidatePort(port);

        var source = new TaskCompletionSource<BuildHatDeviceType>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnPortChanged(object? sender, BuildHatPortEventArgs e)
        {
            if ((e.Port.Index == port) && (e.DeviceType is { } type))
            {
                source.TrySetResult(type);
            }
        }

        PortChanged += OnPortChanged;
        try
        {
            if (GetState(port).DeviceType is { } current)
            {
                return current;
            }

            return await source.Task.WaitAsync(timeout, token).ConfigureAwait(false);
        }
        finally
        {
            PortChanged -= OnPortChanged;
        }
    }

    public BuildHatMotor GetMotor(int port)
    {
        ValidatePort(port);
        return new BuildHatMotor(this, Ports[port]);
    }

    public BuildHatMotorPair GetMotorPair(int left, int right)
    {
        ValidatePort(left);
        ValidatePort(right);
        if (left == right)
        {
            throw new ArgumentException("Left and right must be different ports.", nameof(right));
        }

        return new BuildHatMotorPair(this, Ports[left], Ports[right]);
    }

    public BuildHatPassiveMotor GetPassiveMotor(int port)
    {
        ValidatePort(port);
        return new BuildHatPassiveMotor(this, Ports[port]);
    }

    public BuildHatLight GetLight(int port)
    {
        ValidatePort(port);
        return new BuildHatLight(this, Ports[port]);
    }

    public BuildHatColorDistanceSensor GetColorDistanceSensor(int port)
    {
        ValidatePort(port);
        return new BuildHatColorDistanceSensor(this, Ports[port]);
    }

    public BuildHatColorSensor GetColorSensor(int port)
    {
        ValidatePort(port);
        return new BuildHatColorSensor(this, Ports[port]);
    }

    public BuildHatDistanceSensor GetDistanceSensor(int port)
    {
        ValidatePort(port);
        return new BuildHatDistanceSensor(this, Ports[port]);
    }

    public BuildHatForceSensor GetForceSensor(int port)
    {
        ValidatePort(port);
        return new BuildHatForceSensor(this, Ports[port]);
    }

    public BuildHatLightMatrix GetLightMatrix(int port)
    {
        ValidatePort(port);
        return new BuildHatLightMatrix(this, Ports[port]);
    }

    public BuildHatTiltSensor GetTiltSensor(int port)
    {
        ValidatePort(port);
        return new BuildHatTiltSensor(this, Ports[port]);
    }

    public BuildHatMotionSensor GetMotionSensor(int port)
    {
        ValidatePort(port);
        return new BuildHatMotionSensor(this, Ports[port]);
    }

    //------------------------------------------------------------------------
    // Port
    //------------------------------------------------------------------------

    internal BuildHatPortState GetState(int index)
    {
        lock (sync)
        {
            var state = states[index];
            return new BuildHatPortState(state.Type, state.Speed, state.Position, state.Absolute, state.Values);
        }
    }

    internal void SelectMode(int index, int mode)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mode);

        lock (sync)
        {
            var state = RequireDataDevice(index);
            PowerUp(index, state);
            var selection = String.Create(CultureInfo.InvariantCulture, $"select {mode} ; selrate {options.DataInterval}");
            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; {ClearCombiFragment(state)}select ; {selection}"));
            state.Selection = selection;
            state.Combi = false;
            state.SelectedMode = mode;
            state.SelectedValues = null;
        }
    }

    internal void SelectCombi(int index, int[] modes)
    {
        ArgumentNullException.ThrowIfNull(modes);
        if (modes.Length == 0)
        {
            throw new ArgumentException("Modes are empty.", nameof(modes));
        }

        var builder = new StringBuilder();
        foreach (var mode in modes)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(mode, nameof(modes));
            builder.Append(CultureInfo.InvariantCulture, $" {mode} 0");
        }

        lock (sync)
        {
            var state = RequireDataDevice(index);
            PowerUp(index, state);
            var selection = String.Create(CultureInfo.InvariantCulture, $"combi 0{builder} ; select 0 ; selrate {options.DataInterval}");
            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; select ; {selection}"));
            state.Selection = selection;
            state.Combi = true;
            state.SelectedMode = null;
            state.SelectedValues = null;
        }
    }

    internal void Deselect(int index)
    {
        lock (sync)
        {
            var state = RequireDataDevice(index);
            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; {ClearCombiFragment(state)}select"));
            state.Selection = null;
            state.Combi = false;
            state.SelectedMode = null;
            state.SelectedValues = null;
        }
    }

    internal async Task<IReadOnlyList<double>> ReadOnceAsync(int index, int mode, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mode);

        ReadRequest request;
        lock (sync)
        {
            var state = RequireDataDevice(index);
            PowerUp(index, state);
            if ((state.SelectedMode == mode) && (state.SelectedValues is { } latest))
            {
                return latest;
            }

            state.Read?.Source.TrySetCanceled(CancellationToken.None);
            request = new ReadRequest(mode);
            state.Read = request;
            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; {ClearCombiFragment(state)}selonce {mode}"));
        }

        try
        {
            return await request.Source.Task.WaitAsync(options.ReadTimeout, token).ConfigureAwait(false);
        }
        finally
        {
            lock (sync)
            {
                var state = states[index];
                if (state.Read == request)
                {
                    state.Read = null;
                    RestoreSelection(index, state);
                }
            }
        }
    }

    internal void WriteData(int index, ReadOnlySpan<byte> data)
    {
        var write = WriteFragment(data);

        lock (sync)
        {
            var state = RequireDataDevice(index);
            PowerUp(index, state);
            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; {write}"));
        }
    }

    internal void WriteModeData(int index, int mode, ReadOnlySpan<byte> data)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(mode);
        var write = WriteFragment(data);

        lock (sync)
        {
            var state = RequireDataDevice(index);
            PowerUp(index, state);
            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; {ClearCombiFragment(state)}select {mode} ; selrate {options.DataInterval} ; {write} ; select"));
            RestoreSelection(index, state);
        }
    }

    internal void PowerOn(int index)
    {
        lock (sync)
        {
            var state = RequireDataDevice(index);
            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; port_plimit 1 ; on"));
            state.Powered = false;
        }
    }

    internal void PowerOff(int index)
    {
        lock (sync)
        {
            var state = RequireDataDevice(index);
            if (state.Type is { Support: BuildHatDeviceSupport.LightMatrix })
            {
                throw CreateDeviceException(index, state.Type, "must not be powered off");
            }

            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; off"));
            state.Powered = false;
        }
    }

    internal void SetPowerLimit(int index, double limit)
    {
        ValidateRatio(limit, nameof(limit));

        lock (sync)
        {
            var state = states[index];
            state.PowerLimit = limit;
            if ((device is not null) && !lost && (state.Type is not null))
            {
                Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; port_plimit {limit}"));
            }
        }
    }

    internal void PrepareColorDistanceSensor(int index) =>
        PrepareDevice(index, BuildHatDeviceSupport.ColorDistanceSensor, "is not a Color & Distance Sensor");

    internal void PrepareColorSensor(int index) =>
        PrepareDevice(index, BuildHatDeviceSupport.ColorSensor, "is not a Color Sensor");

    internal void PrepareDistanceSensor(int index) =>
        PrepareDevice(index, BuildHatDeviceSupport.DistanceSensor, "is not a Distance Sensor");

    internal void PrepareForceSensor(int index) =>
        PrepareDevice(index, BuildHatDeviceSupport.ForceSensor, "is not a Force Sensor");

    internal void PrepareLightMatrix(int index) =>
        PrepareDevice(index, BuildHatDeviceSupport.LightMatrix, "is not a 3x3 Color Light Matrix");

    internal void PrepareTiltSensor(int index) =>
        PrepareDevice(index, BuildHatDeviceSupport.TiltSensor, "is not a WeDo 2.0 Tilt Sensor");

    internal void PrepareMotionSensor(int index) =>
        PrepareDevice(index, BuildHatDeviceSupport.MotionSensor, "is not a WeDo 2.0 Motion Sensor");

    internal void WritePassiveMotor(int index, string command) =>
        WritePassive(index, BuildHatDeviceSupport.PassiveMotor, "is not a passive motor", command);

    internal void WriteLight(int index, string command) =>
        WritePassive(index, BuildHatDeviceSupport.Light, "is not a Light", command);

    private void PrepareDevice(int index, BuildHatDeviceSupport support, string reason)
    {
        lock (sync)
        {
            var state = RequireDevice(index, support, reason);
            PowerUp(index, state);
        }
    }

    private void WritePassive(int index, BuildHatDeviceSupport support, string reason, string command)
    {
        lock (sync)
        {
            RequireDevice(index, support, reason);
            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; {command}"));
        }
    }

    private void PowerUp(int index, PortState state)
    {
        if (state.Powered)
        {
            return;
        }

        var power = state.Type?.Support switch
        {
            BuildHatDeviceSupport.ColorDistanceSensor or BuildHatDeviceSupport.ColorSensor => "port_plimit 1 ; set -1",
            BuildHatDeviceSupport.DistanceSensor => "set -1",
            BuildHatDeviceSupport.LightMatrix => "port_plimit 1 ; on",
            _ => null
        };
        if (power is not null)
        {
            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; {power}"));
            state.Powered = true;
        }
    }

    //------------------------------------------------------------------------
    // Motor
    //------------------------------------------------------------------------

    internal void SetSpeed(int index, int speed)
    {
        ValidateSpeed(speed, nameof(speed));

        lock (sync)
        {
            var state = RequireMotor(index);
            WriteCommands(SpeedFragment(index, state, speed));
        }
    }

    internal void SetPower(int index, double power)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(power, -1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(power, 1);

        lock (sync)
        {
            var state = RequireMotor(index);
            WriteCommands(PowerFragment(index, state, power));
        }
    }

    internal void MoveTo(int index, int position, int speed)
    {
        ValidatePositiveSpeed(speed, nameof(speed));

        lock (sync)
        {
            var state = RequireMotor(index);
            if ((state.Mode == MotorMode.Position) && (state.Pending is null) && (state.Target == position))
            {
                return;
            }

            Write(RampFragment(index, state, position, speed, out _));
        }
    }

    internal void Coast(int index)
    {
        lock (sync)
        {
            if (FindMotor(index) is { } state)
            {
                WriteCommands(CoastFragment(index, state));
            }
        }
    }

    internal void Brake(int index)
    {
        lock (sync)
        {
            if (FindMotor(index) is { } state)
            {
                WriteCommands(PowerFragment(index, state, 0));
            }
        }
    }

    internal void Hold(int index)
    {
        lock (sync)
        {
            var state = RequireMotor(index);
            Write(HoldFragment(index, state));
        }
    }

    internal void SetPwmParameters(int index, double threshold, double minimum)
    {
        ValidateRatio(threshold, nameof(threshold));
        ValidateRatio(minimum, nameof(minimum));

        lock (sync)
        {
            var state = states[index];
            state.PwmThreshold = threshold;
            state.MinimumPwm = minimum;
            if ((device is not null) && !lost && state.Type is { Support: BuildHatDeviceSupport.Motor })
            {
                Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; pwmparams {threshold} {minimum}"));
            }
        }
    }

    internal Task RunForSecondsAsync(int index, double seconds, int speed, BuildHatStopMode stop, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seconds);
        ValidateSpeed(speed, nameof(speed));

        PendingOperation operation;
        lock (sync)
        {
            var state = RequireMotor(index);
            Write(PulseFragment(index, state, speed, seconds));
            operation = Begin(index, state, true);
        }

        return WaitOperationsAsync([operation], seconds, stop, token);
    }

    internal Task RunForDegreesAsync(int index, int degrees, int speed, BuildHatStopMode stop, CancellationToken token)
    {
        ValidateMovingSpeed(speed, nameof(speed));

        PendingOperation operation;
        double seconds;
        lock (sync)
        {
            var state = RequireMotor(index);
            var target = state.Position + (speed < 0 ? -degrees : degrees);
            Write(RampFragment(index, state, target, Math.Abs(speed), out seconds));
            operation = Begin(index, state, false);
        }

        return WaitOperationsAsync([operation], seconds, stop, token);
    }

    internal Task RunToPositionAsync(int index, int position, int speed, BuildHatStopMode stop, CancellationToken token)
    {
        ValidatePositiveSpeed(speed, nameof(speed));

        PendingOperation operation;
        double seconds;
        lock (sync)
        {
            var state = RequireMotor(index);
            Write(RampFragment(index, state, position, speed, out seconds));
            operation = Begin(index, state, false);
        }

        return WaitOperationsAsync([operation], seconds, stop, token);
    }

    internal Task RunToAbsolutePositionAsync(int index, int degrees, int speed, BuildHatDirection direction, BuildHatStopMode stop, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(degrees, -180);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(degrees, 180);
        ValidatePositiveSpeed(speed, nameof(speed));

        PendingOperation operation;
        double seconds;
        lock (sync)
        {
            var state = RequireMotor(index);
            if (state.Absolute is not { } absolute)
            {
                throw CreateDeviceException(index, state.Type, state.Type!.HasAbsolutePosition ? "has not reported its absolute position" : "has no absolute position");
            }

            var target = state.Position + GetDelta(absolute, degrees, direction);
            Write(RampFragment(index, state, target, speed, out seconds));
            operation = Begin(index, state, false);
        }

        return WaitOperationsAsync([operation], seconds, stop, token);
    }

    //------------------------------------------------------------------------
    // Motor pair
    //------------------------------------------------------------------------

    internal void SetPairSpeed(int left, int leftSpeed, int right, int rightSpeed)
    {
        ValidateSpeed(leftSpeed, nameof(leftSpeed));
        ValidateSpeed(rightSpeed, nameof(rightSpeed));

        lock (sync)
        {
            var leftState = RequireMotor(left);
            var rightState = RequireMotor(right);
            WriteCommands(SpeedFragment(left, leftState, leftSpeed), SpeedFragment(right, rightState, rightSpeed));
        }
    }

    internal void CoastPair(int left, int right)
    {
        lock (sync)
        {
            WriteCommands(
                FindMotor(left) is { } leftState ? CoastFragment(left, leftState) : string.Empty,
                FindMotor(right) is { } rightState ? CoastFragment(right, rightState) : string.Empty);
        }
    }

    internal void BrakePair(int left, int right)
    {
        lock (sync)
        {
            WriteCommands(
                FindMotor(left) is { } leftState ? PowerFragment(left, leftState, 0) : string.Empty,
                FindMotor(right) is { } rightState ? PowerFragment(right, rightState, 0) : string.Empty);
        }
    }

    internal Task RunPairForSecondsAsync(int left, int leftSpeed, int right, int rightSpeed, double seconds, BuildHatStopMode stop, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seconds);
        ValidateSpeed(leftSpeed, nameof(leftSpeed));
        ValidateSpeed(rightSpeed, nameof(rightSpeed));

        PendingOperation[] operations;
        lock (sync)
        {
            var leftState = RequireMotor(left);
            var rightState = RequireMotor(right);
            WriteCommands(PulseFragment(left, leftState, leftSpeed, seconds), PulseFragment(right, rightState, rightSpeed, seconds));
            operations = [Begin(left, leftState, true), Begin(right, rightState, true)];
        }

        return WaitOperationsAsync(operations, seconds, stop, token);
    }

    internal Task RunPairForDegreesAsync(int left, int leftSpeed, int right, int rightSpeed, int degrees, BuildHatStopMode stop, CancellationToken token)
    {
        ValidateMovingSpeed(leftSpeed, nameof(leftSpeed));
        ValidateMovingSpeed(rightSpeed, nameof(rightSpeed));

        PendingOperation[] operations;
        double seconds;
        lock (sync)
        {
            var leftState = RequireMotor(left);
            var rightState = RequireMotor(right);
            var leftTarget = leftState.Position + (leftSpeed < 0 ? -degrees : degrees);
            var rightTarget = rightState.Position + (rightSpeed < 0 ? -degrees : degrees);
            WriteCommands(
                RampFragment(left, leftState, leftTarget, Math.Abs(leftSpeed), out var leftSeconds),
                RampFragment(right, rightState, rightTarget, Math.Abs(rightSpeed), out var rightSeconds));
            operations = [Begin(left, leftState, false), Begin(right, rightState, false)];
            seconds = Math.Max(leftSeconds, rightSeconds);
        }

        return WaitOperationsAsync(operations, seconds, stop, token);
    }

    //------------------------------------------------------------------------
    // Receive
    //------------------------------------------------------------------------

    private void Receive(SerialDevice port, CancellationToken token)
    {
        var timestamp = Stopwatch.GetTimestamp();
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (port.ReadLine(ReadTimeout) is { } line)
                {
                    Process(line);
                }

                if (Stopwatch.GetElapsedTime(timestamp) >= VoltageInterval)
                {
                    Send("vin");
                    timestamp = Stopwatch.GetTimestamp();
                }
            }
        }
        catch (IOException ex)
        {
            lock (sync)
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                lost = true;
                FailAll(ex);
            }

            ConnectionLost?.Invoke(this, new ErrorEventArgs(ex));
        }
    }

    private void Process(string line)
    {
        if ((line.Length > 3) && (line[0] == 'P') && Char.IsAsciiDigit(line[1]) && (line[1] - '0' < PortCount))
        {
            var index = line[1] - '0';
            if (line[2] == ':')
            {
                ProcessPort(index, line[3..].Trim());
            }
            else if ((line[2] == 'C') || (line[2] == 'M'))
            {
                ProcessData(index, line);
            }

            return;
        }

        if (line.StartsWith(PortFaultMessage, StringComparison.Ordinal))
        {
            ProcessFault(BuildHatFault.PortPower);
            return;
        }

        if (line.StartsWith(MotorFaultMessage, StringComparison.Ordinal))
        {
            ProcessFault(BuildHatFault.MotorPower);
            return;
        }

        if (line.EndsWith(" V", StringComparison.Ordinal) &&
            Double.TryParse(line.AsSpan(0, line.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            lock (sync)
            {
                voltage = value;
            }
        }
    }

    private void ProcessPort(int index, string message)
    {
        if (message.EndsWith(PulseDoneMessage, StringComparison.Ordinal) || message.EndsWith(RampDoneMessage, StringComparison.Ordinal))
        {
            CompleteOperation(index, message.EndsWith(PulseDoneMessage, StringComparison.Ordinal));
            return;
        }

        BuildHatDeviceType? type;
        var active = message.StartsWith(ActivePrefix, StringComparison.Ordinal);
        if (active || message.StartsWith(PassivePrefix, StringComparison.Ordinal))
        {
            var id = message[(active ? ActivePrefix : PassivePrefix).Length..].Trim();
            if (!Int32.TryParse(id, active ? NumberStyles.HexNumber : NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                return;
            }

            type = BuildHatDeviceType.Find(value, active);
        }
        else if (message.StartsWith("disconnected", StringComparison.Ordinal) ||
                 message.StartsWith("no device detected", StringComparison.Ordinal) ||
                 message.EndsWith("disconnecting", StringComparison.Ordinal))
        {
            type = null;
        }
        else
        {
            return;
        }

        lock (sync)
        {
            var state = states[index];
            if ((type is null) && (state.Type is null))
            {
                return;
            }

            Fail(state, new IOException(String.Create(CultureInfo.InvariantCulture, $"Port {Ports[index].Name}: Device is disconnected.")));
            state.Reset();
            state.Type = type;
            if (type is { Support: BuildHatDeviceSupport.Motor })
            {
                Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; select ; combi 0 {(type.HasAbsolutePosition ? "1 0 2 0 3 0" : "1 0 2 0")} ; select 0 ; selrate {options.DataInterval}"));
                Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; port_plimit {state.PowerLimit ?? options.PowerLimit} ; pwmparams {state.PwmThreshold ?? options.PwmThreshold} {state.MinimumPwm ?? options.MinimumPwm}"));
            }
            else if (type is { Support: BuildHatDeviceSupport.PassiveMotor })
            {
                Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; port_plimit {state.PowerLimit ?? options.PowerLimit}"));
            }
            else if (type is { Support: BuildHatDeviceSupport.LightMatrix })
            {
                PowerUp(index, state);
            }
            else if ((type is not null) && (state.PowerLimit is { } limit))
            {
                Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; port_plimit {limit}"));
            }
        }

        PortChanged?.Invoke(this, new BuildHatPortEventArgs(Ports[index], type));
    }

    private void ProcessData(int index, string line)
    {
        var colon = line.IndexOf(':', 3);
        if ((colon < 0) || !Int32.TryParse(line.AsSpan(3, colon - 3), NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            return;
        }

        var tokens = line[(colon + 1)..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var values = new double[tokens.Length];
        for (var i = 0; i < tokens.Length; i++)
        {
            if (!Double.TryParse(tokens[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
            {
                return;
            }
        }

        var combi = line[2] == 'C';
        lock (sync)
        {
            var state = states[index];
            if (state.Type is not { } current)
            {
                return;
            }

            state.Values = values;
            if (!combi && (state.SelectedMode == number))
            {
                state.SelectedValues = values;
            }

            if (current.IsMotor && combi && (values.Length >= 2))
            {
                state.Speed = (int)values[0];
                state.Position = (int)values[1];
                state.Absolute = current.HasAbsolutePosition && (values.Length > 2) ? (int)values[2] : null;
            }

            if (!combi && (state.Read is { } read) && (read.Mode == number))
            {
                state.Read = null;
                read.Source.TrySetResult(values);
                RestoreSelection(index, state);
            }
        }
    }

    private void ProcessFault(BuildHatFault fault)
    {
        lock (sync)
        {
            powerFault = true;
        }

        FaultDetected?.Invoke(this, new BuildHatFaultEventArgs(fault));
    }

    private void CompleteOperation(int index, bool pulse)
    {
        lock (sync)
        {
            var state = states[index];
            if ((state.Pending is { } operation) && (operation.Pulse == pulse))
            {
                state.Pending = null;
                operation.Source.TrySetResult();
            }
        }
    }

    //------------------------------------------------------------------------
    // Operation
    //------------------------------------------------------------------------

    private async Task WaitOperationsAsync(PendingOperation[] operations, double seconds, BuildHatStopMode stop, CancellationToken token)
    {
        var completion = operations.Length == 1 ? operations[0].Source.Task : Task.WhenAll(operations.Select(static x => x.Source.Task));
        try
        {
            await completion.WaitAsync(TimeSpan.FromSeconds(seconds) + options.OperationTimeoutMargin, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            Finish(operations, stop, false);
            throw;
        }
        catch (TimeoutException)
        {
            Finish(operations, BuildHatStopMode.Coast, false);
            throw;
        }

        if ((stop == BuildHatStopMode.Coast) && operations.Any(static x => !x.Pulse))
        {
            try
            {
                await Task.Delay(SettleWait, token).ConfigureAwait(false);
            }
            finally
            {
                Finish(operations, stop, true);
            }

            return;
        }

        Finish(operations, stop, true);
    }

    private void Finish(PendingOperation[] operations, BuildHatStopMode stop, bool completed)
    {
        lock (sync)
        {
            if ((device is null) || lost)
            {
                return;
            }

            var commands = new List<string>(operations.Length);
            foreach (var operation in operations)
            {
                var state = states[operation.Index];
                if ((state.Sequence != operation.Sequence) || (state.Type is not { Support: BuildHatDeviceSupport.Motor }))
                {
                    continue;
                }

                if (state.Pending == operation)
                {
                    state.Pending = null;
                }

                commands.Add(stop switch
                {
                    BuildHatStopMode.Brake => PowerFragment(operation.Index, state, 0),
                    BuildHatStopMode.Hold => completed ? string.Empty : HoldFragment(operation.Index, state),
                    _ => CoastFragment(operation.Index, state)
                });
            }

            WriteCommands([.. commands]);
        }
    }

    private static PendingOperation Begin(int index, PortState state, bool pulse)
    {
        var operation = new PendingOperation(index, state.Sequence, pulse);
        state.Pending = operation;
        return operation;
    }

    private static void Supersede(PortState state)
    {
        state.Sequence++;
        if (state.Pending is { } pending)
        {
            state.Pending = null;
            pending.Source.TrySetCanceled();
        }
    }

    private void FailAll(Exception exception)
    {
        foreach (var state in states)
        {
            Fail(state, exception);
        }
    }

    private static void Fail(PortState state, Exception exception)
    {
        if (state.Pending is { } pending)
        {
            state.Pending = null;
            pending.Source.TrySetException(exception);
        }

        if (state.Read is { } read)
        {
            state.Read = null;
            read.Source.TrySetException(exception);
        }
    }

    //------------------------------------------------------------------------
    // Command
    //------------------------------------------------------------------------

    private static string SpeedFragment(int index, PortState state, int speed)
    {
        if ((state.Mode == MotorMode.Speed) && (state.Pending is null) && (state.Target == speed))
        {
            return string.Empty;
        }

        var command = state.Mode == MotorMode.Speed
            ? String.Create(CultureInfo.InvariantCulture, $"port {index} ; set {speed}")
            : String.Create(CultureInfo.InvariantCulture, $"port {index} ; pid {index} {SpeedPid} ; set {speed}");
        Supersede(state);
        state.Mode = MotorMode.Speed;
        state.Target = speed;
        return command;
    }

    private static string PowerFragment(int index, PortState state, double power)
    {
        var permille = (int)Math.Round(power * PowerScale);
        if ((state.Mode == MotorMode.Power) && (state.Pending is null) && (state.Target == permille))
        {
            return string.Empty;
        }

        Supersede(state);
        state.Mode = MotorMode.Power;
        state.Target = permille;
        return String.Create(CultureInfo.InvariantCulture, $"port {index} ; pwm ; set {permille / PowerScale:0.###}");
    }

    private static string CoastFragment(int index, PortState state)
    {
        if ((state.Mode == MotorMode.None) && (state.Pending is null))
        {
            return string.Empty;
        }

        Supersede(state);
        state.Mode = MotorMode.None;
        state.Target = 0;
        return String.Create(CultureInfo.InvariantCulture, $"port {index} ; coast");
    }

    private static string HoldFragment(int index, PortState state)
    {
        Supersede(state);
        state.Mode = MotorMode.Position;
        state.Target = state.Position;
        return String.Create(CultureInfo.InvariantCulture, $"port {index} ; pid {index} {PositionPid} ; set {state.Position / 360d:F4}");
    }

    private static string RampFragment(int index, PortState state, int target, int speed, out double seconds)
    {
        var from = state.Position / 360d;
        var to = target / 360d;
        seconds = Math.Max(MinimumRampSeconds, Math.Abs(to - from) / (speed * RotationsPerSpeed));
        Supersede(state);
        state.Mode = MotorMode.Position;
        state.Target = target;
        return String.Create(CultureInfo.InvariantCulture, $"port {index} ; pid {index} {PositionPid} ; set ramp {from:F4} {to:F4} {seconds:F3} 0");
    }

    private static string PulseFragment(int index, PortState state, int speed, double seconds)
    {
        Supersede(state);
        state.Mode = MotorMode.Speed;
        state.Target = 0;
        return String.Create(CultureInfo.InvariantCulture, $"port {index} ; pid {index} {SpeedPid} ; set pulse {speed} 0.0 {seconds:F3} 0");
    }

    private static string WriteFragment(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
        {
            throw new ArgumentException("Data is empty.", nameof(data));
        }

        var builder = new StringBuilder("write1");
        foreach (var value in data)
        {
            builder.Append(CultureInfo.InvariantCulture, $" {value:x2}");
        }

        return builder.ToString();
    }

    private static string ClearCombiFragment(PortState state) => state.Combi ? "combi 0 ; " : string.Empty;

    private string CreateStopCommand()
    {
        var builder = new StringBuilder();
        for (var i = 0; i < PortCount; i++)
        {
            if (builder.Length > 0)
            {
                builder.Append(" ; ");
            }

            if (states[i].Type is { Support: BuildHatDeviceSupport.LightMatrix })
            {
                builder.Append(CultureInfo.InvariantCulture, $"port {i} ; write1 c2 0 0 0 0 0 0 0 0 0");
            }
            else
            {
                builder.Append(CultureInfo.InvariantCulture, $"port {i} ; pwm ; coast ; off");
            }
        }

        return builder.ToString();
    }

    private void RestoreSelection(int index, PortState state)
    {
        if (state.Selection is { } selection)
        {
            Write(String.Create(CultureInfo.InvariantCulture, $"port {index} ; {selection}"));
        }
    }

    private void Send(string command)
    {
        lock (sync)
        {
            Write(command);
        }
    }

    private void WriteCommands(params string[] commands)
    {
        var line = String.Join(" ; ", commands.Where(static x => x.Length > 0));
        if (line.Length == 0)
        {
            return;
        }

        if (line.Length <= MaxCommandLength)
        {
            Write(line);
            return;
        }

        foreach (var command in commands.Where(static x => x.Length > 0))
        {
            Write(command);
        }
    }

    private void Write(string command) => device?.Write(command + "\r");

    //------------------------------------------------------------------------
    // Validation
    //------------------------------------------------------------------------

    private void RequireOpen()
    {
        if ((device is null) || lost)
        {
            throw new InvalidOperationException("Build HAT is not open.");
        }
    }

    private PortState RequireMotor(int index)
    {
        RequireOpen();
        var state = states[index];
        if (state.Type is not { Support: BuildHatDeviceSupport.Motor })
        {
            throw CreateDeviceException(index, state.Type, "is not a supported motor");
        }

        return state;
    }

    private PortState? FindMotor(int index)
    {
        if ((device is null) || lost)
        {
            return null;
        }

        var state = states[index];
        return state.Type is { Support: BuildHatDeviceSupport.Motor } ? state : null;
    }

    private PortState RequireDataDevice(int index)
    {
        RequireOpen();
        var state = states[index];
        return state.Type?.Support switch
        {
            null or BuildHatDeviceSupport.Unsupported => throw CreateDeviceException(index, state.Type, "is not supported"),
            BuildHatDeviceSupport.Motor => throw CreateDeviceException(index, state.Type, "is a motor and its data is managed by the library"),
            BuildHatDeviceSupport.PassiveMotor or BuildHatDeviceSupport.Light => throw CreateDeviceException(index, state.Type, "is a passive device and has no data"),
            _ => state
        };
    }

    private PortState RequireDevice(int index, BuildHatDeviceSupport support, string reason)
    {
        RequireOpen();
        var state = states[index];
        if (state.Type?.Support != support)
        {
            throw CreateDeviceException(index, state.Type, reason);
        }

        return state;
    }

    private static InvalidOperationException CreateDeviceException(int index, BuildHatDeviceType? type, string reason) =>
        new(type is null
            ? String.Create(CultureInfo.InvariantCulture, $"Port {(char)('A' + index)}: No device is connected.")
            : String.Create(CultureInfo.InvariantCulture, $"Port {(char)('A' + index)}: {type.Name} {reason}. support=[{type.Support}]"));

    private static void ValidatePort(int port)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(port, PortCount);
    }

    private static void ValidateSpeed(int speed, string name)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(speed, -MaxSpeed, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(speed, MaxSpeed, name);
    }

    private static void ValidateMovingSpeed(int speed, string name)
    {
        ValidateSpeed(speed, name);
        ArgumentOutOfRangeException.ThrowIfZero(speed, name);
    }

    private static void ValidatePositiveSpeed(int speed, string name)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(speed, 1, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(speed, MaxSpeed, name);
    }

    private static void ValidateRatio(double value, string name)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 0, name);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 1, name);
    }

    private static int GetDelta(int current, int target, BuildHatDirection direction)
    {
        var clockwise = (((target - current) % 360) + 360) % 360;
        return direction switch
        {
            BuildHatDirection.Clockwise => clockwise,
            BuildHatDirection.Anticlockwise => clockwise == 0 ? 0 : clockwise - 360,
            _ => clockwise >= 180 ? clockwise - 360 : clockwise
        };
    }

    //------------------------------------------------------------------------
    // State
    //------------------------------------------------------------------------

    private enum MotorMode
    {
        None,
        Speed,
        Position,
        Power
    }

    private sealed class PortState
    {
        public BuildHatDeviceType? Type { get; set; }

        public int Speed { get; set; }

        public int Position { get; set; }

        public int? Absolute { get; set; }

        public IReadOnlyList<double> Values { get; set; } = [];

        public string? Selection { get; set; }

        public bool Combi { get; set; }

        public int? SelectedMode { get; set; }

        public IReadOnlyList<double>? SelectedValues { get; set; }

        public bool Powered { get; set; }

        public MotorMode Mode { get; set; }

        public int Target { get; set; }

        public int Sequence { get; set; }

        public PendingOperation? Pending { get; set; }

        public ReadRequest? Read { get; set; }

        public double? PowerLimit { get; set; }

        public double? PwmThreshold { get; set; }

        public double? MinimumPwm { get; set; }

        public void Reset()
        {
            Type = null;
            Speed = 0;
            Position = 0;
            Absolute = null;
            Values = [];
            Selection = null;
            Combi = false;
            SelectedMode = null;
            SelectedValues = null;
            Powered = false;
            Mode = MotorMode.None;
            Target = 0;
            Pending = null;
            Read = null;
        }
    }

    private sealed class PendingOperation
    {
        public int Index { get; }

        public int Sequence { get; }

        public bool Pulse { get; }

        public TaskCompletionSource Source { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public PendingOperation(int index, int sequence, bool pulse)
        {
            Index = index;
            Sequence = sequence;
            Pulse = pulse;
        }
    }

    private sealed class ReadRequest
    {
        public int Mode { get; }

        public TaskCompletionSource<IReadOnlyList<double>> Source { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ReadRequest(int mode)
        {
            Mode = mode;
        }
    }
}
