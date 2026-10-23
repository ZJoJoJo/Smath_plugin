using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace EngineeringTools
{
    public sealed class SettingsForm : Form
    {
        public BodeSettings Result { get; private set; }
        public Size ResultSize { get; private set; }
        private readonly string hiddenSeries;
        private readonly NumericUpDown plotWidth = new NumericUpDown { Name = "PlotWidth", Minimum = 400, Maximum = 2000, Increment = 20 };
        private readonly NumericUpDown plotHeight = new NumericUpDown { Name = "PlotHeight", Minimum = 360, Maximum = 1600, Increment = 20 };
        private readonly TextBox series = new TextBox();
        private readonly TextBox minimum = new TextBox();
        private readonly TextBox maximum = new TextBox();
        private readonly NumericUpDown samples = new NumericUpDown { Minimum = 51, Maximum = 2001, Increment = 50 };
        private readonly CheckBox logFrequency = new CheckBox { Text = "对数频率轴" };
        private readonly CheckBox decibels = new CheckBox { Text = "幅值使用 dB（20 log₁₀|H|）" };
        private readonly CheckBox radiansFrequency = new CheckBox { Text = "频率单位 rad/s（否则为 Hz）" };
        private readonly CheckBox radiansPhase = new CheckBox { Text = "相位单位 rad（否则为 °）" };
        private readonly CheckBox unwrapPhase = new CheckBox { Text = "展开相位，消除 ±180° 跳变" };
        private readonly CheckBox autoMagnitude = new CheckBox { Text = "幅值轴自动范围" };
        private readonly CheckBox autoPhase = new CheckBox { Text = "相位轴自动范围" };
        private readonly TextBox magnitudeMinimum = new TextBox();
        private readonly TextBox magnitudeMaximum = new TextBox();
        private readonly TextBox phaseMinimum = new TextBox();
        private readonly TextBox phaseMaximum = new TextBox();

        public SettingsForm(BodeSettings settings) : this(settings, new Size(600, 460)) { }
        public SettingsForm(BodeSettings settings, Size size)
        {
            hiddenSeries = settings.HiddenSeries;
            Text = "波特图设置";
            Font = new Font("Microsoft YaHei UI", 9);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(590, 660);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            TableLayoutPanel table = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 18 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(table);
            AddRow(table, "曲线（分号分隔）", series);
            AddRow(table, "图尺寸：宽 / 高", Pair(plotWidth, plotHeight));
            FlowLayoutPanel presets = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty };
            foreach (Size preset in new[] { new Size(480, 380), new Size(600, 460), new Size(720, 480) })
            {
                Size chosen = preset;
                string title = preset.Width == 480 ? "紧凑" : preset.Width == 600 ? "标准" : "宽幅";
                Button button = new Button { Name = "SizePreset" + preset.Width, Text = title, AutoSize = true, Margin = new Padding(0, 0, 6, 0) };
                button.Click += delegate { plotWidth.Value = chosen.Width; UpdateMinimumHeight(); plotHeight.Value = Math.Max(plotHeight.Minimum, chosen.Height); };
                presets.Controls.Add(button);
            }
            AddRow(table, "尺寸预设", presets);
            AddRow(table, "最小频率", minimum);
            AddRow(table, "最大频率", maximum);
            AddRow(table, "采样点数", samples);
            AddRow(table, "频率轴", logFrequency);
            AddRow(table, "频率单位", radiansFrequency);
            AddRow(table, "幅值", decibels);
            AddRow(table, "相位单位", radiansPhase);
            AddRow(table, "相位连续性", unwrapPhase);
            AddRow(table, "幅值范围", autoMagnitude);
            AddRow(table, "幅值 最小 / 最大", Pair(magnitudeMinimum, magnitudeMaximum));
            AddRow(table, "相位范围", autoPhase);
            AddRow(table, "相位 最小 / 最大", Pair(phaseMinimum, phaseMaximum));
            Label help = new Label { Text = "例：H.0;H.1(s);H.2(s);T.OL(s)\n名称中的点表示 SMath 下标；曲线使用当前区域之前的定义。", AutoSize = true, Dock = DockStyle.Fill };
            table.Controls.Add(help, 0, table.Controls.Count / 2);
            table.SetColumnSpan(help, 2);
            FlowLayoutPanel buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
            Button accept = new Button { Text = "确定", AutoSize = true };
            Button cancel = new Button { Text = "取消", AutoSize = true, DialogResult = DialogResult.Cancel };
            accept.Click += delegate { AcceptSettings(); };
            buttons.Controls.Add(accept);
            buttons.Controls.Add(cancel);
            table.Controls.Add(buttons, 0, 17);
            table.SetColumnSpan(buttons, 2);
            AcceptButton = accept;
            CancelButton = cancel;
            series.Text = settings.Series;
            plotWidth.Value = Math.Max(plotWidth.Minimum, Math.Min(plotWidth.Maximum, size.Width));
            UpdateMinimumHeight();
            plotHeight.Value = Math.Max(plotHeight.Minimum, Math.Min(plotHeight.Maximum, size.Height));
            ResultSize = new Size((int)plotWidth.Value, (int)plotHeight.Value);
            plotWidth.ValueChanged += delegate { UpdateMinimumHeight(); };
            series.TextChanged += delegate { UpdateMinimumHeight(); };
            minimum.Text = Format(settings.Minimum); maximum.Text = Format(settings.Maximum);
            samples.Value = settings.Samples;
            logFrequency.Checked = settings.LogFrequency; decibels.Checked = settings.Decibels;
            radiansFrequency.Checked = settings.RadiansFrequency; radiansPhase.Checked = settings.RadiansPhase;
            unwrapPhase.Checked = settings.UnwrapPhase; autoMagnitude.Checked = settings.AutoMagnitude; autoPhase.Checked = settings.AutoPhase;
            magnitudeMinimum.Text = Format(settings.MagnitudeMinimum); magnitudeMaximum.Text = Format(settings.MagnitudeMaximum);
            phaseMinimum.Text = Format(settings.PhaseMinimum); phaseMaximum.Text = Format(settings.PhaseMaximum);
            autoMagnitude.CheckedChanged += delegate { magnitudeMinimum.Enabled = magnitudeMaximum.Enabled = !autoMagnitude.Checked; };
            autoPhase.CheckedChanged += delegate { phaseMinimum.Enabled = phaseMaximum.Enabled = !autoPhase.Checked; };
            magnitudeMinimum.Enabled = magnitudeMaximum.Enabled = !autoMagnitude.Checked;
            phaseMinimum.Enabled = phaseMaximum.Enabled = !autoPhase.Checked;
        }

        private static string Format(double value) { return value.ToString("G10", CultureInfo.InvariantCulture); }
        private void UpdateMinimumHeight()
        {
            int height;
            try { height = PlotLayout.MinimumHeight((int)plotWidth.Value - 8, new BodeSettings { Series = series.Text }); }
            catch (ArgumentException) { height = PlotLayout.MinimumHeight((int)plotWidth.Value - 8, 1); }
            plotHeight.Minimum = (decimal)(Math.Ceiling((height + 8) / 10d) * 10);
        }
        private static double Parse(TextBox box) { return double.Parse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture); }
        private static Control Pair(Control left, Control right)
        {
            TableLayoutPanel pair = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
            pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); pair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            left.Dock = right.Dock = DockStyle.Fill;
            pair.Controls.Add(left, 0, 0); pair.Controls.Add(right, 1, 0);
            return pair;
        }
        private static void AddRow(TableLayoutPanel table, string label, Control control)
        {
            int row = table.Controls.Count / 2;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 31));
            table.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
            control.Dock = DockStyle.Fill;
            table.Controls.Add(control, 1, row);
        }
        private void AcceptSettings()
        {
            try
            {
                BodeSettings settings = new BodeSettings
                {
                    Series = series.Text.Trim(), HiddenSeries = hiddenSeries, Minimum = Parse(minimum), Maximum = Parse(maximum), Samples = (int)samples.Value,
                    LogFrequency = logFrequency.Checked, Decibels = decibels.Checked, RadiansFrequency = radiansFrequency.Checked,
                    RadiansPhase = radiansPhase.Checked, UnwrapPhase = unwrapPhase.Checked, AutoMagnitude = autoMagnitude.Checked, AutoPhase = autoPhase.Checked,
                    MagnitudeMinimum = Parse(magnitudeMinimum), MagnitudeMaximum = Parse(magnitudeMaximum), PhaseMinimum = Parse(phaseMinimum), PhaseMaximum = Parse(phaseMaximum)
                };
                settings.Validate(); Result = settings;
                ResultSize = new Size((int)plotWidth.Value, (int)plotHeight.Value);
                DialogResult = DialogResult.OK; Close();
            }
            catch (Exception exception) { MessageBox.Show(this, exception.Message, "请检查设置", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
    }
}
