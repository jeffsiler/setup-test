using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TrimbleBatteryMonitor.Core.Models;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;

namespace TrimbleBatteryMonitor.Views;

public partial class BatteryChartControl : UserControl
{
    private static readonly System.Windows.Media.Brush[] SeriesBrushes =
    [
        new SolidColorBrush(Color.FromRgb(0, 99, 163)),
        new SolidColorBrush(Color.FromRgb(46, 125, 50)),
        new SolidColorBrush(Color.FromRgb(198, 40, 40)),
        new SolidColorBrush(Color.FromRgb(123, 31, 162)),
    ];

    public BatteryChartControl()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Redraw();
    }

    public void SetSamples(IReadOnlyList<BatterySample> samples)
    {
        _samples = samples;
        Redraw();
    }

    private IReadOnlyList<BatterySample> _samples = Array.Empty<BatterySample>();

    private void Redraw()
    {
        ChartCanvas.Children.Clear();

        var width = ChartCanvas.ActualWidth;
        var height = ChartCanvas.ActualHeight;
        if (width < 40 || height < 40 || _samples.Count == 0)
        {
            ChartCanvas.Children.Add(new TextBlock
            {
                Text = "Collecting battery data...",
                Foreground = Brushes.Gray,
            });
            Canvas.SetLeft(ChartCanvas.Children[0], 8);
            Canvas.SetTop(ChartCanvas.Children[0], 8);
            return;
        }

        const double leftPad = 36;
        const double rightPad = 12;
        const double topPad = 12;
        const double bottomPad = 24;
        var plotWidth = width - leftPad - rightPad;
        var plotHeight = height - topPad - bottomPad;

        DrawGridLines(plotHeight, leftPad, topPad, plotWidth);
        DrawYAxisLabels(plotHeight, topPad);

        var groups = _samples
            .GroupBy(s => s.DeviceId)
            .OrderBy(g => g.Key)
            .ToList();

        var minTime = _samples.Min(s => s.TimestampUtc);
        var maxTime = _samples.Max(s => s.TimestampUtc);
        if (maxTime <= minTime)
        {
            maxTime = minTime.AddMinutes(1);
        }

        var colorIndex = 0;
        foreach (var group in groups)
        {
            var ordered = group.OrderBy(s => s.TimestampUtc).ToList();
            if (ordered.Count < 2)
            {
                continue;
            }

            var brush = SeriesBrushes[colorIndex % SeriesBrushes.Length];
            colorIndex++;

            var polyline = new Polyline
            {
                Stroke = brush,
                StrokeThickness = 2,
                Fill = null,
            };

            foreach (var sample in ordered)
            {
                var xRatio = (sample.TimestampUtc - minTime).TotalSeconds /
                             (maxTime - minTime).TotalSeconds;
                var x = leftPad + xRatio * plotWidth;
                var y = topPad + plotHeight - (sample.ChargePercent / 100.0) * plotHeight;
                polyline.Points.Add(new Point(x, y));
            }

            ChartCanvas.Children.Add(polyline);

            var label = new TextBlock
            {
                Text = ordered[0].BatteryName,
                Foreground = brush,
                FontSize = 11,
            };
            ChartCanvas.Children.Add(label);
            Canvas.SetLeft(label, leftPad + colorIndex * 90);
            Canvas.SetTop(label, 2);
        }
    }

    private void DrawGridLines(double plotHeight, double leftPad, double topPad, double plotWidth)
    {
        foreach (var percent in new[] { 0, 25, 50, 75, 100 })
        {
            var y = topPad + plotHeight - (percent / 100.0) * plotHeight;
            var line = new Line
            {
                X1 = leftPad,
                X2 = leftPad + plotWidth,
                Y1 = y,
                Y2 = y,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1,
            };
            ChartCanvas.Children.Add(line);
        }
    }

    private void DrawYAxisLabels(double plotHeight, double topPad)
    {
        foreach (var percent in new[] { 0, 50, 100 })
        {
            var y = topPad + plotHeight - (percent / 100.0) * plotHeight;
            var label = new TextBlock
            {
                Text = $"{percent}%",
                FontSize = 10,
                Foreground = Brushes.Gray,
            };
            ChartCanvas.Children.Add(label);
            Canvas.SetLeft(label, 2);
            Canvas.SetTop(label, y - 8);
        }
    }
}
