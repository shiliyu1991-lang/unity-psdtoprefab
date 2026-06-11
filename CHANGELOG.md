# Changelog

## [1.0.0] - 2026-06-11

### Added
- 首个版本(经典版,拷贝进 Assets 使用,Unity 2021+):
- 纯 C# PSD 解析(8-bit RGB/灰度,RAW/RLE),嵌套分组、Unicode 图层名、文本图层(TySh)。
- UGUI Prefab 生成:图层切 Sprite、坐标/层级还原、btn_ 自动挂 Button、命名前缀 Btn_/Image_/Label_/Node_。
- 隐藏图层与完全透明图层自动忽略;图层不透明度映射到颜色 alpha;分组不透明度映射为 CanvasGroup。
- 编辑器窗口 Tools/PSD to Prefab,支持拖拽 PSD。

> 后续版本(v1.1.0 起)以 UPM 包形式维护,见本仓库 `main` 分支(Unity 6 适配 + TextMeshPro 支持)。
