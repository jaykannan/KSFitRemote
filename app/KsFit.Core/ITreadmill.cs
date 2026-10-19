namespace KsFit.Core;

public enum MachineState { Unknown, Idle, Running, Paused, Stopped }

public sealed record TreadmillData(
    double SpeedKmh, double? DistanceM, TimeSpan? Elapsed, int? Calories, double? InclinePercent, int? HeartRate);

/// <summary>Protocol-independent treadmill control surface.</summary>
public interface ITreadmill
{
    string ProtocolName { get; }
    double MinSpeed { get; }
    double MaxSpeed { get; }
    double SpeedStep { get; }
    event Action<TreadmillData>? DataReceived;
    event Action<MachineState>? StateChanged;
    Task InitAsync();
    Task Start();
    Task Pause();
    Task Stop();
    Task SetSpeed(double kmh);
    Task CloseAsync();
}
