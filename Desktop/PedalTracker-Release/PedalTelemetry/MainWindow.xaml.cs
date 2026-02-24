using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PedalTelemetry.Services;

namespace PedalTelemetry;

public partial class MainWindow : Window
{
    private readonly IRacingService _iracing;
    private readonly System.Windows.Forms.NotifyIcon _trayIcon;

    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(0x00, 0xC8, 0x00));
    private static readonly SolidColorBrush OrangeBrush = new(Color.FromRgb(0xFF, 0xAA, 0x00));
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(0xE0, 0x20, 0x20));
    private static readonly SolidColorBrush DimBrush = new(Color.FromRgb(0x88, 0x88, 0x88));
    private static readonly SolidColorBrush DarkGreenBrush = new(Color.FromRgb(0x0A, 0x3D, 0x0A));
    private static readonly SolidColorBrush DarkRedBrush = new(Color.FromRgb(0x3D, 0x0A, 0x0A));
    private static readonly SolidColorBrush NeutralBgBrush = new(Color.FromRgb(0x2A, 0x2A, 0x2A));
    private static readonly SolidColorBrush PurpleBrush = new(Color.FromRgb(0xBB, 0x44, 0xFF));
    private static readonly SolidColorBrush DefaultLapBrush = new(Color.FromRgb(0xCC, 0xCC, 0xCC));

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    static MainWindow()
    {
        GreenBrush.Freeze();
        OrangeBrush.Freeze();
        RedBrush.Freeze();
        DimBrush.Freeze();
        DarkGreenBrush.Freeze();
        DarkRedBrush.Freeze();
        NeutralBgBrush.Freeze();
        PurpleBrush.Freeze();
        DefaultLapBrush.Freeze();
    }

    public MainWindow()
    {
        InitializeComponent();

        _iracing = new IRacingService();
        _iracing.TelemetryUpdated += OnTelemetryUpdated;
        _iracing.StateChanged += OnStateChanged;
        _iracing.Start();

        // Tray icon
        _trayIcon = new System.Windows.Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!),
            Text = "Pedal Telemetry",
            Visible = true
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        var trayMenu = new System.Windows.Forms.ContextMenuStrip();
        trayMenu.Items.Add("Show", null, (_, _) => RestoreFromTray());
        trayMenu.Items.Add("Exit", null, (_, _) => { _trayIcon.Visible = false; Close(); });
        _trayIcon.ContextMenuStrip = trayMenu;

        SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
        };
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OnTelemetryUpdated(object? sender, TelemetryUpdateEventArgs e)
    {
        Dispatcher.InvokeAsync(() =>
        {
            // Update chart
            Chart.AddDataPoint(e.Throttle, e.Brake, e.Abs);

            // Update vertical bars
            int throttlePct = (int)(e.Throttle * 100);
            int brakePct = (int)(e.Brake * 100);

            ThrottleBar.Height = Math.Max(0, ThrottleBar.Parent is FrameworkElement parent
                ? parent.ActualHeight * e.Throttle : 0);
            BrakeBar.Height = Math.Max(0, BrakeBar.Parent is FrameworkElement brakeParent
                ? brakeParent.ActualHeight * e.Brake : 0);

            ThrottlePercent.Text = $"{throttlePct}%";
            BrakePercent.Text = $"{brakePct}%";

            // Update lap times
            if (e.BestLapTime > 0)
            {
                BestLapLabel.Text = FormatLapTime(e.BestLapTime);
                BestLapLabel.Foreground = PurpleBrush;
            }

            if (e.LastLapTime > 0)
            {
                LastLapLabel.Text = FormatLapTime(e.LastLapTime);
                // Purple if last lap matches best lap
                bool isBest = e.BestLapTime > 0 && Math.Abs(e.LastLapTime - e.BestLapTime) < 0.001f;
                LastLapLabel.Foreground = isBest ? PurpleBrush : DefaultLapBrush;
            }

            // Update session info (title bar)
            if (e.Sof > 0)
                SofLabel.Text = $"SOF {e.Sof}";
            else
                SofLabel.Text = "SOF —";

            if (e.SessionTimeRemain > 0 && e.SessionTimeRemain < 604800) // < 7 days means valid
            {
                var tr = TimeSpan.FromSeconds(e.SessionTimeRemain);
                TimeRemainLabel.Text = tr.TotalHours >= 1
                    ? $"{(int)tr.TotalHours}:{tr.Minutes:D2}:{tr.Seconds:D2}"
                    : $"{tr.Minutes:D2}:{tr.Seconds:D2}";
            }
            else
            {
                TimeRemainLabel.Text = "";
            }

            string maxInc = e.MaxIncidents < 0 ? "-" : e.MaxIncidents.ToString();
            IncidentsLabel.Text = $"x {e.Incidents}/{maxInc}";

            // Update delta
            if (e.DeltaValid)
            {
                string sign = e.DeltaToBest >= 0 ? "+" : "";
                DeltaLabel.Text = $"{sign}{e.DeltaToBest:F3}";
                DeltaLabel.Foreground = e.DeltaToBest <= 0 ? GreenBrush : RedBrush;
                DeltaBorder.Background = e.DeltaToBest <= 0 ? DarkGreenBrush : DarkRedBrush;
            }
            else
            {
                DeltaLabel.Text = "—";
                DeltaLabel.Foreground = DimBrush;
                DeltaBorder.Background = NeutralBgBrush;
            }
        });
    }

    private void OnStateChanged(object? sender, ConnectionState state)
    {
        Dispatcher.InvokeAsync(() =>
        {
            switch (state)
            {
                case ConnectionState.Disconnected:
                    StatusDot.Fill = RedBrush;
                    StatusText.Text = "  Disconnected";
                    break;
                case ConnectionState.InMenu:
                    StatusDot.Fill = OrangeBrush;
                    StatusText.Text = "  In Menu";
                    break;
                case ConnectionState.InPit:
                    StatusDot.Fill = OrangeBrush;
                    StatusText.Text = "  In Pit / Menu";
                    break;
                case ConnectionState.OnTrack:
                    StatusDot.Fill = GreenBrush;
                    StatusText.Text = "  On Track";
                    break;
            }
        });
    }

    private static string FormatLapTime(float seconds)
    {
        if (seconds <= 0) return "—";
        var ts = TimeSpan.FromSeconds(seconds);
        return ts.Minutes > 0
            ? $"{ts.Minutes}:{ts.Seconds:D2}.{ts.Milliseconds:D3}"
            : $"{ts.Seconds}.{ts.Milliseconds:D3}";
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => Hide();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Copy_Click(object sender, RoutedEventArgs e) { /* Future: copy telemetry data */ }
    private void Settings_Click(object sender, RoutedEventArgs e) => SettingsPopup.IsOpen = !SettingsPopup.IsOpen;

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Opacity = e.NewValue;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _iracing.Dispose();
    }
}
