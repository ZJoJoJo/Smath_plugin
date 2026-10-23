# 计算书 PDF 导出 · WorksheetPdf

入口：工具 → ▤ 导出计算书 PDF…，与其他三个插件集中放在“工具”菜单。

支持 A4 纵/横向、A3 纵向、200/300 dpi、页边距和页码。按内容自动分页，尽量避开公式和图形。输出为高清图像 PDF，文字不支持搜索或复制。

本目录包含完整插件源码和 `Common` 公共代码，可独立复制出去构建：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -SMathPath "C:\Path\To\SMath" -Package
```

DLL 生成在本目录 `build/`，安装 ZIP 生成在 `packages/`；程序集名称为 `WorksheetPdf`。版本在 `Common/AssemblyInfo.cs` 和 `plugin.json` 中配置。
