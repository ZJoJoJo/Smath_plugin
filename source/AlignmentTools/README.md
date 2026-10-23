# 工程对齐 · AlignmentTools

支持左、右、上、下、水平/垂直居中、水平/垂直等间距和公式基线对齐。菜单和浮动面板带中文标题及示意图标。

入口：工具 → 工程对齐 / 工程对齐面板…；工具栏；Ctrl+Shift+L。先选中两个或更多区域；等间距至少选择三个。支持撤销与重做。

本目录包含完整插件源码和 `Common` 公共代码。可独立复制出去构建，传入自己的 SMath 安装目录：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -SMathPath "C:\Path\To\SMath" -Package
```

DLL 生成在本目录 `build/`，安装 ZIP 生成在 `packages/`。版本在 `Common/AssemblyInfo.cs` 和 `plugin.json` 中配置。
