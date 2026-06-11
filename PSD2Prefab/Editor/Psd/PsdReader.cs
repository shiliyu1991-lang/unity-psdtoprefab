using System;
using System.Text;

namespace Psd2Prefab
{
    /// <summary>大端字节序读取器(PSD 文件全部为大端)。</summary>
    internal sealed class PsdReader
    {
        private readonly byte[] _d;
        public int Pos;

        public PsdReader(byte[] data, int pos = 0) { _d = data; Pos = pos; }

        public int Length => _d.Length;
        public int Remaining => _d.Length - Pos;

        public byte U8() { return _d[Pos++]; }

        public ushort U16()
        {
            ushort v = (ushort)((_d[Pos] << 8) | _d[Pos + 1]);
            Pos += 2;
            return v;
        }

        public uint U32()
        {
            uint v = ((uint)_d[Pos] << 24) | ((uint)_d[Pos + 1] << 16) | ((uint)_d[Pos + 2] << 8) | _d[Pos + 3];
            Pos += 4;
            return v;
        }

        public short I16() { return (short)U16(); }
        public int I32() { return (int)U32(); }

        public string Ascii(int n)
        {
            string s = Encoding.ASCII.GetString(_d, Pos, n);
            Pos += n;
            return s;
        }

        public byte[] Bytes(int n)
        {
            byte[] b = new byte[n];
            Buffer.BlockCopy(_d, Pos, b, 0, n);
            Pos += n;
            return b;
        }

        public void Skip(long n) { Pos += (int)n; }

        /// <summary>Pascal 字符串:1 字节长度 + 内容,整体补齐到 pad 的倍数。</summary>
        public string PascalString(int pad)
        {
            int start = Pos;
            int len = U8();
            string s = Encoding.UTF8.GetString(_d, Pos, len);
            Pos += len;
            int total = Pos - start;
            int rem = total % pad;
            if (rem != 0) Pos += pad - rem;
            return s;
        }

        /// <summary>Unicode 字符串:4 字节字符数 + UTF-16BE 内容。</summary>
        public string UnicodeString()
        {
            int count = (int)U32();
            var sb = new StringBuilder(count);
            for (int i = 0; i < count; i++)
            {
                char c = (char)U16();
                if (c != '\0') sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
