using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using SMath.Drawing;
using Graphics = System.Drawing.Graphics;

namespace EngineeringTools
{
    public interface IPlotCanvas : IDisposable
    {
        void Line(Color color, float width, bool dashed, float startX, float startY, float endX, float endY);
        void Text(string text, Color color, float left, float top, float width);
        void TextRight(string text, Color color, float left, float top, float width);
        void Fill(Color color, RectangleF rectangle);
        void Marker(Color color, float centerX, float centerY);
        void Clip(RectangleF rectangle);
        void Restore();
    }

    public sealed class GdiPlotCanvas : IPlotCanvas
    {
        private readonly Graphics graphics;
        private readonly Font font = new Font("Microsoft YaHei UI", 10);
        private GraphicsState clipState;
        public GdiPlotCanvas(Graphics graphics) { this.graphics = graphics; graphics.SmoothingMode = SmoothingMode.AntiAlias; }
        public void Line(Color color, float width, bool dashed, float startX, float startY, float endX, float endY)
        {
            using (Pen pen = new Pen(color, width))
            {
                if (dashed) pen.DashStyle = DashStyle.Dash;
                graphics.DrawLine(pen, startX, startY, endX, endY);
            }
        }
        public void Text(string text, Color color, float left, float top, float width)
        {
            using (Brush brush = new SolidBrush(color))
            using (StringFormat format = new StringFormat { FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                graphics.DrawString(text, font, brush, new RectangleF(left, top, width, 25), format);
        }
        public void TextRight(string text, Color color, float left, float top, float width)
        {
            float measured = graphics.MeasureString(text, font).Width;
            using (Font fitted = new Font(font.FontFamily, font.Size * Math.Min(1, width / measured)))
            using (Brush brush = new SolidBrush(color))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Far, FormatFlags = StringFormatFlags.NoWrap })
                graphics.DrawString(text, fitted, brush, new RectangleF(left, top, width, 25), format);
        }
        public void Fill(Color color, RectangleF rectangle) { using (Brush brush = new SolidBrush(color)) graphics.FillRectangle(brush, rectangle); }
        public void Marker(Color color, float centerX, float centerY) { using (Brush brush = new SolidBrush(color)) graphics.FillEllipse(brush, centerX - 3, centerY - 3, 6, 6); }
        public void Clip(RectangleF rectangle) { clipState = graphics.Save(); graphics.SetClip(rectangle); }
        public void Restore() { graphics.Restore(clipState); }
        public void Dispose() { font.Dispose(); }
    }

    public sealed class SMathPlotCanvas : IPlotCanvas
    {
        private readonly IGraphics graphics;
        private readonly FontInfo font = new FontInfo("Microsoft YaHei UI", 10, (FontfaceStyle)0);
        public SMathPlotCanvas(IGraphics graphics) { this.graphics = graphics; }
        public void Line(Color color, float width, bool dashed, float startX, float startY, float endX, float endY)
        {
            using (ColorPen pen = new ColorPen(color, width))
            {
                if (dashed)
                {
                    const float dashLength = 5;
                    double length = Math.Sqrt(Math.Pow(endX - startX, 2) + Math.Pow(endY - startY, 2));
                    if (length == 0) return;
                    for (float offset = 0; offset < length; offset += 2 * dashLength)
                    {
                        float end = (float)Math.Min(offset + dashLength, length);
                        graphics.DrawLine(pen, startX + (endX - startX) * offset / (float)length, startY + (endY - startY) * offset / (float)length,
                            startX + (endX - startX) * end / (float)length, startY + (endY - startY) * end / (float)length);
                    }
                }
                else graphics.DrawLine(pen, startX, startY, endX, endY);
            }
        }
        public void Text(string text, Color color, float left, float top, float width) { graphics.DrawString(text, font, new ColorBrush(color), left, top, width, StringOptions.GenericDefault); }
        public void TextRight(string text, Color color, float left, float top, float width)
        {
            float measured = graphics.MeasureString(text, font, 65001, StringOptions.GenericDefault).Width;
            FontInfo fitted = new FontInfo(font.Name, font.Size * Math.Min(1, width / measured), (FontfaceStyle)0);
            float textWidth = graphics.MeasureString(text, fitted, 65001, StringOptions.GenericDefault).Width;
            graphics.DrawString(text, fitted, new ColorBrush(color), left + width - textWidth, top, 65001, StringOptions.GenericDefault);
        }
        public void Fill(Color color, RectangleF rectangle) { graphics.FillRectangle(new ColorBrush(color), rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height); }
        public void Marker(Color color, float centerX, float centerY) { graphics.FillEllipse(new ColorBrush(color), centerX - 3, centerY - 3, 6, 6); }
        public void Clip(RectangleF rectangle) { graphics.SaveAndClip(rectangle); }
        public void Restore() { graphics.Restore(); }
        public void Dispose() { }
    }

    public sealed class PlotLayout
    {
        public readonly RectangleF Magnitude;
        public readonly RectangleF Phase;
        public readonly RectangleF Readout;
        public readonly float FrequencyTitleTop;
        public readonly int LegendColumns;
        public readonly int ReadoutColumns;
        private readonly float legendCellWidth;
        public static int MinimumHeight(int width, BodeSettings settings)
        {
            var specs = settings.Specs();
            return MinimumHeight(width, specs.Count, specs.Count == 0 ? 8 : specs.Max(spec => spec.Label.Length));
        }
        public static int MinimumHeight(int width, int seriesCount, int maxLabelLength = 8)
        {
            int legends = LegendColumnCount(width, seriesCount, maxLabelLength);
            int rows = Math.Max(1, (seriesCount + legends - 1) / legends);
            int columns = width >= 540 ? 2 : 1;
            int readoutRows = Math.Max(1, (seriesCount + columns - 1) / columns);
            return (int)(Math.Ceiling((72 + rows * 24 + 36 + 28 + readoutRows * 24 + 68 + 90) / 10d) * 10);
        }
        public PlotLayout(Size size) : this(size, 4) { }
        private static int LegendCellWidth(int maxLabelLength) { return Math.Max(92, 42 + maxLabelLength * 8); }
        private static int LegendColumnCount(int width, int count, int maxLabelLength)
        {
            return Math.Max(1, Math.Min(count, (width - 24) / LegendCellWidth(maxLabelLength)));
        }
        public PlotLayout(Size size, BodeSettings settings) : this(size, settings.Specs().Count, settings.Specs().Select(spec => spec.Label.Length).DefaultIfEmpty(8).Max()) { }
        public PlotLayout(Size size, int seriesCount, int maxLabelLength = 8)
        {
            LegendColumns = LegendColumnCount(size.Width, seriesCount, maxLabelLength);
            legendCellWidth = Math.Min(size.Width - 24, LegendCellWidth(maxLabelLength));
            int legendRows = Math.Max(1, (seriesCount + LegendColumns - 1) / LegendColumns);
            ReadoutColumns = size.Width >= 540 ? 2 : 1;
            int readoutRows = Math.Max(1, (seriesCount + ReadoutColumns - 1) / ReadoutColumns);
            float top = 72 + legendRows * 24;
            float readoutHeight = 28 + readoutRows * 24;
            const float gap = 36;
            float height = Math.Max(20, (size.Height - top - gap - readoutHeight - 68) / 2f);
            Magnitude = new RectangleF(64, top, Math.Max(80, size.Width - 84), height);
            Phase = new RectangleF(64, Magnitude.Bottom + gap, Magnitude.Width, height);
            Readout = new RectangleF(12, size.Height - readoutHeight - 8, size.Width - 24, readoutHeight);
            FrequencyTitleTop = Readout.Top - 28;
        }
        public RectangleF LegendItem(int index)
        {
            return new RectangleF(12 + index % LegendColumns * legendCellWidth, 42 + index / LegendColumns * 24, legendCellWidth, 24);
        }
        public RectangleF LegendCheckbox(int index)
        {
            RectangleF item = LegendItem(index);
            return new RectangleF(item.Left, item.Top + 3, 14, 14);
        }
        public int HitLegend(PointF location, int count)
        {
            for (int index = 0; index < count; index++) if (LegendItem(index).Contains(location)) return index;
            return -1;
        }
        public double Frequency(float position, BodeSettings settings)
        {
            double ratio = Math.Max(0, Math.Min(1, (position - Magnitude.Left) / Magnitude.Width));
            return settings.LogFrequency ? settings.Minimum * Math.Pow(settings.Maximum / settings.Minimum, ratio) : settings.Minimum + (settings.Maximum - settings.Minimum) * ratio;
        }
    }

    public static class PlotRenderer
    {
        public static readonly Color[] Colors = { Color.FromArgb(33, 106, 190), Color.FromArgb(208, 84, 48), Color.FromArgb(30, 139, 93), Color.FromArgb(141, 77, 177), Color.FromArgb(160, 117, 20), Color.FromArgb(0, 137, 155), Color.FromArgb(184, 64, 126), Color.FromArgb(84, 98, 113) };
        private static readonly Color Foreground = Color.FromArgb(35, 43, 53);
        private static readonly Color Grid = Color.FromArgb(224, 229, 235);
        private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

        public static string Number(double value) { return value.ToString("G4", Invariant); }
        public static string FrequencyLabel(double value)
        {
            if (value >= 1000000) return Number(value / 1000000) + "M";
            if (value >= 1000) return Number(value / 1000) + "k";
            return Number(value);
        }
        public static double X(double frequency, RectangleF box, BodeSettings settings)
        {
            double ratio = settings.LogFrequency ? Math.Log(frequency / settings.Minimum) / Math.Log(settings.Maximum / settings.Minimum) : (frequency - settings.Minimum) / (settings.Maximum - settings.Minimum);
            return box.Left + ratio * box.Width;
        }
        private static float Y(double value, RectangleF box, double minimum, double maximum) { return (float)(box.Bottom - (value - minimum) / (maximum - minimum) * box.Height); }

        public static double[] Bounds(IEnumerable<double> values, bool automatic, double minimum, double maximum)
        {
            if (!automatic) return new[] { minimum, maximum };
            double[] finite = values.Where(BodeSettings.Finite).ToArray();
            if (finite.Length == 0) return new[] { -1d, 1d };
            minimum = finite.Min(); maximum = finite.Max();
            double padding = Math.Max(1e-9, Math.Max((maximum - minimum) * .09, Math.Max(Math.Abs(minimum), Math.Abs(maximum)) * .025));
            if (maximum == minimum && maximum == 0) padding = 1;
            return new[] { minimum - padding, maximum + padding };
        }

        private static IEnumerable<double> Ticks(double minimum, double maximum)
        {
            double approximate = (maximum - minimum) / 4;
            double scale = Math.Pow(10, Math.Floor(Math.Log10(approximate)));
            double step = scale * (approximate / scale <= 1 ? 1 : approximate / scale <= 2 ? 2 : approximate / scale <= 5 ? 5 : 10);
            for (double tick = Math.Ceiling(minimum / step) * step; tick <= maximum + step * 1e-8; tick += step) yield return Math.Abs(tick) < step * 1e-8 ? 0 : tick;
        }

        private static void Axes(IPlotCanvas canvas, RectangleF box, BodeSettings settings, double[] bounds, string label, bool frequencyLabels)
        {
            canvas.Text(label, Foreground, box.Left, box.Top - 28, 190);
            foreach (double value in Ticks(bounds[0], bounds[1]))
            {
                float position = Y(value, box, bounds[0], bounds[1]);
                canvas.Line(Grid, 1, false, box.Left, position, box.Right, position);
                canvas.TextRight(Number(value), Foreground, 4, position - 10, box.Left - 12);
            }
            IEnumerable<double> frequencies = settings.LogFrequency
                ? Enumerable.Range((int)Math.Ceiling(Math.Log10(settings.Minimum)), Math.Max(0, (int)Math.Floor(Math.Log10(settings.Maximum)) - (int)Math.Ceiling(Math.Log10(settings.Minimum)) + 1)).Select(exponent => Math.Pow(10, exponent))
                : Ticks(settings.Minimum, settings.Maximum);
            double[] frequencyArray = frequencies.ToArray();
            int stride = Math.Max(1, (int)Math.Ceiling(frequencyArray.Length / Math.Max(2, box.Width / 80)));
            for (int index = 0; index < frequencyArray.Length; index++)
            {
                float position = (float)X(frequencyArray[index], box, settings);
                canvas.Line(Grid, 1, false, position, box.Top, position, box.Bottom);
                if (frequencyLabels && index % stride == 0)
                    canvas.Text(FrequencyLabel(frequencyArray[index]), Foreground, Math.Min(position - 16, box.Right - 46), box.Bottom + 9, 68);
            }
            canvas.Line(Color.Gray, 1, false, box.Left, box.Top, box.Right, box.Top);
            canvas.Line(Color.Gray, 1, false, box.Left, box.Bottom, box.Right, box.Bottom);
            canvas.Line(Color.Gray, 1, false, box.Left, box.Top, box.Left, box.Bottom);
            canvas.Line(Color.Gray, 1, false, box.Right, box.Top, box.Right, box.Bottom);
        }

        public static void Draw(IPlotCanvas canvas, Size size, BodeSettings settings, BodeData data, double? cursor, string error)
        {
            canvas.Fill(Color.White, new RectangleF(0, 0, size.Width, size.Height));
            canvas.Fill(Color.FromArgb(245, 248, 252), new RectangleF(0, 0, size.Width, 34));
            canvas.Text("波特图  |  s = jω", Foreground, 14, 7, 280);
            if (data == null)
            {
                canvas.Text(string.IsNullOrEmpty(error) ? "等待计算；双击设置曲线和坐标轴。" : error, Color.Firebrick, 14, 52, size.Width - 28);
                return;
            }
            PlotLayout layout = new PlotLayout(size, settings);
            int[] visible = Enumerable.Range(0, data.Series.Count).Where(index => settings.IsSeriesVisible(data.Series[index].Spec.Label)).ToArray();
            for (int index = 0; index < data.Series.Count; index++)
            {
                RectangleF item = layout.LegendItem(index);
                RectangleF check = layout.LegendCheckbox(index);
                bool selected = settings.IsSeriesVisible(data.Series[index].Spec.Label);
                Color border = selected ? Colors[0] : Color.Gray;
                canvas.Fill(selected ? border : Color.White, check);
                canvas.Line(border, 1, false, check.Left, check.Top, check.Right, check.Top);
                canvas.Line(border, 1, false, check.Right, check.Top, check.Right, check.Bottom);
                canvas.Line(border, 1, false, check.Right, check.Bottom, check.Left, check.Bottom);
                canvas.Line(border, 1, false, check.Left, check.Bottom, check.Left, check.Top);
                if (selected)
                {
                    canvas.Line(Color.White, 1.6f, false, check.Left + 3, check.Top + 7, check.Left + 6, check.Top + 10);
                    canvas.Line(Color.White, 1.6f, false, check.Left + 6, check.Top + 10, check.Left + 11, check.Top + 4);
                }
                canvas.Line(Colors[index % Colors.Length], 2.5f, false, item.Left + 20, item.Top + 10, item.Left + 32, item.Top + 10);
                canvas.Text(data.Series[index].Spec.Label, selected ? Foreground : Color.Gray, item.Left + 38, item.Top, item.Width - 42);
            }
            double[] magnitudeBounds = Bounds(visible.SelectMany(index => data.Series[index].Magnitudes), settings.AutoMagnitude, settings.MagnitudeMinimum, settings.MagnitudeMaximum);
            double[] phaseBounds = Bounds(visible.SelectMany(index => data.Series[index].Phases), settings.AutoPhase, settings.PhaseMinimum, settings.PhaseMaximum);
            Axes(canvas, layout.Magnitude, settings, magnitudeBounds, settings.Decibels ? "幅值 / dB" : "幅值 / 倍率", false);
            Axes(canvas, layout.Phase, settings, phaseBounds, settings.RadiansPhase ? "相位 / rad" : "相位 / °", true);
            canvas.Text((settings.RadiansFrequency ? "角频率 ω / rad/s" : "频率 f / Hz") + (settings.LogFrequency ? " · 对数刻度" : " · 线性刻度"), Foreground, layout.Phase.Left, layout.FrequencyTitleTop, size.Width - layout.Phase.Left);
            for (int panel = 0; panel < 2; panel++)
            {
                RectangleF box = panel == 0 ? layout.Magnitude : layout.Phase;
                double[] bounds = panel == 0 ? magnitudeBounds : phaseBounds;
                canvas.Clip(box);
                foreach (int seriesIndex in visible)
                {
                    double[] values = panel == 0 ? data.Series[seriesIndex].Magnitudes : data.Series[seriesIndex].Phases;
                    PointF? previous = null;
                    for (int index = 0; index < values.Length; index++)
                    {
                        if (!BodeSettings.Finite(values[index])) { previous = null; continue; }
                        float ordinate = Y(values[index], box, bounds[0], bounds[1]);
                        if (!BodeSettings.Finite(ordinate) || Math.Abs(ordinate) > 1000000) { previous = null; continue; }
                        PointF point = new PointF((float)X(data.Frequencies[index], box, settings), ordinate);
                        if (previous.HasValue) canvas.Line(Colors[seriesIndex % Colors.Length], 1.8f, false, previous.Value.X, previous.Value.Y, point.X, point.Y);
                        previous = point;
                    }
                }
                if (cursor.HasValue && visible.Length > 0)
                {
                    float horizontal = (float)X(cursor.Value, box, settings);
                    canvas.Line(Color.DimGray, 1, true, horizontal, box.Top, horizontal, box.Bottom);
                    foreach (int seriesIndex in visible)
                    {
                        double[] values = panel == 0 ? data.Series[seriesIndex].Magnitudes : data.Series[seriesIndex].Phases;
                        double value = BodeData.Interpolate(data.Frequencies, values, cursor.Value, settings.LogFrequency);
                        if (!BodeSettings.Finite(value)) continue;
                        float vertical = Y(value, box, bounds[0], bounds[1]);
                        if (vertical < box.Top || vertical > box.Bottom) continue;
                        canvas.Line(Color.FromArgb(180, Colors[seriesIndex % Colors.Length]), 1, true, box.Left, vertical, horizontal, vertical);
                        canvas.Marker(Colors[seriesIndex % Colors.Length], horizontal, vertical);
                    }
                }
                canvas.Restore();
            }
            if (cursor.HasValue && visible.Length > 0)
            {
                canvas.Fill(Color.FromArgb(245, 248, 252), layout.Readout);
                canvas.Text((settings.RadiansFrequency ? "ω = " : "f = ") + Number(cursor.Value) + (settings.RadiansFrequency ? " rad/s" : " Hz"), Foreground, layout.Readout.Left + 8, layout.Readout.Top + 3, layout.Readout.Width - 16);
                float cellWidth = (layout.Readout.Width - 16) / layout.ReadoutColumns;
                for (int position = 0; position < visible.Length; position++)
                {
                    int index = visible[position];
                    BodeSeries series = data.Series[index];
                    double magnitude = BodeData.Interpolate(data.Frequencies, series.Magnitudes, cursor.Value, settings.LogFrequency);
                    double phase = BodeData.Interpolate(data.Frequencies, series.Phases, cursor.Value, settings.LogFrequency);
                    string detail = series.Spec.Label + "  " + Number(magnitude) + (settings.Decibels ? " dB" : " ×") + "  |  " + Number(phase) + (settings.RadiansPhase ? " rad" : "°");
                    float left = layout.Readout.Left + 8 + position % layout.ReadoutColumns * cellWidth;
                    float top = layout.Readout.Top + 28 + position / layout.ReadoutColumns * 24;
                    canvas.Clip(new RectangleF(left, top, cellWidth - 6, 24));
                    canvas.Text(detail, Colors[index % Colors.Length], left, top, cellWidth - 6);
                    canvas.Restore();
                }
            }
        }
    }
}
