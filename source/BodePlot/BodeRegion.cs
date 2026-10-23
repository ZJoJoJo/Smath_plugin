using System;
using System.Drawing;
using SMath.Controls;
using SMath.Manager;
using SMath.Math;

namespace EngineeringTools
{
    public sealed class BodeRegion : RegionEvaluable
    {
        public BodeSettings Settings = new BodeSettings();
        private BodeData data;
        private string error;
        private double? cursor;
        internal BodeData PlotData { get { return data; } }
        internal string PlotError { get { return error; } }
        internal double? PlotCursor { get { return cursor; } }
        internal Size PlotSize { get { return new Size(Math.Max(1, Size.Width - 8), Math.Max(1, Size.Height - 8)); } }

        public BodeRegion(SessionProfile profile) : base(profile)
        {
            Size = new Size(600, 460);
            Evaluable = true;
            Terms = new[] { new Term("0", TermType.Operand, 0) };
        }
        public BodeRegion(BodeRegion original) : base(original)
        {
            Settings = original.Settings.Clone();
            data = original.data;
            error = original.error;
            Size = original.Size;
        }
        public override RegionBase Clone() { return new BodeRegion(this); }
        public override HistoryStepBase GetHistoryStep() { return new BodeHistoryStep(this); }
        public override bool IsEmpty() { return false; }
        public override bool CheckEvaluability() { return true; }
        public override void OnEvaluation(Store store)
        {
            data = null; error = null;
            try { data = BodeData.Sample(Settings, (spec, omega) => SMathResponse.Evaluate(store, spec, omega)); }
            catch (Exception exception) { error = exception.Message; }
            InDate = true;
            Invalidate();
        }
        public override void OnPaint(PaintEventOptions options)
        {
            options.Graphics.Save();
            try
            {
                options.Graphics.Translate(options.ClipRectangle.Left, options.ClipRectangle.Top);
                using (IPlotCanvas canvas = new SMathPlotCanvas(options.Graphics)) PlotRenderer.Draw(canvas, PlotSize, Settings, data, cursor, error);
            }
            finally { options.Graphics.Restore(); }
        }
        public override void OnMouseMove(MouseEventOptions options)
        {
            base.OnMouseMove(options);
            PlotLayout layout = new PlotLayout(PlotSize, Settings);
            PointF location = options.Location;
            if (layout.Magnitude.Contains(location) || layout.Phase.Contains(location)) cursor = layout.Frequency(location.X, Settings);
            else cursor = null;
            Invalidate();
        }
        public override void OnMouseDown(MouseEventOptions options)
        {
            base.OnMouseDown(options);
            if (data != null)
            {
                int index = new PlotLayout(PlotSize, Settings).HitLegend(options.Location, data.Series.Count);
                if (index >= 0)
                {
                    if (options.Button == MouseInputButtons.Left && options.Clicks == 1)
                    {
                        string label = data.Series[index].Spec.Label;
                        if (Parent == null) Settings.SetSeriesVisible(label, !Settings.IsSeriesVisible(label));
                        else using (Parent.History.CreateTransaction(false))
                        {
                            Settings.SetSeriesVisible(label, !Settings.IsSeriesVisible(label));
                            CreateHistoryStep("切换波特图曲线显示");
                        }
                        Invalidate();
                    }
                    return;
                }
            }
            if (options.Clicks >= 2) Configure();
        }
        public void Configure()
        {
            using (SettingsForm dialog = new SettingsForm(Settings, Size))
            {
                if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
                using (Parent.History.CreateTransaction(false))
                {
                    Settings = dialog.Result; SetPlotSize(dialog.ResultSize); cursor = null;
                    CreateHistoryStep("波特图设置与尺寸");
                }
                RequestEvaluation(true);
                Invalidate();
            }
        }
        public void SetPlotSize(Size size)
        {
            int minimumHeight = PlotLayout.MinimumHeight(size.Width - 8, Settings);
            if (size.Width < 400 || size.Width > 2000 || size.Height < minimumHeight + 8 || size.Height > 1600)
                throw new ArgumentException("波特图宽度应为 400–2000，高度至少为 " + (minimumHeight + 8) + "，以保留坐标和读数空间。");
            Size = size;
            Invalidate();
        }
        public override void ToXml(StorageWriter writer, FileParsingContext context)
        {
            PropertiesWriter properties = writer.Properties;
            properties.SetString("series", Settings.Series);
            properties.SetString("hiddenSeries", Settings.HiddenSeries);
            properties.SetDouble("minimum", Settings.Minimum); properties.SetDouble("maximum", Settings.Maximum);
            properties.SetInt32("samples", Settings.Samples);
            properties.SetBoolean("logFrequency", Settings.LogFrequency); properties.SetBoolean("decibels", Settings.Decibels);
            properties.SetBoolean("radiansFrequency", Settings.RadiansFrequency); properties.SetBoolean("radiansPhase", Settings.RadiansPhase);
            properties.SetBoolean("unwrapPhase", Settings.UnwrapPhase);
            properties.SetBoolean("autoMagnitude", Settings.AutoMagnitude); properties.SetBoolean("autoPhase", Settings.AutoPhase);
            properties.SetDouble("magnitudeMinimum", Settings.MagnitudeMinimum); properties.SetDouble("magnitudeMaximum", Settings.MagnitudeMaximum);
            properties.SetDouble("phaseMinimum", Settings.PhaseMinimum); properties.SetDouble("phaseMaximum", Settings.PhaseMaximum);
            properties.SetInt32("plotWidth", Size.Width); properties.SetInt32("plotHeight", Size.Height);
        }
        public override void FromXml(StorageReader reader, FileParsingContext context)
        {
            PropertiesReader properties = reader.Properties;
            Settings = new BodeSettings
            {
                Series = properties.GetString("series", "H.0;H.1(s);H.2(s);T.OL(s)"),
                HiddenSeries = properties.GetString("hiddenSeries", ""),
                Minimum = properties.GetDouble("minimum", 10), Maximum = properties.GetDouble("maximum", 1000000), Samples = properties.GetInt32("samples", 501),
                LogFrequency = properties.GetBoolean("logFrequency", true), Decibels = properties.GetBoolean("decibels", true),
                RadiansFrequency = properties.GetBoolean("radiansFrequency", false), RadiansPhase = properties.GetBoolean("radiansPhase", false), UnwrapPhase = properties.GetBoolean("unwrapPhase", true),
                AutoMagnitude = properties.GetBoolean("autoMagnitude", true), AutoPhase = properties.GetBoolean("autoPhase", true),
                MagnitudeMinimum = properties.GetDouble("magnitudeMinimum", -80), MagnitudeMaximum = properties.GetDouble("magnitudeMaximum", 80),
                PhaseMinimum = properties.GetDouble("phaseMinimum", -270), PhaseMaximum = properties.GetDouble("phaseMaximum", 90)
            };
            int width = Math.Max(400, Math.Min(2000, properties.GetInt32("plotWidth", 600)));
            int height = Math.Max(PlotLayout.MinimumHeight(width - 8, Settings) + 8, Math.Min(1600, properties.GetInt32("plotHeight", 460)));
            Size = new Size(width, height);
            data = null; cursor = null;
        }
    }
}
