namespace Back2Britain
{
    internal class Chunk
    {
        private List<Block> blocks;

        public Chunk(List<Block> blocks)
        {
            this.blocks = blocks;
        }

        public List<Block> Blocks
        {
            get { return blocks; }
        }

        public long nextblock { get {
                return blocks.Last().Offset + blocks.Last().DecompressedLength;
            } }
        public int Count
        {
            get
            {
                return blocks.Count;
            }
        }
    }
}
