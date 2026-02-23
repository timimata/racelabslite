using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PedalTelemetry.Services;

namespace PedalTelemetry;

public partial class MainWindow : Window
{
    private readonly IRacingService _iracing;

    private static readonly SolidColorBrush GreenBrush = new(Color.FromRgb(0x00, 0xC8, 0x00));
    private static readonly SolidColorBrush OrangeBrush = new(Color.FromRgb(0xFF, 0xAA, 0x00));
    private static readonly SolidColorBrush RedBrush = new(Color.FromRgb(0xE0, 0x20, 0x20));
    private static readonly SolidColorBrush DimBrush = new(Color.FromRgb(0x88, 0x88, 0x88));

    static MainWindow()
    {
        GreenBrush.Freeze();
        OrangeBrush.Freeze();
        RedBrush.Freeze();
        DimBrush.Freeze();
    }

    public MainWindow()
    {
        InitializeComponent();

        _iracing = new IRacingService();
        _iracing.TelemetryUpdated += OnTelemetryUpdated;
        _iracing.StateChanged += OnStateChanged;
        _iracing.Start();
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
            if (e.LastLapTime > 0)
                LastLapLabel.Text = FormatLapTime(e.LastLapTime);

            if (e.BestLapTime > 0)
                BestLapLabel.Text = FormatLapTime(e.BestLapTime);

            // Update delta
            if (e.DeltaValid)
            {
                string sign = e.DeltaToBest >= 0 ? "+" : "";
                DeltaLabel.Text = $"{sign}{e.DeltaToBest:F3}";
                DeltaLabel.Foreground = e.DeltaToBest <= 0 ? GreenBrush : RedBrush;
            }
            else
            {
                DeltaLabel.Text = "—";
                DeltaLabel.Foreground = DimBrush;
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
                    TitleDot.Fill = RedBrush;
                    break;
                case ConnectionState.InMenu:
                    StatusDot.Fill = OrangeBrush;
                    StatusText.Text = "  In Menu";
                    TitleDot.Fill = OrangeBrush;
                    break;
                case ConnectionState.InPit:
                    StatusDot.Fill = OrangeBrush;
                    StatusText.Text = "  In Pit / Menu";
                    TitleDot.Fill = OrangeBrush;
                    break;
                case ConnectionState.OnTrack:
                    StatusDot.Fill = GreenBrush;
                    StatusText.Text = "  On Track";
                    TitleDot.Fill = GreenBrush;
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

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Settings_Click(object sender, RoutedEventArgs e) { /* Future: settings popup */ }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Opacity = e.NewValue;
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _iracing.Dispose();
    }
}
