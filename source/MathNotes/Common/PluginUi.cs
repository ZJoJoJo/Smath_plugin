using System;
using System.Windows.Forms;
namespace EngineeringTools
{
    internal static class PluginUi
    {
        public static void Run(Action action)
        {
            try { action(); }
            catch (Exception exception) { MessageBox.Show(exception.Message, "SMath 插件", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
    }
}
