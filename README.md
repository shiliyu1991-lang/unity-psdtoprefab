# PSD2Prefab (v1.0.0 经典版) — PSD 转 UGUI Prefab 的 Unity 编辑器插件

Convert Photoshop PSD files into UGUI prefabs inside the Unity Editor — pure C# PSD parser, no Photoshop required. (Classic edition: copy-into-Assets, Unity 2021+)

把 Photoshop 的 PSD 文件一键转换为 UGUI Prefab:每个图层切成 Sprite,按分组还原层级与坐标,文本图层生成 Text 组件,`btn_` 图层自动挂 Button。纯 C# 解析 PSD,无需安装 Photoshop,无任何外部依赖。

> 你正在查看本仓库的 **`assets-edition` 分支(经典版)**。最新的 UPM 版在 [`main` 分支](../../tree/main)。

**这是 v1.0.0 经典版**:直接拷贝进 `Assets/` 使用,适用 Unity 2021 LTS 及以上,文本使用 Legacy Text(无 TMP 依赖)。
如需 Package Manager(Git URL)安装与 TextMeshPro 支持(Unity 6 推荐),请切换到本仓库的 **[`main` 分支(v1.1.0 UPM 版)](../../tree/main)**。

## 安装

把本仓库中的 `PSD2Prefab` 文件夹拷贝到项目的 `Assets/` 目录下即可(代码都在 `Editor/` 中,不会打进包体)。

## 使用

1. 菜单栏 **Tools → PSD to Prefab** 打开窗口。
2. 选择 PSD 文件(点"浏览",或把 .psd 直接拖到窗口上)。
3. 设置输出目录(默认 `Assets/PSD2Prefab_Output`),点 **生成 Prefab**。
4. 生成结果:`输出目录/Node_<psd名>.prefab` + `<psd名>_Textures/` 切图目录。把 Prefab 拖到场景中的 Canvas 下即可使用。

## 命名规则

PSD 中的图层命名决定生成的节点类型与名字前缀:

| PSD 图层名 | 生成节点 | 组件 |
|---|---|---|
| `btn_关闭` | `Btn_关闭` | Image + Button(命名为 btn_ 的分组也会整组挂 Button) |
| 文本图层 | `Label_xxx` | Text(内容取自 PSD 文本) |
| 普通图片图层 | `Image_xxx` | Image |
| 分组 / 其它 | `Node_xxx` | 空 RectTransform(分组不透明度 < 100% 时附加 CanvasGroup) |

原图层名里的 `btn_`、`img_`、`image_`、`label_`、`txt_` 等前缀会被剥掉后再加上统一前缀;重名自动加序号。

## 转换规则

- 坐标:锚点居中,按 PSD 文档坐标换算 anchoredPosition,尺寸 = 图层包围盒。
- 层级顺序:与 PSD 一致(PSD 下层 = Hierarchy 中靠前的兄弟节点)。
- 隐藏图层、完全透明图层:跳过,不生成节点也不切图。
- 图层不透明度:应用到 Image/Text 的颜色 alpha。
- 文本图层:生成 UGUI Text,启用 BestFit 自适应字号,默认黑色居中(PSD 的字体/字号/颜色不还原,需手动调整)。
- Button:transition 为默认 ColorTint,targetGraphic 指向自身 Image/Text。
- 切图:每个栅格图层导出独立 PNG 并自动设置为 Sprite(Single、无 mipmap)。

## 支持范围

支持:8-bit RGB / 灰度 PSD,RAW 与 RLE 压缩,嵌套分组,中文等 Unicode 图层名,智能对象(按其栅格化结果)。

不支持(解析时会给出明确报错或忽略):PSB 大文档、16/32-bit 通道、CMYK 等色彩模式、图层样式(投影/描边等不会渲染,导出前请先栅格化或合并图层)、剪贴蒙版与图层蒙版(忽略)、调整图层(忽略)。

## 目录结构

```
PSD2Prefab/
├── Editor/
│   ├── Psd/
│   │   ├── PsdReader.cs      # 大端二进制读取器
│   │   ├── PsdLayer.cs       # 图层数据结构
│   │   └── PsdDocument.cs    # PSD 解析器(头/图层记录/分组/文本/像素)
│   ├── PsdPrefabBuilder.cs   # 切图导出 + UGUI 层级构建 + Prefab 保存
│   └── PsdToPrefabWindow.cs  # 编辑器窗口 (Tools/PSD to Prefab)
└── Example/
    └── sample.psd            # 含嵌套分组/中文图层名/btn_ 命名的测试文件
```

解析器(`Editor/Psd/`)不依赖 Unity API,已通过与 psd-tools 的逐像素交叉验证(分组嵌套、图层顺序、RLE/RAW 解码、Unicode 图层名、文本提取)。

## License

MIT © liyu
