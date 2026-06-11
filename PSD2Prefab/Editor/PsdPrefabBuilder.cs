using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Psd2Prefab
{
    /// <summary>把解析后的 PsdDocument 生成为 UGUI Prefab。</summary>
    public static class PsdPrefabBuilder
    {
        private static readonly string[] StripPrefixes =
            { "btn_", "img_", "image_", "label_", "lbl_", "txt_", "text_", "node_" };

        /// <summary>
        /// 生成 Prefab。返回生成的 Prefab 资源路径。
        /// </summary>
        /// <param name="psdPath">PSD 文件绝对路径或项目内路径</param>
        /// <param name="outputFolder">输出目录(必须在 Assets 下),如 Assets/PSD2Prefab_Output</param>
        public static string Build(string psdPath, string outputFolder)
        {
            if (!outputFolder.Replace('\\', '/').StartsWith("Assets"))
                throw new ArgumentException("输出目录必须在 Assets 目录内");

            var doc = PsdDocument.Load(psdPath);

            string psdName = Sanitize(Path.GetFileNameWithoutExtension(psdPath));
            string texFolder = $"{outputFolder}/{psdName}_Textures";
            EnsureFolder(outputFolder);
            EnsureFolder(texFolder);

            // 根节点:RectTransform,尺寸 = 文档尺寸
            var rootGo = new GameObject("Node_" + psdName, typeof(RectTransform));
            try
            {
                var rootRt = rootGo.GetComponent<RectTransform>();
                rootRt.sizeDelta = new Vector2(doc.Width, doc.Height);

                var usedNames = new Dictionary<string, int>();
                int total = CountLayers(doc.RootLayers);
                int done = 0;
                BuildChildren(doc, doc.RootLayers, rootRt, texFolder, usedNames, ref done, total);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{outputFolder}/Node_{psdName}.prefab");
                PrefabUtility.SaveAsPrefabAsset(rootGo, prefabPath);
                return prefabPath;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootGo);
                EditorUtility.ClearProgressBar();
            }
        }

        // ------------------------------------------------------------------
        private static void BuildChildren(PsdDocument doc, List<PsdLayer> layers, RectTransform parent,
            string texFolder, Dictionary<string, int> usedNames, ref int done, int total)
        {
            // layers 顺序为自下而上;UGUI 中后创建的兄弟节点渲染在上层,顺序一致,直接顺序创建。
            foreach (var layer in layers)
            {
                done++;
                if (!layer.Visible) continue; // 跳过隐藏图层(含整组)

                EditorUtility.DisplayProgressBar("PSD2Prefab",
                    $"处理图层 {layer.Name} ({done}/{total})", total > 0 ? (float)done / total : 0f);

                if (layer.IsGroup)
                {
                    var go = NewUINode(NodeName("Node_", layer.Name, usedNames), parent);
                    var rt = go.GetComponent<RectTransform>();
                    // 分组使用全屏拉伸,使其中心与文档中心一致,子节点可统一用文档坐标换算
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    if (layer.Opacity < 255)
                    {
                        var cg = go.AddComponent<CanvasGroup>();
                        cg.alpha = layer.Opacity / 255f;
                    }
                    bool isBtnGroup = HasPrefix(layer.Name, "btn_");
                    BuildChildren(doc, layer.Children, rt, texFolder, usedNames, ref done, total);
                    if (isBtnGroup)
                    {
                        go.name = NodeName("Btn_", layer.Name, usedNames);
                        var btn = go.AddComponent<Button>();
                        var img = go.GetComponentInChildren<Image>();
                        if (img != null) { btn.targetGraphic = img; img.raycastTarget = true; }
                    }
                    continue;
                }

                if (layer.IsText)
                {
                    CreateTextNode(doc, layer, parent, usedNames);
                    continue;
                }

                if (layer.Rgba == null || layer.Width <= 0 || layer.Height <= 0) continue;
                if (layer.IsFullyTransparent) continue; // 忽略完全透明的图层

                CreateImageNode(doc, layer, parent, texFolder, usedNames);
            }
        }

        // ------------------------------------------------------------------
        private static void CreateImageNode(PsdDocument doc, PsdLayer layer, RectTransform parent,
            string texFolder, Dictionary<string, int> usedNames)
        {
            bool isButton = HasPrefix(layer.Name, "btn_");
            string nodeName = NodeName(isButton ? "Btn_" : "Image_", layer.Name, usedNames);

            Sprite sprite = ExportSprite(layer, texFolder, nodeName);

            var go = NewUINode(nodeName, parent);
            PlaceRect(doc, layer, go.GetComponent<RectTransform>());

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = new Color(1f, 1f, 1f, layer.Opacity / 255f);
            img.raycastTarget = isButton;

            if (isButton)
            {
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
            }
        }

        private static void CreateTextNode(PsdDocument doc, PsdLayer layer, RectTransform parent,
            Dictionary<string, int> usedNames)
        {
            string nodeName = NodeName(HasPrefix(layer.Name, "btn_") ? "Btn_" : "Label_", layer.Name, usedNames);
            var go = NewUINode(nodeName, parent);
            var rt = go.GetComponent<RectTransform>();
            PlaceRect(doc, layer, rt);
            // 文本图层 bounds 往往偏紧,稍微放大避免截断
            rt.sizeDelta = new Vector2(layer.Width + 8, layer.Height + 8);

            var text = go.AddComponent<Text>();
            text.text = string.IsNullOrEmpty(layer.TextContent) ? layer.Name : layer.TextContent;
            text.font = GetDefaultFont();
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 8;
            text.resizeTextMaxSize = Mathf.Max(10, layer.Height);
            text.color = new Color(0f, 0f, 0f, layer.Opacity / 255f);
            text.raycastTarget = false;

            if (HasPrefix(layer.Name, "btn_"))
            {
                var btn = go.AddComponent<Button>();
                btn.targetGraphic = text;
                text.raycastTarget = true;
            }
        }

        // ------------------------------------------------------------------
        private static Sprite ExportSprite(PsdLayer layer, string texFolder, string baseName)
        {
            int w = layer.Width, h = layer.Height;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            // PSD 行序自上而下,Unity 自下而上 → 垂直翻转
            for (int y = 0; y < h; y++)
            {
                int srcRow = y * w;
                int dstRow = (h - 1 - y) * w;
                for (int x = 0; x < w; x++)
                {
                    int s = (srcRow + x) * 4;
                    pixels[dstRow + x] = new Color32(layer.Rgba[s], layer.Rgba[s + 1], layer.Rgba[s + 2], layer.Rgba[s + 3]);
                }
            }
            tex.SetPixels32(pixels);
            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{texFolder}/{baseName}.png");
            File.WriteAllBytes(assetPath, png);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        // ------------------------------------------------------------------
        private static GameObject NewUINode(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return go;
        }

        /// <summary>按 PSD 文档坐标(y 向下)换算 anchoredPosition(锚点居中)。</summary>
        private static void PlaceRect(PsdDocument doc, PsdLayer layer, RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float cx = (layer.Left + layer.Right) * 0.5f;
            float cy = (layer.Top + layer.Bottom) * 0.5f;
            rt.anchoredPosition = new Vector2(cx - doc.Width * 0.5f, doc.Height * 0.5f - cy);
            rt.sizeDelta = new Vector2(layer.Width, layer.Height);
        }

        private static Font GetDefaultFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null)
            {
                try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            }
            return f;
        }

        private static bool HasPrefix(string name, string prefix)
        {
            return name != null && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>去掉原始命名前缀 + 加上类型前缀 + 去非法字符 + 去重。</summary>
        private static string NodeName(string typePrefix, string rawName, Dictionary<string, int> usedNames)
        {
            string n = (rawName ?? "").Trim();
            foreach (var p in StripPrefixes)
            {
                if (n.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                {
                    n = n.Substring(p.Length);
                    break;
                }
            }
            n = Sanitize(n);
            if (string.IsNullOrEmpty(n)) n = "Unnamed";
            string full = typePrefix + n;
            if (usedNames.TryGetValue(full, out int count))
            {
                usedNames[full] = count + 1;
                return $"{full}_{count + 1}";
            }
            usedNames[full] = 0;
            return full;
        }

        private static string Sanitize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c > 127) sb.Append(c);
                else if (c == ' ') sb.Append('_');
            }
            return sb.ToString();
        }

        private static void EnsureFolder(string path)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static int CountLayers(List<PsdLayer> layers)
        {
            int n = 0;
            foreach (var l in layers) { n++; n += CountLayers(l.Children); }
            return n;
        }
    }
}
