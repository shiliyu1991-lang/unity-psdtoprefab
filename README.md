# PSD2Prefab — PSD 转 UGUI Prefab 的 Unity 编辑器插件

把 Photoshop 的 **PSD** 文件一键转换为 **UGUI Prefab**:每个图层切成 Sprite,按分组还原层级与坐标,文本图层生成 Text / TextMeshPro 组件,`btn_` 图层自动挂 Button。**纯 C# 解析 PSD,无需安装 Photoshop,无任何外部依赖。**

Convert Photoshop PSD files into UGUI prefabs inside the Unity Editor — pure C# PSD parser, no Photoshop required.

[![Unity](https://img.shields.io/badge/Unity-2021.3%20LTS%20~%206.3%20LTS-black)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.md)
[![UPM](https://img.shields.io/badge/UPM-v1.1.0-success)](package.json)

---

## 仓库结构:一个仓库,两个版本(两个分支)

| 分支 | 版本 | 形态 | 适用场景 |
|---|---|---|---|
| **`main`**(默认) | **v1.1.0 UPM 版** | 标准 UPM 包(`package.json` / `asmdef` / `Samples~`) | **推荐**。Package Manager 通过 Git URL 安装,支持 Unity 6 + TextMeshPro |
| **`assets-edition`** | v1.0.0 经典版 | 直接拷进 `Assets/` 的源码 | 不想引入包管理的老项目,仅 Legacy Text |

> 当前 README 描述的是 **main 分支(UPM 版)**。经典版文档见 [`assets-edition` 分支](../../tree/assets-edition)。

**支持 Unity 2021.3 LTS ~ Unity 6.3 LTS (6000.3)。** Unity 6 下文本图层默认可使用 TextMeshPro。

---

## 安装(UPM 版 / main 分支)

**方式一(推荐,UPM Git URL):** 打开 **Window → Package Manager → + → Add package from git URL...**,输入:

```
https://github.com/shiliyu1991-lang/unity-psdtoprefab.git
```

> main 是默认分支,无需指定。若想固定到某个版本,可在 URL 后追加 `#v1.1.0`。

**方式二(直接拷贝):** 把整个 `Editor` 文件夹连同 `package.json` 放到项目某个目录(例如 `Assets/PSD2Prefab/`)。
> 若只想要"拷进 Assets、零包管理"的最简形态,请改用 [`assets-edition` 分支](../../tree/assets-edition) 的经典版。

代码都在 Editor 程序集中,不会打进游戏包体。示例 PSD 可在 Package Manager 的 **Samples** 页签导入(直接拷贝方式则在 `Samples~/Example/sample.psd`)。

---

## 使用

1. 菜单栏 **Tools → PSD to Prefab** 打开窗口。
2. 选择 PSD 文件(点"浏览",或把 .psd 直接拖到窗口上)。
3. 设置输出目录(默认 `Assets/PSD2Prefab_Output`);若项目中有 TextMeshPro(Unity 6 内置),可勾选"使用 TextMeshPro"。
4. 点 **生成 Prefab**:输出 `Node_<psd名>.prefab` + `<psd名>_Textures/` 切图目录,把 Prefab 拖到 Canvas 下即可。

## 命名规则

PSD 中的图层命名决定生成的节点类型与名字前缀:

| PSD 图层名 | 生成节点 | 组件 |
|---|---|---|
| `btn_关闭` | `Btn_关闭` | Image + Button(命名为 btn_ 的分组也会整组挂 Button) |
| 文本图层 | `Label_xxx` | Text 或 TextMeshProUGUI(内容取自 PSD 文本) |
| 普通图片图层 | `Image_xxx` | Image |
| 分组 / 其它 | `Node_xxx` | 空 RectTransform(分组不透明度 < 100% 时附加 CanvasGroup) |

原图层名里的 `btn_`、`img_`、`image_`、`label_`、`txt_` 等前缀会被剥掉后再加上统一前缀;重名自动加序号。

## 转换规则

- 坐标:锚点居中,按 PSD 文档坐标换算 anchoredPosition,尺寸 = 图层包围盒。
- 层级顺序:与 PSD 一致(PSD 下层 = Hierarchy 中靠前的兄弟节点)。
- 隐藏图层、完全透明图层:跳过,不生成节点也不切图。
- 图层不透明度:应用到 Image/Text 的颜色 alpha。
- 文本图层:启用自动字号(BestFit / TMP AutoSize),默认黑色居中;PSD 的字体/字号/颜色不还原,需手动调整。
- Button:transition 为默认 ColorTint,targetGraphic 指向自身 Image/Text。
- 切图:每个栅格图层导出独立 PNG 并自动设置为 Sprite(Single、无 mipmap)。

## 支持范围

支持:8-bit RGB / 灰度 PSD,RAW 与 RLE 压缩,嵌套分组,中文等 Unicode 图层名,智能对象(按其栅格化结果)。

不支持(解析时会给出明确报错或忽略):PSB 大文档、16/32-bit 通道、CMYK 等色彩模式、图层样式(投影/描边等不会渲染,导出前请先栅格化或合并图层)、剪贴蒙版与图层蒙版(忽略)、调整图层(忽略)。

## 目录结构(main / UPM 版)

```
PSD2Prefab/                       (UPM 包根目录 = GitHub 仓库根)
├── package.json                  # UPM 清单 (com.liyu.psd2prefab)
├── Editor/
│   ├── Psd2Prefab.Editor.asmdef  # Editor 程序集;TMP 存在时注入 PSD2PREFAB_TMP 宏
│   ├── Psd/
│   │   ├── PsdReader.cs          # 大端二进制读取器
│   │   ├── PsdLayer.cs           # 图层数据结构
│   │   └── PsdDocument.cs        # PSD 解析器(头/图层记录/分组/文本/像素)
│   ├── PsdPrefabBuilder.cs       # 切图导出 + UGUI 层级构建 + Prefab 保存
│   └── PsdToPrefabWindow.cs      # 编辑器窗口 (Tools/PSD to Prefab)
├── Samples~/Example/sample.psd   # 含嵌套分组/中文图层名/btn_ 命名的测试文件
├── CHANGELOG.md
└── LICENSE.md                    # MIT
```

解析器(`Editor/Psd/`)不依赖 Unity API,已通过与 psd-tools 的逐像素交叉验证(分组嵌套、图层顺序、RLE/RAW 解码、Unicode 图层名、文本提取)。

## 版本历史

见 [CHANGELOG.md](CHANGELOG.md)。两个版本对应两个 tag:

- **`v1.1.0`** → `main`(UPM 版)
- **`v1.0.0`** → `assets-edition`(经典版)

## License

MIT © liyu
