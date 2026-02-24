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
            dc.DrawText(text, new Point(w - text.Width - 4, y + 2));
        }

        if (_count < 2) return;

        // Draw traces
        DrawTrace(dc, _throttleBuffer, ThrottlePen, w, h);
        DrawBrakeTrace(dc, w, h);
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

    // Desenha a linha de travão mudando de cor conforme o ABS: vermelho normal, laranja quando ABS ativo
    private void DrawBrakeTrace(DrawingContext dc, double w, double h)
    {
        int startIndex = (_writeIndex - _count + BufferSize) % BufferSize;
        double xStep = w / (BufferSize - 1);

        // Acumular segmentos por cor (vermelho vs laranja)
        StreamGeometry? currentGeometry = null;
        StreamGeometryContext? currentCtx = null;
        bool currentIsAbs = _absBuffer[startIndex];
        var geometries = new System.Collections.Generic.List<(StreamGeometry geo, Pen pen)>();

        void FlushGeometry()
        {
            if (currentCtx != null && currentGeometry != null)
            {
                ((IDisposable)currentCtx).Dispose();
                currentGeometry.Freeze();
                geometries.Add((currentGeometry, currentIsAbs ? AbsPen : BrakePen));
            }
        }

        for (int i = 0; i < _count; i++)
        {
            int idx = (startIndex + i) % BufferSize;
            double x = i * xStep;
            double y = h - (h * _brakeBuffer[idx]);
            bool isAbs = _absBuffer[idx];

            if (i == 0)
            {
                currentIsAbs = isAbs;
                currentGeometry = new StreamGeometry();
                currentCtx = currentGeometry.Open();
                currentCtx.BeginFigure(new Point(x, y), false, false);
            }
            else if (isAbs != currentIsAbs)
            {
                // Mudança de estado: continuar até este ponto, depois trocar
                currentCtx!.LineTo(new Point(x, y), true, true);
                FlushGeometry();

                // Começar novo segmento com nova cor
                currentIsAbs = isAbs;
                currentGeometry = new StreamGeometry();
                currentCtx = currentGeometry.Open();
                currentCtx.BeginFigure(new Point(x, y), false, false);
            }
            else
            {
                currentCtx!.LineTo(new Point(x, y), true, true);
            }
        }

        FlushGeometry();

        foreach (var (geo, pen) in geometries)
            dc.DrawGeometry(null, pen, geo);
    }
}
