using System;
using System.Linq;
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
    public int Incidents { get; init; }
    public int MaxIncidents { get; init; }
    public double SessionTimeRemain { get; init; }
    public int Sof { get; init; }
}

public class IRacingService : IDisposable
{
    private readonly IRacingSdk _sdk;
    private bool _disposed;

    public event EventHandler<TelemetryUpdateEventArgs>? TelemetryUpdated;
    public event EventHandler<ConnectionState>? StateChanged;

    private ConnectionState _lastState = ConnectionState.Disconnected;
    private int _maxIncidents;
    private int _sof;
    private bool _sessionInfoParsed;

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
        _sessionInfoParsed = false;
        UpdateState(ConnectionState.InMenu);
    }

    private void OnDisconnected()
    {
        _sessionInfoParsed = false;
        UpdateState(ConnectionState.Disconnected);
    }

    private void ParseSessionInfo()
    {
        try
        {
            var info = _sdk.Data.SessionInfo;
            if (info == null) return;

            // Parse IncidentLimit from WeekendOptions
            var incLimit = info.WeekendInfo?.WeekendOptions?.IncidentLimit;
            if (!string.IsNullOrEmpty(incLimit))
                _maxIncidents = incLimit == "unlimited" ? -1 : int.TryParse(incLimit, out int n) ? n : -1;

            // Calculate SOF from average iRating of all drivers
            var drivers = info.DriverInfo?.Drivers;
            if (drivers != null && drivers.Count > 0)
            {
                var ratings = drivers.Where(d => d.IRating > 0 && d.CarIsPaceCar == 0 && d.IsSpectator == 0).Select(d => d.IRating).ToList();
                if (ratings.Count > 0)
                    _sof = (int)ratings.Average();
            }

            _sessionInfoParsed = true;
        }
        catch { /* ignore parse errors */ }
    }

    private void OnTelemetryData()
    {
        try
        {
            if (!_sessionInfoParsed)
                ParseSessionInfo();

            float throttle = _sdk.Data.GetFloat("Throttle");
            float brake = _sdk.Data.GetFloat("Brake");
            bool abs = false;
            try { abs = _sdk.Data.GetBool("BrakeABSactive"); } catch { }
            if (!abs) { try { abs = _sdk.Data.GetFloat("BrakeABSCutPct") > 0; } catch { } }
            float lastLap = _sdk.Data.GetFloat("LapLastLapTime");
            float bestLap = _sdk.Data.GetFloat("LapBestLapTime");
            float delta = _sdk.Data.GetFloat("LapDeltaToBestLap");
            bool deltaOk = _sdk.Data.GetBool("LapDeltaToBestLap_OK");
            bool isOnTrack = _sdk.Data.GetBool("IsOnTrack");
            bool onPitRoad = _sdk.Data.GetBool("OnPitRoad");
            int trackSurface = _sdk.Data.GetInt("PlayerTrackSurface");

            int incidents = 0;
            try { incidents = _sdk.Data.GetInt("PlayerCarMyIncidentCount"); } catch { }

            double timeRemain = 0;
            try { timeRemain = _sdk.Data.GetDouble("SessionTimeRemain"); } catch { }

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
                State = state,
                Incidents = incidents,
                MaxIncidents = _maxIncidents,
                SessionTimeRemain = timeRemain,
                Sof = _sof
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
