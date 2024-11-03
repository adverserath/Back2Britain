using Back2Britain.Utility;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Back2Britain.IO
{
    public unsafe ref struct StackDataReader
    {
        private const MethodImplOptions IMPL_OPTION = MethodImplOptions.AggressiveInlining;


        private readonly ReadOnlySpan<byte> _data;

        public StackDataReader(ReadOnlySpan<byte> data)
        {
            _data = data;
            Length = data.Length;
            Position = 0;
        }


        public int Position { get; private set; }
        public long Length { get; }
        public readonly int Remaining => (int)(Length - Position);

        public readonly byte this[int index] => _data[index];

        public ReadOnlySpan<byte> Buffer => _data;


        [MethodImpl(IMPL_OPTION)]
        public void Skip(int count)
        {
            Position += count;
        }

        public byte[] ReadArray(int count)
        {
            if (Position + count > Length)
            {
                return Array.Empty<byte>();
            }

            var buf = Buffer.Slice(Position, count).ToArray();
            Position += count;

            return buf;
        }

        [MethodImpl(IMPL_OPTION)]
        public byte ReadUInt8()
        {
            if (Position + sizeof(byte) > Length)
            {
                return 0;
            }

            return _data[Position++];
        }

        [MethodImpl(IMPL_OPTION)]
        public uint ReadUInt32LE()
        {
            if (Position + sizeof(uint) > Length)
            {
                return 0;
            }

            BinaryPrimitives.TryReadUInt32LittleEndian(_data.Slice(Position), out uint v);

            Skip(sizeof(uint));

            return v;
        }

        public int Read(Span<byte> buffer)
        {
            if (Position + buffer.Length > Length)
            {
                return -1;
            }

            _data.Slice(Position, buffer.Length).CopyTo(buffer);
            Skip(buffer.Length);
            return buffer.Length;
        }

        private string ReadRawString(int length, int sizeT, bool safe)
        {
            if (length == 0 || Position + sizeT > Length)
            {
                return string.Empty;
            }

            bool fixedLength = length > 0;
            int remaining = Remaining;
            int size;

            if (fixedLength)
            {
                size = length * sizeT;

                if (size > remaining)
                {
                    size = remaining;
                }
            }
            else
            {
                size = remaining - (remaining & (sizeT - 1));
            }

            ReadOnlySpan<byte> slice = _data.Slice(Position, size);

            int index = GetIndexOfZero(slice, sizeT);
            size = index < 0 ? size : index;

            string result;

            if (size <= 0)
            {
                result = String.Empty;
            }
            else
            {
                result = StringHelper.Cp1252ToString(slice.Slice(0, size));

                if (safe)
                {
                    Span<char> buff = stackalloc char[256];
                    ReadOnlySpan<char> chars = result.AsSpan();

                    ValueStringBuilder sb = new ValueStringBuilder(buff);

                    bool hasDoneAnyReplacements = false;
                    int last = 0;
                    for (int i = 0; i < chars.Length; i++)
                    {
                        if (!StringHelper.IsSafeChar(chars[i]))
                        {
                            hasDoneAnyReplacements = true;
                            sb.Append(chars.Slice(last, i - last));
                            last = i + 1; // Skip the unsafe char
                        }
                    }

                    if (hasDoneAnyReplacements)
                    {
                        // append the rest of the string
                        if (last < chars.Length)
                        {
                            sb.Append(chars.Slice(last, chars.Length - last));
                        }

                        result = sb.ToString();
                    }

                    sb.Dispose();
                }
            }

            Position += Math.Max(size + (!fixedLength && index >= 0 ? sizeT : 0), length * sizeT);

            return result;
        }

        // from modernuo <3
        private string ReadString(Encoding encoding, int length, int sizeT, bool safe)
        {
            if (length == 0 || Position + sizeT > Length)
            {
                return string.Empty;
            }

            bool fixedLength = length > 0;
            int remaining = Remaining;
            int size;

            if (fixedLength)
            {
                size = length * sizeT;

                if (size > remaining)
                {
                    size = remaining;
                }
            }
            else
            {
                size = remaining - (remaining & (sizeT - 1));
            }

            ReadOnlySpan<byte> slice = _data.Slice(Position, size);

            int index = GetIndexOfZero(slice, sizeT);
            size = index < 0 ? size : index;

            string result;

            fixed (byte* ptr = slice)
            {
                result = encoding.GetString(ptr, size);
            }

            if (safe)
            {
                Span<char> buff = stackalloc char[256];
                ReadOnlySpan<char> chars = result.AsSpan();

                ValueStringBuilder sb = new ValueStringBuilder(buff);

                bool hasDoneAnyReplacements = false;
                int last = 0;
                for (int i = 0; i < chars.Length; i++)
                {
                    if (!StringHelper.IsSafeChar(chars[i]))
                    {
                        hasDoneAnyReplacements = true;
                        sb.Append(chars.Slice(last, i - last));
                        last = i + 1; // Skip the unsafe char
                    }
                }

                if (hasDoneAnyReplacements)
                {
                    // append the rest of the string
                    if (last < chars.Length)
                    {
                        sb.Append(chars.Slice(last, chars.Length - last));
                    }

                    result = sb.ToString();
                }

                sb.Dispose();
            }

            Position += Math.Max(size + (!fixedLength && index >= 0 ? sizeT : 0), length * sizeT);

            return result;
        }

        [MethodImpl(IMPL_OPTION)]
        private static int GetIndexOfZero(ReadOnlySpan<byte> span, int sizeT)
        {
            switch (sizeT)
            {
                case 2: return MemoryMarshal.Cast<byte, char>(span).IndexOf('\0') * 2;
                case 4: return MemoryMarshal.Cast<byte, uint>(span).IndexOf((uint)0) * 4;
                default: return span.IndexOf((byte)0);
            }
        }
    }
}
