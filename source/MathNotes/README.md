# 中文公式备注 · MathNotes

入口：工具 → ✎ 中文公式备注…；插入 → 中文公式备注。左侧输入中文及 LaTeX，右侧实时预览；双击已有备注再次编辑。

```text
设计备注：这里的公式仅用于说明，不参与计算。
行内公式：$s=j2\pi f$。
$$H(s)=\frac{1+s/\omega_z}{1+s/\omega_p}$$
```

支持全角公式字符兼容。源文和预览图随计算书保存，内容不创建变量。编辑时跟随系统输入法。

`assets/` 包含预览页面、离线 KaTeX 和字体；`dependencies/WebView2/` 包含必要 SDK 库及加载器。保留这些文件和上游许可文件。首次编辑需要系统已安装 WebView2 Runtime。

本目录包含完整插件源码和 `Common` 公共代码，可独立复制出去构建：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -SMathPath "C:\Path\To\SMath" -Package
```

DLL 生成在本目录 `build/`，安装 ZIP 生成在 `packages/`。版本在 `Common/AssemblyInfo.cs` 和 `plugin.json` 中配置。
