using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SMath.Controls;
using SMath.Drawing;
using SMath.Manager;
namespace EngineeringTools
{
    public sealed class NotesRegion : RegionBase
    {
        private NoteSnapshot snapshot = new NoteSnapshot();
        private IBitmap cached;
        public NoteSnapshot Snapshot { get { return snapshot.Clone(); } }
        public NotesRegion(SessionProfile profile) : base(profile) { Size = new Size(snapshot.Width + 8, snapshot.Height + 8); }
        public NotesRegion(NotesRegion original) : base(original) { snapshot = original.snapshot.Clone(); Size = original.Size; }
        public override RegionBase Clone() { return new NotesRegion(this); }
        public override bool IsEmpty() { return false; }
        public override HistoryStepBase GetHistoryStep() { return new NoteHistoryStep(this); }
        public void SetSnapshot(NoteSnapshot value)
        {
            if (value.Width < 240 || value.Width > 1000 || value.Height < 1 || value.Height > 4000 || value.FontSize < 10 || value.FontSize > 30 || value.Source == null || value.Source.Length > 200000) throw new ArgumentException("备注内容或尺寸无效。");
            if (value.Png != null && value.Png.Length > 20000000) throw new ArgumentException("备注图像过大。");
            snapshot = value.Clone();
            if (cached != null) { cached.Dispose(); cached = null; }
            Size = new Size(snapshot.Width + 8, snapshot.Height + 8);
            Invalidate();
        }
        public override void OnPaint(PaintEventOptions options)
        {
            options.Graphics.Save();
            try
            {
                options.Graphics.Translate(options.ClipRectangle.Left, options.ClipRectangle.Top);
                if (snapshot.Png != null && snapshot.Png.Length > 0)
                {
                    if (cached == null) { cached = new DesktopBitmap(); using (MemoryStream stream = new MemoryStream(snapshot.Png, false)) cached.FromStream(stream); }
                    options.Graphics.DrawImage(cached, new RectangleF(0, 0, snapshot.Width, snapshot.Height), new RectangleF(0, 0, cached.Width, cached.Height));
                }
                else
                {
                    options.Graphics.FillRectangle(new ColorBrush(Color.FromArgb(245, 248, 251)), 0, 0, snapshot.Width, snapshot.Height);
                    options.Graphics.DrawString("双击编辑中文公式备注", new FontInfo("Microsoft YaHei UI", 11, (FontfaceStyle)0), new ColorBrush(Color.FromArgb(50, 70, 90)), 12, 12, Size.Width - 24, StringOptions.GenericDefault);
                }
            }
            finally { options.Graphics.Restore(); }
        }
        public override void OnMouseDown(MouseEventOptions options) { base.OnMouseDown(options); if (options.Clicks >= 2) Configure(); }
        public void Configure()
        {
            using (NotesEditor editor = new NotesEditor(snapshot))
            {
                if (editor.ShowDialog() != DialogResult.OK) return;
                using (Parent.History.CreateTransaction(false)) { SetSnapshot(editor.Result); CreateHistoryStep("备注内容"); }
            }
        }
        public override void ToXml(StorageWriter writer, FileParsingContext context)
        {
            writer.Properties.SetString("source", snapshot.Source);
            writer.Properties.SetInt32("noteWidth", snapshot.Width); writer.Properties.SetInt32("noteHeight", snapshot.Height); writer.Properties.SetInt32("noteFontSize", snapshot.FontSize);
            writer.Properties.SetString("png", snapshot.Png == null ? "" : Convert.ToBase64String(snapshot.Png));
        }
        public override void FromXml(StorageReader reader, FileParsingContext context)
        {
            string png = reader.Properties.GetString("png", "");
            if (png.Length > 28000000) throw new ArgumentException("备注图像超过大小限制。");
            SetSnapshot(new NoteSnapshot { Source = reader.Properties.GetString("source", ""), Width = reader.Properties.GetInt32("noteWidth", 660), Height = reader.Properties.GetInt32("noteHeight", 100), FontSize = reader.Properties.GetInt32("noteFontSize", 16), Png = string.IsNullOrEmpty(png) ? null : Convert.FromBase64String(png) });
        }
        public override void Dispose() { if (cached != null) { cached.Dispose(); cached = null; } base.Dispose(); }
    }
    public sealed class NoteHistoryStep : HistoryStepBase
    {
        private readonly NoteSnapshot snapshot;
        public NoteHistoryStep(NotesRegion region) : base(region) { snapshot = region.Snapshot; }
        public override void Apply(RegionBase region) { ((NotesRegion)region).SetSnapshot(snapshot); base.Apply(region); }
        public override bool Equals(object other)
        {
            NoteHistoryStep step = other as NoteHistoryStep;
            return step != null && base.Equals(other) && snapshot.Source == step.snapshot.Source && snapshot.Width == step.snapshot.Width && snapshot.FontSize == step.snapshot.FontSize && snapshot.Height == step.snapshot.Height;
        }
        public override int GetHashCode() { return base.GetHashCode() ^ snapshot.Source.GetHashCode() ^ snapshot.Width ^ snapshot.FontSize; }
    }
}
