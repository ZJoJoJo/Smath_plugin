# 设计与接口

四个插件分别使用自己的程序集、GUID 和 ZIP，可独立安装。菜单入口统一放在“工具”菜单。

| 插件 | 主要实现 |
| --- | --- |
| AlignmentTools | `IPluginMenuExtender` 提供带图标菜单、工具栏和浮动面板；区域对齐使用工作表历史事务 |
| BodePlot | `IPluginCustomRegion`、`RegionEvaluable`；使用 SMath 原生复数求值，绘制幅频及相频曲线 |
| PdfExport | `IPluginMenuExtender`；通过公开区域绘制接口分页生成图像 PDF |
| MathNotes | `IPluginCustomRegion`、`RegionBase`；独立显示中文和 KaTeX 公式，不实现求值接口 |

波特图和备注在 `OnPaint` 内使用传入区域坐标建立局部绘图坐标，再恢复调用方画布。可编辑区域通过 `ToXml` / `FromXml` 保存，并提供独立历史步骤支持撤销与重做。

波特图按 `s = jω` 求值，支持常数和单参数传递函数。曲线图例默认勾选，取消后隐藏对应两幅曲线、交点及底部读数，选择状态随计算书保存。频率读数使用采样插值。

备注源文使用普通多行文本框，输入法跟随系统。离线 WebView2 仅负责 KaTeX 排版，结果保存为图像，源文不交给 SMath 求值器。兼容公式中的全角美元符号和全角 ASCII；原始源文保留。

安装包使用 SMath 的 `PluginInstallerUtility.AppendVersionToFileName` 与 `Archiver` 创建，主 DLL 条目的 ZIP 注释保存插件 GUID。

## 参考

- [SMath Creating Extensions](https://docs.smath.com/creating-extensions/)
- [IPluginMenuExtender](https://smath.com/documentation/api/html/T_SMath_Controls_IPluginMenuExtender.htm)
- [IPluginCustomRegion](https://smath.com/documentation/api/html/T_SMath_Controls_IPluginCustomRegion.htm)
- [RegionBase](https://smath.com/documentation/api/html/T_SMath_Controls_RegionBase.htm)
- [RegionEvaluable](https://smath.com/documentation/api/html/T_SMath_Controls_RegionEvaluable.htm)
