using System.Collections.Generic;

namespace Psd2Prefab
{
    /// <summary>解析后的一个 PSD 图层(或分组)。坐标为 PSD 文档坐标系(y 向下)。</summary>
    public sealed class PsdLayer
    {
        public string Name = "";
        public int Left, Top, Right, Bottom;
        public byte Opacity = 255;
        public bool Visible = true;
        public string BlendMode = "norm";

        /// <summary>lsct: 1=展开分组 2=折叠分组 3=分组结束标记(隐藏图层)</summary>
        public int SectionType;

        /// <summary>文本图层内容(非文本图层为 null)。</summary>
        public string TextContent;

        /// <summary>RGBA 像素(行序自上而下),非栅格图层为 null。</summary>
        public byte[] Rgba;
        public bool HasAlphaChannel;

        /// <summary>子图层,顺序为自下而上(index 0 = 最底层)。</summary>
        public List<PsdLayer> Children = new List<PsdLayer>();

        public int Width => Right - Left;
        public int Height => Bottom - Top;
        public bool IsGroup => SectionType == 1 || SectionType == 2;
        public bool IsSectionEnd => SectionType == 3;
        public bool IsText => TextContent != null;

        /// <summary>每个通道的 (id, 数据长度),供图像数据段按序读取。</summary>
        internal readonly List<KeyValuePair<int, long>> ChannelInfos = new List<KeyValuePair<int, long>>();

        /// <summary>是否完全透明(有 alpha 通道且全为 0)。</summary>
        public bool IsFullyTransparent
        {
            get
            {
                if (Rgba == null || !HasAlphaChannel) return false;
                for (int i = 3; i < Rgba.Length; i += 4)
                    if (Rgba[i] != 0) return false;
                return true;
            }
        }
    }
}
