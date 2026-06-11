# PSD2Prefab — A Unity Editor plugin that converts PSD to UGUI Prefab

**English** · [中文](./README.zh-CN.md)

Convert Photoshop **PSD** files into **UGUI Prefabs** with one click: every layer is sliced into a Sprite, groups rebuild the hierarchy and coordinates, text layers generate Text / TextMeshPro components, and `btn_` layers automatically get a Button attached. **Pure C# PSD parsing — no Photoshop install required, and no external dependencies.**

[![Unity](https://img.shields.io/badge/Unity-2021.3%20LTS%20~%206.3%20LTS-black)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.md)
[![UPM](https://img.shields.io/badge/UPM-v1.1.0-success)](package.json)

---

## Repository layout: one repo, two editions (two branches)

| Branch | Version | Form | When to use |
|---|---|---|---|
| **`main`** (default) | **v1.1.0 UPM edition** | Standard UPM package (`package.json` / `asmdef` / `Samples~`) | **Recommended.** Install via Git URL in the Package Manager; supports Unity 6 + TextMeshPro |
| **`assets-edition`** | v1.0.0 classic edition | Source dropped straight into `Assets/` | Older projects that don't want a package manager; Legacy Text only |

> This README describes the **main branch (UPM edition)**. For the classic edition docs, see the [`assets-edition` branch](../../tree/assets-edition).

**Supports Unity 2021.3 LTS ~ Unity 6.3 LTS (6000.3).** On Unity 6, text layers can use TextMeshPro by default.

---

## Installation (UPM edition / main branch)

**Option 1 (recommended, UPM Git URL):** Open **Window → Package Manager → + → Add package from git URL...** and enter:

```
https://github.com/shiliyu1991-lang/unity-psdtoprefab.git
```

> `main` is the default branch, so you don't need to specify it. To pin to a specific version, append `#v1.1.0` to the URL.

**Option 2 (copy directly):** Drop the entire `Editor` folder together with `package.json` into some directory in your project (for example `Assets/PSD2Prefab/`).
> If you just want the simplest "copy into Assets, zero package management" form, use the classic edition on the [`assets-edition` branch](../../tree/assets-edition) instead.

All the code lives in an Editor assembly, so it won't be included in your game build. The sample PSD can be imported from the **Samples** tab in the Package Manager (or, with the direct-copy approach, from `Samples~/Example/sample.psd`).

---

## Usage

1. Open the window from the menu bar via **Tools → PSD to Prefab**.
2. Select a PSD file (click "Browse", or drag a .psd directly onto the window).
3. Set the output directory (default `Assets/PSD2Prefab_Output`); if your project has TextMeshPro (built in on Unity 6), you can check "Use TextMeshPro".
4. Click **Generate Prefab**: it outputs `Node_<psdName>.prefab` plus a `<psdName>_Textures/` slice directory. Just drag the Prefab under a Canvas.

## Naming rules

Layer names in the PSD determine the generated node type and the name prefix:

| PSD layer name | Generated node | Component |
|---|---|---|
| `btn_关闭` | `Btn_关闭` | Image + Button (a group named `btn_` also gets a Button attached to the whole group) |
| Text layer | `Label_xxx` | Text or TextMeshProUGUI (content taken from the PSD text) |
| Regular image layer | `Image_xxx` | Image |
| Group / other | `Node_xxx` | Empty RectTransform (a CanvasGroup is added when the group's opacity is < 100%) |

Prefixes in the original layer name such as `btn_`, `img_`, `image_`, `label_`, `txt_` are stripped off and then replaced with a unified prefix; duplicate names get an automatic index.

## Conversion rules

- Coordinates: anchor centered, `anchoredPosition` computed from the PSD document coordinates, size = the layer's bounding box.
- Sibling order: matches the PSD (a lower layer in the PSD = an earlier sibling node in the Hierarchy).
- Hidden layers and fully transparent layers: skipped — no node is generated and no slice is exported.
- Layer opacity: applied to the alpha of the Image/Text color.
- Text layers: auto font sizing is enabled (BestFit / TMP AutoSize), defaulting to black and centered; the PSD's font/size/color are not restored and must be adjusted manually.
- Button: transition is the default ColorTint, and targetGraphic points to the node's own Image/Text.
- Slicing: each rasterized layer is exported as a separate PNG and automatically set as a Sprite (Single, no mipmap).

## Supported scope

Supported: 8-bit RGB / grayscale PSD, RAW and RLE compression, nested groups, Unicode layer names (Chinese, etc.), and smart objects (using their rasterized result).

Not supported (the parser will give a clear error or ignore them): PSB large documents, 16/32-bit channels, CMYK and other color modes, layer styles (drop shadow, stroke, etc. are not rendered — rasterize or merge the layers before exporting), clipping masks and layer masks (ignored), and adjustment layers (ignored).

## Directory structure (main / UPM edition)

```
PSD2Prefab/                       (UPM package root = GitHub repo root)
├── package.json                  # UPM manifest (com.liyu.psd2prefab)
├── Editor/
│   ├── Psd2Prefab.Editor.asmdef  # Editor assembly; injects the PSD2PREFAB_TMP define when TMP is present
│   ├── Psd/
│   │   ├── PsdReader.cs          # Big-endian binary reader
│   │   ├── PsdLayer.cs           # Layer data structures
│   │   └── PsdDocument.cs        # PSD parser (header / layer records / groups / text / pixels)
│   ├── PsdPrefabBuilder.cs       # Slice export + UGUI hierarchy build + Prefab save
│   └── PsdToPrefabWindow.cs      # Editor window (Tools/PSD to Prefab)
├── Samples~/Example/sample.psd   # Test file with nested groups / Chinese layer names / btn_ naming
├── CHANGELOG.md
└── LICENSE.md                    # MIT
```

The parser (`Editor/Psd/`) does not depend on the Unity API, and has been cross-validated pixel-by-pixel against psd-tools (group nesting, layer order, RLE/RAW decoding, Unicode layer names, text extraction).

## Version history

See [CHANGELOG.md](CHANGELOG.md). The two editions correspond to two tags:

- **`v1.1.0`** → `main` (UPM edition)
- **`v1.0.0`** → `assets-edition` (classic edition)

## License

MIT © liyu
