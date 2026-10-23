# SMath 工程插件

四个独立插件，分别提供工程对齐、交互波特图、计算书 PDF 导出和中文公式备注。当前版本 **0.2.7.0**。

已验证环境：Windows、.NET Framework 4.8、SMath Studio 1.5.0.9678 / Core 1.75.9678.0。

## 目录

```text
source/
  AlignmentTools/   工程对齐源码、独立构建脚本
  BodePlot/         交互波特图源码、独立构建脚本
  PdfExport/        PDF 导出源码、独立构建脚本
  MathNotes/        中文公式备注源码、离线依赖、独立构建脚本
installers/         四个可直接安装的 ZIP、SHA-256 校验值
docs/               设计说明和第三方依赖说明
build.ps1           统一构建、打包入口
```

每个源码目录都包含自己的 `Common` 文件，不依赖其他插件源码目录。`MathNotes` 同时包含离线 KaTeX、字体和 WebView2 SDK 必要文件。SMath 主程序及其程序集由使用者自行安装，不随仓库提供。

## 安装和入口

在 SMath 的插件管理器中选择 **Install from file…**，按需选择 `installers` 中的 ZIP，安装后重启 SMath。保留 ZIP 原名，不要解压后安装。

| 插件安装包 | 功能与入口 |
| --- | --- |
| `AlignmentTools.0_2_7_0.zip` | 工具 → 工程对齐；工具 → 工程对齐面板…；Ctrl+Shift+L |
| `BodePlot.0_2_7_0.zip` | 工具 → ∿ 交互波特图…；插入 → 交互波特图 |
| `WorksheetPdf.0_2_7_0.zip` | 工具 → ▤ 导出计算书 PDF… |
| `MathNotes.0_2_7_0.zip` | 工具 → ✎ 中文公式备注…；插入 → 中文公式备注 |

工程对齐支持九种对齐与分布，菜单带中文和示意图标。波特图支持多条传递函数、联动游标、曲线显示复选框、坐标设置及图形尺寸调整。备注支持中文和离线 KaTeX，内容不进入计算。PDF 按内容分页输出高清图像，文字不支持搜索或复制。

首次编辑公式备注需要 Microsoft Edge WebView2 Runtime。已有备注的源文及预览图保存在计算书中。插件不会自动安装系统组件。

## 构建

使用 Windows PowerShell 和系统 .NET Framework C# 编译器。`SMathPath` 应指向含有 `SMath.Controls.dll` 等程序集的 SMath 安装目录。以下路径仅为示例，请替换为自己的安装位置。

```powershell
# 构建四个插件，输出至 build/
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -SMathPath "C:\Path\To\SMath"

# 只构建 PDF 插件
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -SMathPath "C:\Path\To\SMath" -Plugin PdfExport

# 构建并用 SMath 原生接口重新生成 installers/ 安装包及校验值
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -SMathPath "C:\Path\To\SMath" -Package
```

每个插件也可单独复制出去构建：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\source\BodePlot\build.ps1 -SMathPath "C:\Path\To\SMath" -Package
```

单独构建时，DLL 输出到该插件的 `build/`，ZIP 输出到该插件的 `packages/`。更新版本时同步修改插件的 `Common/AssemblyInfo.cs` 和 `plugin.json`。

## GitHub 上传

将本目录内的文件作为仓库根目录上传，保留 `source/`、`installers/`、`docs/` 和根目录文件。`.gitignore` 已排除构建输出，保留安装 ZIP 和必要的第三方运行库。

本目录未包含开发机器的 SMath 测试副本、个人计算书、测试缓存、备份或临时文件。项目自身的开源许可证由仓库所有者选择；第三方文件的许可见 [依赖说明](docs/THIRD-PARTY-NOTICES.md)。
