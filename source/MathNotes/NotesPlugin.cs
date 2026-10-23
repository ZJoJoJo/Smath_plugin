using System;
using System.Drawing;
using System.Windows.Forms;
using SMath.Controls;
using SMath.Manager;
namespace EngineeringTools
{
    public sealed class NotesPlugin : IPluginCustomRegion, IPluginMenuExtender
    {
        public string TagName { get { return "engineering-notes"; } }
        public Type RegionType { get { return typeof(NotesRegion); } }
        public DragAndDropFileType[] DragAndDropFileTypes { get { return new DragAndDropFileType[0]; } }
        public string[] SupportedClipboardFormats { get { return new string[0]; } }
        public void Initialize() { }
        public void Dispose() { }
        public RegionBase CreateNew(SessionProfile profile) { return new NotesRegion(profile); }
        public MenuButton[] GetMenuItem(SessionProfile profile)
        {
            return new[] { new MenuButton("✎ 中文公式备注…", args => PluginUi.Run(delegate { Insert(args); })) { Name = "MathNotes.Insert", Location = MenuButtonLocation.ToolsMenu, Behavior = MenuButtonBehavior.SingleExecutionForAll } };
        }
        public MenuButton[] GetMenuItems(SessionProfile profile)
        {
            return new[] { new MenuButton("中文公式备注", args => PluginUi.Run(delegate { Insert(args); })) { Name = "MathNotes.Create", Behavior = MenuButtonBehavior.SingleExecutionForAll } };
        }
        public MenuButton[] GetContextMenuItems(MenuContext context)
        {
            if (context.CurrentRegions != null && context.CurrentRegions.Length == 1 && context.CurrentRegions[0] is NotesRegion) return new[] { new MenuButton("编辑中文公式备注…", args => ((NotesRegion)context.CurrentRegions[0]).Configure()) };
            return new MenuButton[0];
        }
        private static void Insert(MenuButtonArgs args)
        {
            if (args.Worksheet == null) throw new InvalidOperationException("请先打开计算书。");
            using (NotesEditor editor = new NotesEditor(new NoteSnapshot()))
            {
                if (editor.ShowDialog() != DialogResult.OK) return;
                NotesRegion region = new NotesRegion(args.SessionProfile); region.SetSnapshot(editor.Result);
                int bottom = 0;
                using (RegionsEnumerator regions = args.Worksheet.GetRegionsEnumerator()) while (regions.MoveNext()) bottom = Math.Max(bottom, regions.AbsoluteLocation.Y + regions.Current.Size.Height);
                region.Location = new Point(0, bottom + 24);
                args.Worksheet.CreateRegions(new[] { region }, true, (LocationCorrectionMode)0);
                args.Worksheet.ScrollToRegion(region, true);
            }
        }
    }
}
