using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace EngineeringTools
{
    public sealed class NoteSnapshot
    {
        public string Source = "备注：这里的公式仅用于说明，不参与计算。\n$$H(s)=\\frac{1+s/\\omega_z}{1+s/\\omega_p}$$";
        public int Width = 660;
        public int Height = 100;
        public int FontSize = 16;
        public byte[] Png;
        public int FormulaCount;
        public int FractionCount;
        public NoteSnapshot Clone() { return (NoteSnapshot)MemberwiseClone(); }
    }
    public sealed class NoteRenderInfo
    {
        public string type;
        public int revision;
        public double width;
        public double height;
        public double ratio;
        public string[] errors;
        public int formulaCount;
        public int fractionCount;
        public int normalizedCount;
    }
    public sealed class NotesRenderer : IDisposable
    {
        // The off-screen WebView must never take keyboard/IME focus from the source editor.
        private sealed class RenderHost : Form
        {
            protected override bool ShowWithoutActivation { get { return true; } }
            protected override CreateParams CreateParams
            {
                get { CreateParams value = base.CreateParams; value.ExStyle |= 0x08000000 | 0x00000080; return value; }
            }
        }
        private static bool loaderConfigured;
        private readonly Form host;
        private readonly dynamic browser;
        private readonly Assembly core;
        private readonly JavaScriptSerializer json = new JavaScriptSerializer();
        private Task initialization;
        private readonly TaskCompletionSource<bool> ready = new TaskCompletionSource<bool>();
        private TaskCompletionSource<NoteRenderInfo> pending;
        private int revision;
        private bool disposed;
        public string Warning { get; private set; }
        public NotesRenderer()
        {
            host = new RenderHost { FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1350, 400), AutoScaleMode = AutoScaleMode.None };
            string folder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "libraries");
            core = Assembly.LoadFrom(Path.Combine(folder, "Microsoft.Web.WebView2.Core.bin"));
            Assembly forms = Assembly.LoadFrom(Path.Combine(folder, "Microsoft.Web.WebView2.WinForms.bin"));
            browser = Activator.CreateInstance(forms.GetType("Microsoft.Web.WebView2.WinForms.WebView2", true));
            ((Control)browser).Dock = DockStyle.Fill;
            host.Controls.Add((Control)browser);
        }
        private object CoreEnum(string name, string value) { return Enum.Parse(core.GetType("Microsoft.Web.WebView2.Core." + name, true), value); }
        private static void Listen(object source, string name, Action<object, object> action)
        {
            EventInfo eventInfo = source.GetType().GetEvent(name);
            ParameterInfo[] parameters = eventInfo.EventHandlerType.GetMethod("Invoke").GetParameters();
            ParameterExpression sender = Expression.Parameter(parameters[0].ParameterType, "sender");
            ParameterExpression arguments = Expression.Parameter(parameters[1].ParameterType, "arguments");
            Delegate handler = Expression.Lambda(eventInfo.EventHandlerType, Expression.Invoke(Expression.Constant(action), Expression.Convert(sender, typeof(object)), Expression.Convert(arguments, typeof(object))), sender, arguments).Compile();
            eventInfo.AddEventHandler(source, handler);
        }
        private async Task InitializeAsync()
        {
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string loader = Path.Combine(folder, "runtimes", IntPtr.Size == 8 ? "win-x64" : "win-x86", "native");
            Type environmentType = core.GetType("Microsoft.Web.WebView2.Core.CoreWebView2Environment", true);
            if (!loaderConfigured)
            {
                environmentType.GetMethod("SetLoaderDllFolderPath").Invoke(null, new object[] { loader });
                loaderConfigured = true;
            }
            string cache = Environment.GetEnvironmentVariable("SMATH_NOTES_CACHE");
            if (string.IsNullOrEmpty(cache)) cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMathMathNotes", "WebView2");
            Directory.CreateDirectory(cache);
            Task environmentTask = (Task)environmentType.GetMethod("CreateAsync").Invoke(null, new object[] { null, cache, null });
            await environmentTask;
            dynamic environment = environmentTask.GetType().GetProperty("Result").GetValue(environmentTask, null);
            host.Show();
            await (Task)browser.EnsureCoreWebView2Async(environment);
            browser.ZoomFactor = 2;
            browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            browser.CoreWebView2.Settings.AreDevToolsEnabled = false;
            browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            browser.CoreWebView2.SetVirtualHostNameToFolderMapping("smath-notes.local", Path.Combine(folder, "assets"), (dynamic)CoreEnum("CoreWebView2HostResourceAccessKind", "Allow"));
            browser.CoreWebView2.AddWebResourceRequestedFilter("*", (dynamic)CoreEnum("CoreWebView2WebResourceContext", "All"));
            Listen((object)browser.CoreWebView2, "WebResourceRequested", delegate(object sender, object arguments)
            {
                dynamic args = arguments;
                Uri address;
                if (Uri.TryCreate(args.Request.Uri, UriKind.Absolute, out address) && address.Host != "smath-notes.local" && address.Scheme != "data")
                    args.Response = environment.CreateWebResourceResponse(new MemoryStream(), 403, "Offline only", "");
            });
            Listen((object)browser.CoreWebView2, "NavigationStarting", delegate(object sender, object arguments) { dynamic args = arguments; if (!args.Uri.StartsWith("https://smath-notes.local/", StringComparison.OrdinalIgnoreCase)) args.Cancel = true; });
            Listen((object)browser.CoreWebView2, "NewWindowRequested", delegate(object sender, object arguments) { dynamic args = arguments; args.Handled = true; });
            Listen((object)browser.CoreWebView2, "WebMessageReceived", delegate(object sender, object arguments)
            {
                dynamic args = arguments;
                NoteRenderInfo info = json.Deserialize<NoteRenderInfo>((string)args.WebMessageAsJson);
                if (info.type == "ready") ready.TrySetResult(true);
                if (info.type == "rendered" && info.revision == revision && pending != null) pending.TrySetResult(info);
            });
            browser.Source = new Uri("https://smath-notes.local/preview.html?v=" + Assembly.GetExecutingAssembly().GetName().Version);
            if (await Task.WhenAny(ready.Task, Task.Delay(15000)) != ready.Task) throw new InvalidOperationException("离线公式渲染器启动超时。");
        }
        public async Task<NoteSnapshot> RenderAsync(string source, int width, int fontSize)
        {
            if (disposed) throw new ObjectDisposedException("NotesRenderer");
            if (source.Length > 200000) throw new ArgumentException("备注内容过长，请拆分成多个备注区域。");
            if (width < 240 || width > 1000 || fontSize < 10 || fontSize > 30) throw new ArgumentException("备注宽度或字号超出范围。");
            if (initialization == null) initialization = InitializeAsync();
            await initialization;
            host.ClientSize = new Size(width * 2 + 20, 600);
            pending = new TaskCompletionSource<NoteRenderInfo>();
            int requestRevision = ++revision;
            await (Task)browser.CoreWebView2.ExecuteScriptAsync("window.setNote(" + json.Serialize(source) + "," + width + "," + fontSize + "," + requestRevision + ");");
            if (await Task.WhenAny(pending.Task, Task.Delay(15000)) != pending.Task) throw new InvalidOperationException("公式排版超时，请检查公式内容。");
            NoteRenderInfo info = await pending.Task;
            if (info.height > 4000) throw new ArgumentException("备注超过 4000 像素，请拆成多个区域。");
            host.ClientSize = new Size(width * 2 + 20, (int)Math.Ceiling(info.height * 2 + 24));
            await Task.Delay(120);
            using (MemoryStream stream = new MemoryStream())
            {
                await (Task)browser.CoreWebView2.CapturePreviewAsync((dynamic)CoreEnum("CoreWebView2CapturePreviewImageFormat", "Png"), stream);
                stream.Position = 0;
                using (Bitmap captured = new Bitmap(stream))
                {
                    int captureWidth = Math.Min(captured.Width, (int)Math.Round(info.width * info.ratio));
                    int captureHeight = Math.Min(captured.Height, (int)Math.Round(info.height * info.ratio));
                    if (captureWidth <= 0 || captureHeight <= 0) throw new InvalidOperationException("公式备注图像尺寸无效。");
                    using (Bitmap cropped = captured.Clone(new Rectangle(0, 0, captureWidth, captureHeight), PixelFormat.Format32bppArgb))
                    using (MemoryStream png = new MemoryStream())
                    {
                        cropped.Save(png, ImageFormat.Png);
                        Warning = info.errors != null && info.errors.Length > 0 ? info.errors[0] : info.normalizedCount > 0 ? "已兼容全角公式符号，已排版 " + info.formulaCount + " 个公式（源文保留原样）。" : null;
                        return new NoteSnapshot { Source = source, Width = width, Height = (int)Math.Ceiling(info.height), FontSize = fontSize, Png = png.ToArray(), FormulaCount = info.formulaCount, FractionCount = info.fractionCount };
                    }
                }
            }
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            browser.Dispose(); host.Close(); host.Dispose();
        }
    }
}
