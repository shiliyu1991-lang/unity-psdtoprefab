# Changelog

本项目遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/) 与 [语义化版本](https://semver.org/lang/zh-CN/)。

## [1.1.0] - 2026-06-11

### Added
- 标准 UPM 包结构(package.json / asmdef / Samples~),支持 Package Manager 通过 Git URL 安装。
- TextMeshPro 支持:存在 TMP 时(Unity 6 内置)窗口出现"使用 TextMeshPro"开关,文本图层生成 TextMeshProUGUI。
- 兼容 Unity 6.x(已按 Unity 6.3 LTS / uGUI 2.0 适配),最低支持 Unity 2021.3 LTS。

### Changed
- 示例 PSD 移至 `Samples~/Example`,通过 Package Manager 的 Samples 导入。

## [1.0.0] - 2026-06-11

### Added
- 首个版本:纯 C# PSD 解析(8-bit RGB/灰度,RAW/RLE),嵌套分组、Unicode 图层名、文本图层(TySh)。
- UGUI Prefab 生成:图层切 Sprite、坐标/层级还原、btn_ 自动挂 Button、命名前缀 Btn_/Image_/Label_/Node_。
- 隐藏图层与完全透明图层自动忽略;图层不透明度映射到颜色 alpha;分组不透明度映射为 CanvasGroup。
- 编辑器窗口 Tools/PSD to Prefab,支持拖拽 PSD。
