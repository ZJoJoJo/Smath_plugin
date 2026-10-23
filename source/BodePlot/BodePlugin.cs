using System;
using System.Drawing;
using System.Windows.Forms;
using SMath.Controls;
using SMath.Manager;
namespace EngineeringTools
{
    public sealed class BodePlugin : IPluginCustomRegion, IPluginMenuExtender
    {
        public string TagName { get { return "engineering-bode"; } }
        public Type RegionType { get { return typeof(BodeRegion); } }
        public DragAndDropFileType[] DragAndDropFileTypes { get { return new DragAndDropFileType[0]; } }
        public string[] SupportedClipboardFormats { get { return new string[0]; } }
        public void Initialize() { }
        public void Dispose() { }
        public RegionBase CreateNew(SessionProfile profile) { return new BodeRegion(profile); }
        public MenuButton[] GetMenuItem(SessionProfile profile)
        {
            return new[] { new MenuButton("∿ 交互波特图…", args => PluginUi.Run(delegate { Insert(args); })) { Name = "BodePlot.Insert", Location = MenuButtonLocation.ToolsMenu, Behavior = MenuButtonBehavior.SingleExecutionForAll } };
        }
        public MenuButton[] GetMenuItems(SessionProfile profile)
        {
            return new[] { new MenuButton("交互波特图", args => PluginUi.Run(delegate { Insert(args); })) { Name = "BodePlot.Create", Behavior = MenuButtonBehavior.SingleExecutionForAll } };
        }
        public MenuButton[] GetContextMenuItems(MenuContext context)
        {
            if (context.CurrentRegions != null && context.CurrentRegions.Length == 1 && context.CurrentRegions[0] is BodeRegion)
                return new[] { new MenuButton("波特图设置…", args => ((BodeRegion)context.CurrentRegions[0]).Configure()) };
            return new MenuButton[0];
        }
        private static void Insert(MenuButtonArgs args)
        {
            if (args.Worksheet == null) throw new InvalidOperationException("请先打开计算书。");
            BodeRegion region = new BodeRegion(args.SessionProfile);
            using (SettingsForm dialog = new SettingsForm(region.Settings, region.Size))
            {
                if (dialog.ShowDialog() != DialogResult.OK) return;
                region.Settings = dialog.Result;
                region.SetPlotSize(dialog.ResultSize);
            }
            int bottom = 0;
            using (RegionsEnumerator regions = args.Worksheet.GetRegionsEnumerator())
                while (regions.MoveNext()) bottom = Math.Max(bottom, regions.AbsoluteLocation.Y + regions.Current.Size.Height);
            region.Location = new Point(0, bottom + 36);
            args.Worksheet.CreateRegions(new[] { region }, true, (LocationCorrectionMode)0);
            region.RequestEvaluation(true);
            args.Worksheet.ScrollToRegion(region, true);
        }
    }
}
