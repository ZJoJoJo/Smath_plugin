using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EngineeringTools
{
    public sealed class NotesEditor : Form
    {
        private readonly TextBox source = new TextBox {
            Name = "NoteSource", AccessibleName = "备注源文输入", Dock = DockStyle.Fill,
            Multiline = true, AcceptsReturn = true, AcceptsTab = true, WordWrap = true,
            ScrollBars = ScrollBars.Vertical, MaxLength = 200000, HideSelection = false,
            Font = new Font("Microsoft YaHei UI", 11), BackColor = Color.White,
            ForeColor = Color.FromArgb(25, 35, 45), ImeMode = ImeMode.NoControl, TabIndex = 0
        };
        private readonly PictureBox preview = new PictureBox { SizeMode = PictureBoxSizeMode.AutoSize };
        private readonly NumericUpDown width = new NumericUpDown { Minimum = 240, Maximum = 1000, Increment = 20, Width = 70 };
        private readonly NumericUpDown fontSize = new NumericUpDown { Minimum = 10, Maximum = 30, Width = 55 };
        private readonly Label status = new Label { AutoSize = true, Padding = new Padding(8) };
        private readonly Button save = new Button { Text = "保存备注", AutoSize = true, Enabled = false };
        private readonly Timer debounce = new Timer { Interval = 350 };
        private readonly NotesRenderer renderer = new NotesRenderer();
        private bool rendering;
        private bool dirty = true;
        private int editRevision;
        private bool closing;
        public NoteSnapshot Result { get; private set; }
        public NotesEditor(NoteSnapshot snapshot)
        {
            Text = "中文公式备注"; Font = new Font("Microsoft YaHei UI", 9); AutoScaleMode = AutoScaleMode.Dpi; ImeMode = ImeMode.NoControl;
            ClientSize = new Size(1120, 650); MinimumSize = new Size(850, 450); StartPosition = FormStartPosition.CenterParent;
            FlowLayoutPanel toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(6) };
            Button inline = new Button { Text = "行内公式 $…$", AutoSize = true };
            Button display = new Button { Text = "独立公式 $$…$$", AutoSize = true };
            inline.Click += delegate { source.SelectedText = "$x = \\frac{a}{b}$"; source.Focus(); };
            display.Click += delegate { source.SelectedText = "\r\n$$H(s)=\\frac{1+s/\\omega_z}{1+s/\\omega_p}$$\r\n"; source.Focus(); };
            toolbar.Controls.Add(inline); toolbar.Controls.Add(display);
            toolbar.Controls.Add(new Label { Text = "宽度", AutoSize = true, Padding = new Padding(5, 5, 0, 0) }); toolbar.Controls.Add(width);
            toolbar.Controls.Add(new Label { Text = "字号", AutoSize = true, Padding = new Padding(5, 5, 0, 0) }); toolbar.Controls.Add(fontSize);
            toolbar.Controls.Add(save);
            Button cancel = new Button { Text = "取消", AutoSize = true, DialogResult = DialogResult.Cancel };
            toolbar.Controls.Add(cancel); CancelButton = cancel;
            SplitContainer split = new SplitContainer { Size = new Size(1120, 550), Dock = DockStyle.Fill, SplitterDistance = 390, Panel1MinSize = 250, Panel2MinSize = 250 };
            Panel previewPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White }; previewPanel.Controls.Add(preview);
            split.Panel1.Controls.Add(source);
            split.Panel1.Controls.Add(new Label { Text = "输入内容（中文 / LaTeX）", Dock = DockStyle.Top, Height = 30, Padding = new Padding(6), BackColor = Color.FromArgb(238, 243, 248) });
            split.Panel2.Controls.Add(previewPanel);
            split.Panel2.Controls.Add(new Label { Text = "公式备注预览", Dock = DockStyle.Top, Height = 30, Padding = new Padding(6), BackColor = Color.FromArgb(238, 243, 248) });
            FlowLayoutPanel footer = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };
            footer.Controls.Add(new Label { Text = "公式示例：$$\\frac{a}{b}$$。公式使用英文半角符号；备注不参与计算。", AutoSize = true, MaximumSize = new Size(800, 0), Padding = new Padding(8) });
            footer.Controls.Add(status);
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(toolbar, 0, 0); layout.Controls.Add(split, 0, 1); layout.Controls.Add(footer, 0, 2);
            Controls.Add(layout);
            source.Text = snapshot.Source.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n"); width.Value = snapshot.Width; fontSize.Value = snapshot.FontSize;
            source.TextChanged += delegate { MarkDirty(); }; width.ValueChanged += delegate { MarkDirty(); }; fontSize.ValueChanged += delegate { MarkDirty(); };
            debounce.Tick += async delegate { debounce.Stop(); await RenderAsync(); };
            Shown += async delegate { source.Focus(); source.Select(0, 0); await RenderAsync(); };
            save.Click += delegate { if (Result != null && !dirty && !rendering) { DialogResult = DialogResult.OK; Close(); } };
            FormClosing += delegate { closing = true; debounce.Stop(); };
            FormClosed += delegate { debounce.Dispose(); renderer.Dispose(); if (preview.Image != null) preview.Image.Dispose(); };
        }
        private void MarkDirty() { editRevision++; dirty = true; save.Enabled = false; debounce.Stop(); debounce.Start(); }
        private async Task RenderAsync()
        {
            if (rendering || closing || IsDisposed) return;
            rendering = true; dirty = false; save.Enabled = false; status.Text = "正在排版…";
            string text = source.Text; int noteWidth = (int)width.Value; int noteFont = (int)fontSize.Value;
            int renderRevision = editRevision;
            try
            {
                NoteSnapshot snapshot = await renderer.RenderAsync(text, noteWidth, noteFont);
                if (closing || IsDisposed || renderRevision != editRevision) return;
                Result = snapshot;
                Image previous = preview.Image;
                using (MemoryStream stream = new MemoryStream(snapshot.Png))
                using (Bitmap image = new Bitmap(stream)) preview.Image = new Bitmap(image, snapshot.Width, snapshot.Height);
                if (previous != null) previous.Dispose();
                status.Text = renderer.Warning ?? (snapshot.FormulaCount > 0 ? "已排版 " + snapshot.FormulaCount + " 个公式" : "预览已更新（普通文字）");
                save.Enabled = !dirty;
            }
            catch (Exception exception) { if (!closing && !IsDisposed) { status.Text = exception.GetBaseException().Message; dirty = true; } }
            finally { rendering = false; if (dirty && !closing && !IsDisposed && editRevision != renderRevision) { debounce.Stop(); debounce.Start(); } }
        }
    }
}
