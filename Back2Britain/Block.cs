namespace Back2Britain
{
    public class Block(long offset, int headerLength, int compressedLength, int decompressedLength, ulong hash, uint data_hash, short flag, byte[] header, byte[] data, string pattern, int extra1, int extra2)
    {
        private byte[] buf;
        public long Offset { get; set; } = offset;
        public int HeaderLength { get; set; } = headerLength;
        public int CompressedLength { get; set; } = compressedLength;
        public int DecompressedLength { get; set; } = decompressedLength;
        public ulong Hash { get; set; } = hash;
        public uint Data_Hash { get; set; } = data_hash;
        public short Flag { get; set; } = flag;
        public byte[] Header { get; set; } = header;
        public byte[] Data { get; set; } = data;
        public string Pattern{ get; set; } = pattern;
        public int Extra1 { get; set; } = extra1;
        public int Extra2 { get; set; } = extra2;

    }
}