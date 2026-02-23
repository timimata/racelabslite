using System;
using System.Windows;
using System.Windows.Media;

namespace PedalTelemetry;

public class TelemetryChart : FrameworkElement
{
    private const int BufferSize = 300; // ~5 seconds at 60Hz

    private readonly float[] _throttleBuffer = new float[BufferSize];
    private readonly float[] _brakeBuffer = new float[BufferSize];
    private readonly bool[] _absBuffer = new bool[BufferSize];
    private int _writeIndex;
    private int _count;

    private static readonly Pen GridPen = new(new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)), 1);
    private static readonly Pen ThrottlePen = new(new SolidColorBrush(Color.FromRgb(0x00, 0xC8, 0x00)), 1.5);
    private static readonly Pen BrakePen = new(new SolidColorBrush(Color.FromRgb(0xE0, 0x20, 0x20)), 1.5);
    private static readonly Pen AbsPen = new(new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00)), 2.0); // Laranja
    private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
    private static readonly Typeface LabelTypeface = new("Segoe UI");
    private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));

    static TelemetryChart()
    {
        GridPen.Freeze();
        ThrottlePen.Freeze();
        BrakePen.Freeze();
        AbsPen.Freeze();
        BackgroundBrush.Freeze();
        LabelBrush.Freeze();
    }

    public void AddDataPoint(float throttle, float brake)
    {
        AddDataPoint(throttle, brake, false);

    }

    public void AddDataPoint(float throttle, float brake, bool abs)
    {
        _throttleBuffer[_writeIndex] = throttle;
        _brakeBuffer[_writeIndex] = brake;
        _absBuffer[_writeIndex] = abs;
        _writeIndex = (_writeIndex + 1) % BufferSize;
        if (_count < BufferSize) _count++;
        InvalidateVisual();
    }

    public void Clear()
    {
        _count = 0;
        _writeIndex = 0;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0) return;

        // Background
        dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, w, h));

        // Horizontal grid lines at 25%, 50%, 75%, 100%
        double[] gridLevels = { 0.25, 0.50, 0.75, 1.0 };
        string[] gridLabels = { "25%", "50%", "75%", "100%" };
        for (int i = 0; i < gridLevels.Length; i++)
        {
            double y = h - (h * gridLevels[i]);
            dc.DrawLine(GridPen, new Point(0, y), new Point(w, y));

            var text = new FormattedText(
                gridLabels[i],
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                10,
                LabelBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            dc.DrawText(text, new Point(w - text.Width - 4, y - text.Height - 2));
        }

        if (_count < 2) return;

        // Draw traces
        DrawTrace(dc, _throttleBuffer, ThrottlePen, w, h);
        DrawTrace(dc, _brakeBuffer, BrakePen, w, h);
        DrawAbsTrace(dc, _brakeBuffer, _absBuffer, AbsPen, w, h);
    }

    private void DrawTrace(DrawingContext dc, float[] buffer, Pen pen, double w, double h)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            int startIndex = (_writeIndex - _count + BufferSize) % BufferSize;
            double xStep = w / (BufferSize - 1);

            float val = buffer[startIndex];
            ctx.BeginFigure(new Point(0, h - (h * val)), false, false);

            for (int i = 1; i < _count; i++)
            {
                int idx = (startIndex + i) % BufferSize;
                double x = i * xStep;
                double y = h - (h * buffer[idx]);
                ctx.LineTo(new Point(x, y), true, true);
            }
        }
        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }

    // Desenha segmentos laranja (ABS) sobrepostos à linha de freio
    private void DrawAbsTrace(DrawingContext dc, float[] brakeBuffer, bool[] absBuffer, Pen pen, double w, double h)
    {
        int startIndex = (_writeIndex - _count + BufferSize) % BufferSize;
        double xStep = w / (BufferSize - 1);
        StreamGeometry geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            bool inAbs = false;
            for (int i = 1; i < _count; i++)
            {
                int idxPrev = (startIndex + i - 1) % BufferSize;
                int idx = (startIndex + i) % BufferSize;
                double x0 = (i - 1) * xStep;
                double y0 = h - (h * brakeBuffer[idxPrev]);
                double x1 = i * xStep;
                double y1 = h - (h * brakeBuffer[idx]);
                if (absBuffer[idx])
                {
                    ctx.BeginFigure(new Point(x0, y0), false, false);
                    ctx.LineTo(new Point(x1, y1), true, true);
                }
            }
        }
        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }
}
