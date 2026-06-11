# PSD2Prefab (v1.0.0 classic edition) — a Unity editor plugin for converting PSD into UGUI prefabs

**English** · [中文](./README.zh-CN.md)

Convert Photoshop PSD files into UGUI prefabs inside the Unity Editor — pure C# PSD parser, no Photoshop required. (Classic edition: copy into Assets, Unity 2021+)

> You are viewing the **`assets-edition` branch (classic edition)** of this repository. The latest UPM version lives on the [`main` branch](../../tree/main).

**This is the v1.0.0 classic edition**: copy it straight into `Assets/` and go. It targets Unity 2021 LTS and newer, and text uses Legacy Text (no TMP dependency).
If you want Package Manager (Git URL) installation with TextMeshPro support (recommended for Unity 6), switch to this repository's **[`main` branch (v1.1.0 UPM edition)](../../tree/main)**.

## Installation

Copy the `PSD2Prefab` folder from this repository into your project's `Assets/` directory — that's it. All of the code lives in `Editor/`, so none of it ends up in your build.

## Usage

1. Open the window from the menu bar: **Tools → PSD to Prefab**.
2. Pick a PSD file (click "Browse", or drag a .psd straight onto the window).
3. Set the output directory (default `Assets/PSD2Prefab_Output`) and click **Generate Prefab**.
4. Output: `<output dir>/Node_<psdName>.prefab` plus a `<psdName>_Textures/` slice directory. Drag the prefab under a Canvas in your scene and you're ready to go.

## Naming rules

Layer names in the PSD determine the type of the generated node and its name prefix:

| PSD layer name | Generated node | Component |
|---|---|---|
| `btn_关闭` | `Btn_关闭` | Image + Button (a group named `btn_` gets a Button attached to the whole group as well) |
| Text layer | `Label_xxx` | Text (content taken from the PSD text) |
| Regular image layer | `Image_xxx` | Image |
| Group / other | `Node_xxx` | Empty RectTransform (a CanvasGroup is added when the group's opacity is < 100%) |

Prefixes in the original layer name such as `btn_`, `img_`, `image_`, `label_`, `txt_` are stripped off before the unified prefix is applied; duplicate names automatically get a sequence number.

## Conversion rules

- Coordinates: anchors are centered, anchoredPosition is derived from the PSD document coordinates, and the size equals the layer's bounding box.
- Hierarchy order: matches the PSD (a lower layer in the PSD = an earlier sibling node in the Hierarchy).
- Hidden layers and fully transparent layers: skipped — neither a node nor a slice is generated.
- Layer opacity: applied to the alpha of the Image/Text color.
- Text layers: a UGUI Text is generated with BestFit auto-sizing enabled, defaulting to black and center-aligned (the PSD's font/size/color are not reproduced and must be adjusted by hand).
- Button: transition defaults to ColorTint, and targetGraphic points at the node's own Image/Text.
- Slicing: each raster layer is exported as a standalone PNG and automatically set up as a Sprite (Single, no mipmaps).

## Supported scope

Supported: 8-bit RGB / grayscale PSDs, RAW and RLE compression, nested groups, Unicode layer names (Chinese, etc.), and smart objects (using their rasterized result).

Not supported (parsing will either report a clear error or ignore them): PSB large documents, 16/32-bit channels, color modes such as CMYK, layer styles (drop shadow, stroke, etc. are not rendered — rasterize or merge those layers before exporting), clipping masks and layer masks (ignored), and adjustment layers (ignored).

## Directory structure

```
PSD2Prefab/
├── Editor/
│   ├── Psd/
│   │   ├── PsdReader.cs      # Big-endian binary reader
│   │   ├── PsdLayer.cs       # Layer data structures
│   │   └── PsdDocument.cs    # PSD parser (header / layer records / groups / text / pixels)
│   ├── PsdPrefabBuilder.cs   # Slice export + UGUI hierarchy build + Prefab save
│   └── PsdToPrefabWindow.cs  # Editor window (Tools/PSD to Prefab)
└── Example/
    └── sample.psd            # Test file with nested groups / Chinese layer names / btn_ naming
```

The parser (`Editor/Psd/`) does not depend on the Unity API, and has been cross-validated pixel-by-pixel against psd-tools (group nesting, layer order, RLE/RAW decoding, Unicode layer names, and text extraction).

## License

MIT © liyu
