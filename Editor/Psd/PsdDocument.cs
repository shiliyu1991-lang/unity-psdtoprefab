using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Psd2Prefab
{
    /// <summary>
    /// 纯 C# 的 PSD 解析器(无外部依赖)。
    /// 支持:8-bit RGB/灰度、图层分组(lsct)、Unicode 图层名(luni)、
    /// 文本图层内容(TySh)、不透明度/可见性、RAW 与 RLE 压缩的图层像素。
    /// 不支持:PSB(大文档)、16/32-bit 深度、CMYK 等其它色彩模式。
    /// </summary>
    public sealed class PsdDocument
    {
        public int Width, Height, Channels, Depth, ColorMode;

        /// <summary>顶层图层树,顺序为自下而上(index 0 = 最底层)。</summary>
        public List<PsdLayer> RootLayers = new List<PsdLayer>();

        public static PsdDocument Load(string path)
        {
            return Parse(File.ReadAllBytes(path));
        }

        public static PsdDocument Parse(byte[] bytes)
        {
            var r = new PsdReader(bytes);
            var doc = new PsdDocument();

            // ---- 文件头 (26 字节) ----
            if (r.Ascii(4) != "8BPS") throw new InvalidDataException("不是有效的 PSD 文件(缺少 8BPS 签名)");
            int version = r.U16();
            if (version != 1) throw new InvalidDataException("不支持 PSB(大文档格式),请在 PS 中另存为 PSD");
            r.Skip(6);
            doc.Channels = r.U16();
            doc.Height = (int)r.U32();
            doc.Width = (int)r.U32();
            doc.Depth = r.U16();
            doc.ColorMode = r.U16();
            if (doc.Depth != 8) throw new InvalidDataException($"仅支持 8-bit 通道深度(当前 {doc.Depth}-bit),请在 PS 中转换为 8 位/通道");
            if (doc.ColorMode != 3 && doc.ColorMode != 1)
                throw new InvalidDataException($"仅支持 RGB / 灰度模式(当前模式 {doc.ColorMode}),请在 PS 中转换为 RGB 颜色");

            // ---- 色彩模式数据段(跳过) ----
            r.Skip(r.U32());
            // ---- 图像资源段(跳过) ----
            r.Skip(r.U32());

            // ---- 图层与蒙版信息段 ----
            long layerMaskLen = r.U32();
            long layerMaskEnd = r.Pos + layerMaskLen;
            if (layerMaskLen > 0)
            {
                long layerInfoLen = r.U32();
                long layerInfoEnd = r.Pos + layerInfoLen;
                if (layerInfoLen > 0)
                {
                    int layerCount = Math.Abs((int)r.I16());
                    var flat = new List<PsdLayer>(layerCount); // 文件顺序:自下而上
                    for (int i = 0; i < layerCount; i++)
                        flat.Add(ReadLayerRecord(r));

                    // 通道图像数据,与图层记录同序
                    foreach (var layer in flat)
                        ReadChannelImageData(r, layer);

                    doc.RootLayers = BuildTree(flat);
                }
                r.Pos = (int)layerInfoEnd;
            }
            r.Pos = (int)Math.Min(layerMaskEnd, r.Length);
            // 其后为合成图数据,无需读取
            return doc;
        }

        // ------------------------------------------------------------------
        private static PsdLayer ReadLayerRecord(PsdReader r)
        {
            var layer = new PsdLayer();
            layer.Top = r.I32();
            layer.Left = r.I32();
            layer.Bottom = r.I32();
            layer.Right = r.I32();

            int channelCount = r.U16();
            for (int c = 0; c < channelCount; c++)
            {
                int id = r.I16();
                long len = r.U32();
                layer.ChannelInfos.Add(new KeyValuePair<int, long>(id, len));
                if (id == -1) layer.HasAlphaChannel = true;
            }

            string sig = r.Ascii(4);
            if (sig != "8BIM") throw new InvalidDataException("图层记录混合模式签名错误,文件可能损坏");
            layer.BlendMode = r.Ascii(4);
            layer.Opacity = r.U8();
            r.U8(); // clipping
            byte flags = r.U8();
            layer.Visible = (flags & 0x02) == 0;
            r.U8(); // filler

            long extraLen = r.U32();
            long extraEnd = r.Pos + extraLen;

            r.Skip(r.U32());            // 图层蒙版数据
            r.Skip(r.U32());            // 混合范围
            layer.Name = r.PascalString(4); // 旧版图层名

            // 附加信息块
            while (r.Pos + 12 <= extraEnd)
            {
                string blockSig = r.Ascii(4);
                if (blockSig != "8BIM" && blockSig != "8B64") break;
                string key = r.Ascii(4);
                long blockLen = r.U32();
                long paddedLen = (blockLen + 1) & ~1L; // 补齐到偶数
                long blockEnd = r.Pos + paddedLen;

                switch (key)
                {
                    case "luni":
                        layer.Name = r.UnicodeString();
                        break;
                    case "lsct":
                        layer.SectionType = (int)r.U32();
                        break;
                    case "TySh":
                        layer.TextContent = ExtractTextFromTySh(r.Bytes((int)blockLen));
                        break;
                }
                r.Pos = (int)blockEnd;
            }
            r.Pos = (int)extraEnd;
            return layer;
        }

        // ------------------------------------------------------------------
        private static void ReadChannelImageData(PsdReader r, PsdLayer layer)
        {
            int w = layer.Width, h = layer.Height;
            bool wantPixels = !layer.IsGroup && !layer.IsSectionEnd && w > 0 && h > 0;
            byte[] rCh = null, gCh = null, bCh = null, aCh = null;

            foreach (var info in layer.ChannelInfos)
            {
                int id = info.Key;
                long dataLen = info.Value; // 含 2 字节压缩标志
                long chanEnd = r.Pos + dataLen;
                bool want = wantPixels && (id == 0 || id == 1 || id == 2 || id == -1);
                if (want && dataLen >= 2)
                {
                    int compression = r.U16();
                    byte[] plane = DecodeChannel(r, compression, w, h, chanEnd);
                    if (plane != null)
                    {
                        switch (id)
                        {
                            case 0: rCh = plane; break;
                            case 1: gCh = plane; break;
                            case 2: bCh = plane; break;
                            case -1: aCh = plane; break;
                        }
                    }
                }
                r.Pos = (int)chanEnd;
            }

            if (!wantPixels || rCh == null) return;

            int n = w * h;
            var rgba = new byte[n * 4];
            for (int i = 0; i < n; i++)
            {
                rgba[i * 4 + 0] = rCh[i];
                rgba[i * 4 + 1] = gCh != null ? gCh[i] : rCh[i]; // 灰度时复用
                rgba[i * 4 + 2] = bCh != null ? bCh[i] : rCh[i];
                rgba[i * 4 + 3] = aCh != null ? aCh[i] : (byte)255;
            }
            layer.Rgba = rgba;
        }

        private static byte[] DecodeChannel(PsdReader r, int compression, int w, int h, long chanEnd)
        {
            int n = w * h;
            if (compression == 0) // RAW
            {
                if (r.Pos + n > chanEnd) return null;
                return r.Bytes(n);
            }
            if (compression == 1) // RLE (PackBits)
            {
                var rowLens = new int[h];
                for (int y = 0; y < h; y++) rowLens[y] = r.U16();
                var plane = new byte[n];
                for (int y = 0; y < h; y++)
                {
                    int outPos = y * w;
                    int rowEnd = r.Pos + rowLens[y];
                    while (r.Pos < rowEnd && outPos < (y + 1) * w)
                    {
                        sbyte ctrl = (sbyte)r.U8();
                        if (ctrl >= 0)
                        {
                            int count = ctrl + 1;
                            for (int k = 0; k < count && outPos < n; k++) plane[outPos++] = r.U8();
                        }
                        else if (ctrl != -128)
                        {
                            int count = 1 - ctrl;
                            byte v = r.U8();
                            for (int k = 0; k < count && outPos < n; k++) plane[outPos++] = v;
                        }
                    }
                    r.Pos = rowEnd;
                }
                return plane;
            }
            return null; // ZIP 等其它压缩不支持
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// 从 TySh(文字工具对象)块中提取文本内容。
        /// 直接扫描描述符中的 "Txt " 键(类型 TEXT,后跟 Unicode 字符串),
        /// 避免实现完整的描述符解析。
        /// </summary>
        private static string ExtractTextFromTySh(byte[] block)
        {
            // 模式: 'Txt ' 'TEXT' u32(字符数) UTF-16BE...
            byte[] pat = Encoding.ASCII.GetBytes("Txt TEXT");
            for (int i = 0; i + pat.Length + 4 <= block.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < pat.Length; j++)
                    if (block[i + j] != pat[j]) { match = false; break; }
                if (!match) continue;

                int p = i + pat.Length;
                int count = (block[p] << 24) | (block[p + 1] << 16) | (block[p + 2] << 8) | block[p + 3];
                p += 4;
                if (count < 0 || p + count * 2 > block.Length) continue;
                var sb = new StringBuilder(count);
                for (int k = 0; k < count; k++)
                {
                    char ch = (char)((block[p + k * 2] << 8) | block[p + k * 2 + 1]);
                    if (ch == '\r') ch = '\n';
                    if (ch != '\0') sb.Append(ch);
                }
                return sb.ToString();
            }
            return ""; // 是文本图层但没解出内容
        }

        // ------------------------------------------------------------------
        /// <summary>
        /// 将文件顺序(自下而上)的扁平图层列表组装为树。
        /// 文件顺序中:type=3 的隐藏分隔层在分组内容“下方”,分组本体(type=1/2)在“上方”。
        /// 自上而下遍历:遇到分组本体入栈,遇到分隔层出栈。
        /// 返回的 Children 顺序为自下而上(index 0 = 最底层)。
        /// </summary>
        private static List<PsdLayer> BuildTree(List<PsdLayer> flatBottomUp)
        {
            var root = new List<PsdLayer>();
            var stack = new Stack<PsdLayer>();

            for (int i = flatBottomUp.Count - 1; i >= 0; i--) // 自上而下
            {
                var layer = flatBottomUp[i];
                if (layer.IsSectionEnd)
                {
                    if (stack.Count > 0) stack.Pop();
                    continue;
                }
                if (stack.Count > 0) stack.Peek().Children.Add(layer);
                else root.Add(layer);
                if (layer.IsGroup) stack.Push(layer);
            }

            ReverseDeep(root);
            return root;
        }

        private static void ReverseDeep(List<PsdLayer> layers)
        {
            layers.Reverse();
            foreach (var l in layers)
                if (l.Children.Count > 0) ReverseDeep(l.Children);
        }
    }
}
