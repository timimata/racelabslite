using System;
using IRSDKSharper;

namespace PedalTelemetry.Services;

public enum ConnectionState
{
    Disconnected,
    InMenu,
    InPit,
    OnTrack
}

public class TelemetryUpdateEventArgs : EventArgs
{
    public float Throttle { get; init; }
    public float Brake { get; init; }
    public bool Abs { get; init; }
    public float LastLapTime { get; init; }
    public float BestLapTime { get; init; }
    public float DeltaToBest { get; init; }
    public bool DeltaValid { get; init; }
    public ConnectionState State { get; init; }
}

public class IRacingService : IDisposable
{
    private readonly IRacingSdk _sdk;
    private bool _disposed;

    public event EventHandler<TelemetryUpdateEventArgs>? TelemetryUpdated;
    public event EventHandler<ConnectionState>? StateChanged;

    private ConnectionState _lastState = ConnectionState.Disconnected;

    public IRacingService()
    {
        _sdk = new IRacingSdk();
        _sdk.OnConnected += OnConnected;
        _sdk.OnDisconnected += OnDisconnected;
        _sdk.OnTelemetryData += OnTelemetryData;
        _sdk.UpdateInterval = 1; // every frame (~60Hz)
    }

    public void Start() => _sdk.Start();
    public void Stop() => _sdk.Stop();

    private void OnConnected()
    {
        UpdateState(ConnectionState.InMenu);
    }

    private void OnDisconnected()
    {
        UpdateState(ConnectionState.Disconnected);
    }

    private void OnTelemetryData()
    {
        try
        {
            float throttle = _sdk.Data.GetFloat("Throttle");
            float brake = _sdk.Data.GetFloat("Brake");
            bool abs = false;
            try { abs = _sdk.Data.GetBool("ABSActive"); } catch { abs = false; }
            float lastLap = _sdk.Data.GetFloat("LapLastLapTime");
            float bestLap = _sdk.Data.GetFloat("LapBestLapTime");
            float delta = _sdk.Data.GetFloat("LapDeltaToBestLap");
            bool deltaOk = _sdk.Data.GetBool("LapDeltaToBestLap_OK");
            bool isOnTrack = _sdk.Data.GetBool("IsOnTrack");
            bool onPitRoad = _sdk.Data.GetBool("OnPitRoad");
            int trackSurface = _sdk.Data.GetInt("PlayerTrackSurface");

            // Determine state
            ConnectionState state;
            if (!isOnTrack || trackSurface == -1)
                state = ConnectionState.InMenu;
            else if (onPitRoad || trackSurface == 1 || trackSurface == 2)
                state = ConnectionState.InPit;
            else
                state = ConnectionState.OnTrack;

            if (state != _lastState)
                UpdateState(state);

            TelemetryUpdated?.Invoke(this, new TelemetryUpdateEventArgs
            {
                Throttle = Math.Clamp(throttle, 0f, 1f),
                Brake = Math.Clamp(brake, 0f, 1f),
                Abs = abs,
                LastLapTime = lastLap,
                BestLapTime = bestLap,
                DeltaToBest = delta,
                DeltaValid = deltaOk,
                State = state
            });
        }
        catch
        {
            // Ignore read errors during transitions
        }
    }

    private void UpdateState(ConnectionState state)
    {
        _lastState = state;
        StateChanged?.Invoke(this, state);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _sdk.Stop();
        GC.SuppressFinalize(this);
    }
}
