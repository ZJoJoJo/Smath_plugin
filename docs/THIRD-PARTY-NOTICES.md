# 第三方依赖

## SMath Studio

构建需使用者提供已安装 SMath 的公开 API 程序集。SMath 主程序及其程序集未包含在本仓库中，适用其自身许可。

## KaTeX 0.19.0

来源：https://registry.npmjs.org/katex/-/katex-0.19.0.tgz

仅公式备注插件使用。离线脚本、样式和字体位于 `source/MathNotes/assets/katex/`，上游许可文件为该目录的 `LICENSE`。安装 ZIP 同样保留许可文件。

## Microsoft WebView2 SDK 1.0.3537.50

来源：https://api.nuget.org/v3-flatcontainer/microsoft.web.webview2/1.0.3537.50/microsoft.web.webview2.1.0.3537.50.nupkg

仅公式备注插件使用。必要托管库及 x86/x64 加载器位于 `source/MathNotes/dependencies/WebView2/`，其许可文件为该目录的 `LICENSE.txt`。托管库以 `.bin` 文件名保留，在运行时加载；无需重新命名。

WebView2 Runtime 是独立系统组件，使用者自行安装，仓库不包含该 Runtime。
