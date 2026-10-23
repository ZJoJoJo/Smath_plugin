using System;
using System.Drawing;
using System.Windows.Forms;
using SMath.Controls;
using SMath.Manager;

namespace EngineeringTools
{
    public sealed class PdfPlugin : IPluginMenuExtender
    {
        public void Initialize() { }
        public void Dispose() { }
        public MenuButton[] GetMenuItem(SessionProfile profile)
        {
            return new[] { new MenuButton("▤ 导出计算书 PDF…", args => PluginUi.Run(delegate { Export(args); })) { Name = "WorksheetPdf.Export", Location = MenuButtonLocation.ToolsMenu, Behavior = MenuButtonBehavior.SingleExecutionForAll } };
        }
        private static void Export(MenuButtonArgs args)
        {
            if (args.Worksheet == null) throw new InvalidOperationException("请先打开计算书。");
            PdfExportSettings settings;
            using (PdfSettingsForm form = new PdfSettingsForm()) { if (form.ShowDialog() != DialogResult.OK) return; settings = form.Result; }
            using (SaveFileDialog dialog = new SaveFileDialog { Filter = "PDF 文件 (*.pdf)|*.pdf", DefaultExt = "pdf", AddExtension = true, FileName = "计算书.pdf" })
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                int pages = WorksheetPdf.Export(args.Worksheet, dialog.FileName, settings);
                MessageBox.Show("已导出 " + pages + " 页。", "PDF 导出", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
    public sealed class PdfSettingsForm : Form
    {
        public PdfExportSettings Result { get; private set; }
        public PdfSettingsForm()
        {
            Text = "计算书 PDF 导出"; Font = new Font("Microsoft YaHei UI", 9); ClientSize = new Size(390, 250);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false; StartPosition = FormStartPosition.CenterParent;
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), ColumnCount = 2, RowCount = 6 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ComboBox paper = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill }; paper.Items.AddRange(new object[] { "A4 纵向", "A4 横向", "A3 纵向" }); paper.SelectedIndex = 0;
            ComboBox dpi = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill }; dpi.Items.AddRange(new object[] { "200 dpi", "300 dpi" }); dpi.SelectedIndex = 1;
            NumericUpDown margin = new NumericUpDown { Minimum = 5, Maximum = 30, Value = 12, Dock = DockStyle.Fill };
            CheckBox numbers = new CheckBox { Text = "显示页码", Checked = true, Dock = DockStyle.Fill };
            layout.Controls.Add(new Label { Text = "纸张", AutoSize = true }, 0, 0); layout.Controls.Add(paper, 1, 0);
            layout.Controls.Add(new Label { Text = "清晰度", AutoSize = true }, 0, 1); layout.Controls.Add(dpi, 1, 1);
            layout.Controls.Add(new Label { Text = "页边距 / mm", AutoSize = true }, 0, 2); layout.Controls.Add(margin, 1, 2);
            layout.Controls.Add(numbers, 1, 3);
            Label detail = new Label { Text = "自动分页，尽量避开公式和图形。\nPDF 使用高清图像保存，文字不支持搜索。", AutoSize = true }; layout.Controls.Add(detail, 0, 4); layout.SetColumnSpan(detail, 2);
            Button export = new Button { Text = "选择保存位置…", AutoSize = true, Dock = DockStyle.Fill };
            export.Click += delegate { Result = new PdfExportSettings { WidthMillimeters = paper.SelectedIndex == 1 ? 297 : paper.SelectedIndex == 2 ? 297 : 210, HeightMillimeters = paper.SelectedIndex == 1 ? 210 : paper.SelectedIndex == 2 ? 420 : 297, Dpi = dpi.SelectedIndex == 0 ? 200 : 300, MarginMillimeters = (double)margin.Value, PageNumbers = numbers.Checked }; DialogResult = DialogResult.OK; };
            layout.Controls.Add(export, 0, 5); layout.SetColumnSpan(export, 2); Controls.Add(layout); AcceptButton = export;
        }
    }
}
