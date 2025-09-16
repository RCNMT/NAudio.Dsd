using System.Text;

namespace NAudio.Dsd
{
    internal static class BinaryReaderExtensions
    {
        public static string ReadString(this BinaryReader br, int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            byte[] bytes = br.ReadBytes(count);
            if (bytes.Length < count) throw new EndOfStreamException();
            return Encoding.ASCII.GetString(bytes);
        }

        public static ushort ReadUInt16BE(this BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(2);
            if (bytes.Length < 2) throw new EndOfStreamException();
            Array.Reverse(bytes);
            return BitConverter.ToUInt16(bytes, 0);
        }

        public static uint ReadUInt32BE(this BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(4);
            if (bytes.Length < 4) throw new EndOfStreamException();
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public static ulong ReadUInt64BE(this BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(8);
            if (bytes.Length < 8) throw new EndOfStreamException();
            Array.Reverse(bytes);
            return BitConverter.ToUInt64(bytes, 0);
        }

        public static short ReadInt16BE(this BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(2);
            if (bytes.Length < 2) throw new EndOfStreamException();
            Array.Reverse(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        public static int ReadInt32BE(this BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(4);
            if (bytes.Length < 4) throw new EndOfStreamException();
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static long ReadInt64BE(this BinaryReader br)
        {
            byte[] bytes = br.ReadBytes(8);
            if (bytes.Length < 8) throw new EndOfStreamException();
            Array.Reverse(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }
    }
}
