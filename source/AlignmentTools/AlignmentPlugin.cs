using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SMath.Controls;
using SMath.Drawing;
using SMath.Manager;

namespace EngineeringTools
{
    public sealed class AlignmentPlugin : IPluginMenuExtender
    {
        public static readonly string[] Labels = { "左对齐", "水平居中", "右对齐", "顶部对齐", "垂直居中", "底部对齐", "水平等间距", "垂直等间距", "公式基线" };
        private readonly List<IDisposable> owned = new List<IDisposable>();
        public void Initialize() { }
        public void Dispose() { foreach (IDisposable item in owned) item.Dispose(); owned.Clear(); }
        public MenuButton[] GetMenuItem(SessionProfile profile)
        {
            MenuButton menu = BuildMenu(MenuButtonLocation.ToolsMenu);
            MenuButton open = new MenuButton("工程对齐面板…", args => PluginUi.Run(delegate
            {
                if (args.Worksheet == null) throw new InvalidOperationException("请先打开计算书。");
                AlignmentPalette palette = new AlignmentPalette(args.Worksheet);
                owned.Add(palette);
                palette.Show();
            })) {
                Name = "AlignmentTools.OpenPalette",
                Location = MenuButtonLocation.ToolsMenu | MenuButtonLocation.FormatToolbar,
                ShortcutKey = InputKeys.Control | InputKeys.Shift | InputKeys.L,
                ShortcutKeyDisplayString = "Ctrl+Shift+L",
                Behavior = MenuButtonBehavior.SingleExecutionForAll
            };
            DesktopBitmap paletteIcon = new DesktopBitmap(); paletteIcon.Wrap(CreateIcon(AlignmentMode.Left, 24));
            open.Icon = paletteIcon; owned.Add(paletteIcon);
            return new[] { menu, open, BuildMenu(MenuButtonLocation.ContextMenu) };
        }
        private MenuButton BuildMenu(MenuButtonLocation location)
        {
            MenuButton root = new MenuButton("工程对齐") { Name = "AlignmentTools." + location, Location = location, Behavior = MenuButtonBehavior.SingleExecutionForAll };
            foreach (AlignmentMode mode in Enum.GetValues(typeof(AlignmentMode)))
            {
                AlignmentMode selected = mode;
                Bitmap image = CreateIcon(mode, 24);
                DesktopBitmap icon = new DesktopBitmap();
                icon.Wrap(image);
                owned.Add(icon);
                MenuButton button = root.AppendChild(Labels[(int)mode], args => PluginUi.Run(delegate { Alignment.Apply(args, selected); }));
                button.Icon = icon;
                button.Behavior = MenuButtonBehavior.DisableWhenNoSelection | MenuButtonBehavior.SingleExecutionForAll;
            }
            return root;
        }
        public static Bitmap CreateIcon(AlignmentMode mode, int pixels)
        {
            Bitmap bitmap = new Bitmap(pixels, pixels);
            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(bitmap))
            using (Pen guide = new Pen(Color.FromArgb(39, 111, 177), 1.5f))
            using (Brush mark = new SolidBrush(Color.FromArgb(73, 80, 91)))
            {
                graphics.ScaleTransform(pixels / 24f, pixels / 24f);
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                bool horizontal = mode == AlignmentMode.Left || mode == AlignmentMode.Center || mode == AlignmentMode.Right || mode == AlignmentMode.Baseline || mode == AlignmentMode.VerticalSpacing;
                if (mode == AlignmentMode.HorizontalSpacing || mode == AlignmentMode.VerticalSpacing)
                {
                    for (int index = 0; index < 3; index++)
                    {
                        if (horizontal) graphics.FillRectangle(mark, 7, 3 + index * 7, 10, 3);
                        else graphics.FillRectangle(mark, 3 + index * 7, 7, 3, 10);
                    }
                    if (horizontal) { graphics.DrawLine(guide, 3, 4, 3, 20); graphics.DrawLine(guide, 1, 8, 5, 8); graphics.DrawLine(guide, 1, 15, 5, 15); }
                    else { graphics.DrawLine(guide, 4, 3, 20, 3); graphics.DrawLine(guide, 8, 1, 8, 5); graphics.DrawLine(guide, 15, 1, 15, 5); }
                }
                else if (mode == AlignmentMode.Baseline)
                {
                    graphics.DrawLine(guide, 1, 17, 23, 17);
                    graphics.FillRectangle(mark, 4, 6, 4, 11); graphics.FillRectangle(mark, 11, 10, 4, 7); graphics.FillRectangle(mark, 18, 3, 3, 14);
                }
                else
                {
                    int anchor = mode == AlignmentMode.Left || mode == AlignmentMode.Top ? 3 : mode == AlignmentMode.Right || mode == AlignmentMode.Bottom ? 21 : 12;
                    if (horizontal) graphics.DrawLine(guide, anchor, 1, anchor, 23); else graphics.DrawLine(guide, 1, anchor, 23, anchor);
                    int[] lengths = { 13, 7, 10 };
                    for (int index = 0; index < 3; index++)
                    {
                        int start = mode == AlignmentMode.Left || mode == AlignmentMode.Top ? anchor : mode == AlignmentMode.Right || mode == AlignmentMode.Bottom ? anchor - lengths[index] : anchor - lengths[index] / 2;
                        if (horizontal) graphics.FillRectangle(mark, start, 4 + 6 * index, lengths[index], 3);
                        else graphics.FillRectangle(mark, 4 + 6 * index, start, 3, lengths[index]);
                    }
                }
            }
            return bitmap;
        }
    }

    public sealed class AlignmentPalette : Form
    {
        private readonly Worksheet worksheet;
        private readonly Timer timer = new Timer { Interval = 300 };
        private readonly Label selection = new Label { Dock = DockStyle.Bottom, Height = 28, TextAlign = ContentAlignment.MiddleLeft };
        public AlignmentPalette(Worksheet worksheet)
        {
            this.worksheet = worksheet;
            Text = "工程对齐";
            Font = new Font("Microsoft YaHei UI", 9);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(390, 190);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), ColumnCount = 3, RowCount = 3 };
            for (int index = 0; index < 3; index++) { grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f)); grid.RowStyles.Add(new RowStyle(SizeType.Percent, 33.333f)); }
            foreach (AlignmentMode mode in Enum.GetValues(typeof(AlignmentMode)))
            {
                AlignmentMode selected = mode;
                Button button = new Button { Text = AlignmentPlugin.Labels[(int)mode], Image = AlignmentPlugin.CreateIcon(mode, 24), TextImageRelation = TextImageRelation.ImageBeforeText, Dock = DockStyle.Fill, AccessibleName = AlignmentPlugin.Labels[(int)mode], Margin = new Padding(3) };
                button.Click += delegate { PluginUi.Run(delegate { Alignment.Apply(new MenuButtonArgs(this.worksheet), selected); }); };
                grid.Controls.Add(button, (int)mode % 3, (int)mode / 3);
                button.Disposed += delegate { button.Image.Dispose(); };
            }
            Controls.Add(grid); Controls.Add(selection);
            timer.Tick += delegate
            {
                try
                {
                    EditableWorksheet editable = worksheet as EditableWorksheet;
                    selection.Text = "  已选中 " + (editable == null ? 0 : editable.GetSelectedRegions().Count) + " 个区域";
                }
                catch (ObjectDisposedException) { Close(); }
            };
            timer.Start();
            FormClosed += delegate { timer.Dispose(); };
        }
    }
}
