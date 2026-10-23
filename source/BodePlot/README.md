# 交互波特图 · BodePlot

入口：工具 → ∿ 交互波特图…；插入 → 交互波特图。先在计算书中定义函数，再插入图形。

曲线以分号分隔，例如 `H.0;H.1(s);H.2(s);T.OL(s)`；点号表示 SMath 下标。按 `s = jω` 调用 SMath 原生求值，传递函数需无量纲。

支持多条幅频/相频曲线、联动游标、Hz/rad/s、dB/倍率、相位度/弧度、相位展开、坐标范围和尺寸设置。图例复选框控制对应两幅曲线和底部读数；默认全部勾选，选择状态随计算书保存。双击图形打开设置。

本目录包含完整插件源码和 `Common` 公共代码，可独立复制出去构建：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -SMathPath "C:\Path\To\SMath" -Package
```

DLL 生成在本目录 `build/`，安装 ZIP 生成在 `packages/`。版本在 `Common/AssemblyInfo.cs` 和 `plugin.json` 中配置。
