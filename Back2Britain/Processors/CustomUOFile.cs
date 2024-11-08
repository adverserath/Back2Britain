﻿using Back2Britain.Utility;
using Back2Britain.IO;

namespace Back2Britain.Processors
{
    internal class CustomUOFile : Utility.FileReader
    {
        private FileInfo _fileInfo;
        private const uint UOP_MAGIC_NUMBER = 0x50594D;
        private readonly bool _hasExtra;
        private readonly string _pattern = "build/gumpartlegacymul/{0:D8}.tga";
        private BinaryReader binaryReader;
        private uint version;
        private uint format_timestamp;
        private long nextBlock;
        private uint block_size;
        private int count;
        private long startingBlock;

        public CustomUOFile(FileStream stream, FileInfo fileInfo, bool hasExtra) : base(stream)
        {
            _hasExtra = hasExtra;
            using (binaryReader = new BinaryReader(stream))
            {
                _fileInfo = fileInfo;
                Console.WriteLine($"Parsing file: {_fileInfo.Name}");
                FillEntries();
            }
        }

        public override BinaryReader Reader => binaryReader;

        public enum CompressionType : ushort
        {
            None,
            Zlib,
            ZlibBwt = 3
        }
        //public List<Block> Blocks = new List<Block>();
        private readonly Dictionary<ulong, Block> _hashes = new Dictionary<ulong, Block>();

        public FileStream Stream { get; }

        private void FillEntries()
        {
            Seek(0, SeekOrigin.Begin);

            if (ReadUInt32() != UOP_MAGIC_NUMBER)
            {
                throw new ArgumentException("Bad uop file");
            }

            version = ReadUInt32();
            format_timestamp = ReadUInt32();
            nextBlock = ReadInt64();
            startingBlock = nextBlock;
            block_size = ReadUInt32();
            count = ReadInt32();


            Seek(nextBlock, SeekOrigin.Begin);
            int total = 0;
            int real_total = 0;
            Loader.NewBar();

            do
            {
                Loader.DisplayLoadingBar("Loading ", real_total, count);
                var filesCount = ReadInt32();
                nextBlock = ReadInt64();
                total += filesCount;

                for (int i = 0; i < filesCount; i++)
                {
                    long offset = ReadInt64();
                    int headerLength = ReadInt32();
                    int compressedLength = ReadInt32();
                    int decompressedLength = ReadInt32();
                    ulong hash = ReadUInt64();
                    uint data_hash = ReadUInt32();
                    short flag = ReadInt16();
                    int length = flag == 1 ? compressedLength : decompressedLength;

                    if (offset == 0)
                    {
                        continue;
                    }

                    real_total++;

                    var offset_data = offset + headerLength;

                    if (flag == 0)
                    {
                        Loader.NewBar();
                        throw new Exception($"{_fileInfo.Name} has data which is already decompressed. Skipping File.");
                    }
                    else if (_hasExtra && flag != 3)
                    {
                        var pos = Position;
                        Seek(offset_data, SeekOrigin.Begin);

                        var extra1 = ReadInt32();
                        var extra2 = ReadInt32();
                        var buf = ReadBytes(compressedLength - 8);
                        Seek(pos, SeekOrigin.Begin);
                        _hashes.Add(hash, new Block(offset, headerLength, compressedLength - 8, decompressedLength, hash, data_hash, flag, [], buf, string.Format(_pattern, real_total - 1), extra1, extra2));

                    }
                    else
                    {
                        long position = Position;
                        Seek(offset, SeekOrigin.Begin);
                        var header = ReadBytes(headerLength);

                        var buf = ReadBytes(compressedLength);
                        Seek(position, SeekOrigin.Begin);

                        _hashes.Add(hash, new Block(offset_data, headerLength, compressedLength, decompressedLength, hash, data_hash, flag, header, buf, string.Format(_pattern, real_total - 1), 0, 0));

                    }
                }

                Seek(nextBlock, SeekOrigin.Begin);
            } while (nextBlock != 0);

        }
        public void ProcessFile()
        {
            if (_hashes.Count > 0)
            {
                Decompress();
                RecalculateData();
                WriteNewFile();
            }
        }

        private void RecalculateData()
        {
            Loader.NewBar();
            long rollingOffset = _hashes.Values.First().Offset;
            List<Block> blocks = _hashes.Values.ToList();
            for (int i = 0; i < blocks.Count; i++)
            {
                Loader.DisplayLoadingBar("Calculating Offsets ", i, blocks.Count);

                if (i != 0 && i % 100 == 0)
                {
                    rollingOffset += 3412; // 100 items per chunk (plus count and nextblock)
                }
                Block? item = blocks[i];
                item.Offset = rollingOffset;
                rollingOffset += item.HeaderLength + item.CompressedLength;
            }
        }

        private void WriteNewFile()
        {

            block_size = 100;
            version = 5;
            string newFileLocation = Path.Combine(_fileInfo.Directory.FullName, _fileInfo.Name);
            Console.WriteLine($"{Environment.NewLine}Writing {newFileLocation}");

            FileStream decompressedFile = new FileStream(newFileLocation, FileMode.Create);
            decompressedFile.Write(BitConverter.GetBytes(UOP_MAGIC_NUMBER));
            decompressedFile.Write(BitConverter.GetBytes(version));
            decompressedFile.Write(BitConverter.GetBytes(format_timestamp));
            decompressedFile.Write(BitConverter.GetBytes(startingBlock));
            decompressedFile.Write(BitConverter.GetBytes(block_size));
            decompressedFile.Write(BitConverter.GetBytes(count));
            decompressedFile.Seek(startingBlock, SeekOrigin.Begin);

            int batchSize = 100;
            int skip = 0;

            List<Chunk> chunks = new List<Chunk>();
            while (true)
            {
                List<Block> batch = _hashes.Values.Skip(skip).Take(batchSize).ToList();
                if (!batch.Any())
                    break;

                chunks.Add(new Chunk(batch));

                skip += batchSize;
            }
            long lastBlock = startingBlock;
            long nextBlockPos = 0;
            int currentCount = 1;
            Loader.NewBar();
            foreach (Chunk chunk in chunks)
            {
                Loader.DisplayLoadingBar("Writing New File ", currentCount++, chunks.Count);

                decompressedFile.Seek(lastBlock, SeekOrigin.Begin);  //Goto chunk start
                decompressedFile.Write(BitConverter.GetBytes(chunk.Count)); //Write Count of chunk
                if (chunks.Last() != chunk)  //Write last chunk as 0 if end
                {
                    nextBlockPos = decompressedFile.Position;   //Record placeholder to update next chunk at the end
                    decompressedFile.Write(BitConverter.GetBytes(startingBlock));
                }
                else
                {
                    decompressedFile.Write(BitConverter.GetBytes(0));
                }

                foreach (Block item in chunk.Blocks)
                {
                    if (item.Flag == 3)
                    {
                        Loader.NewBar();
                        throw new Exception($"{_fileInfo.Name} has data which is decompressed as flag 3.");
                    }
                    else if (_hasExtra && item.Flag != 3)
                    {
                        decompressedFile.Write(BitConverter.GetBytes(item.Offset));
                        decompressedFile.Write(BitConverter.GetBytes(item.HeaderLength));
                        decompressedFile.Write(BitConverter.GetBytes(item.CompressedLength));
                        decompressedFile.Write(BitConverter.GetBytes(item.DecompressedLength));
                        decompressedFile.Write(BitConverter.GetBytes(item.Hash));
                        decompressedFile.Write(BitConverter.GetBytes(item.Data_Hash));
                        decompressedFile.Write(BitConverter.GetBytes(item.Flag));
                        long pos = decompressedFile.Position;
                        decompressedFile.Seek(item.Offset + item.HeaderLength, SeekOrigin.Begin);
                        decompressedFile.Write(BitConverter.GetBytes(item.Extra1));
                        decompressedFile.Write(BitConverter.GetBytes(item.Extra2));
                        decompressedFile.Write(item.Data);
                        lastBlock = decompressedFile.Position;
                        decompressedFile.Seek(pos, SeekOrigin.Begin);
                    }
                }


                decompressedFile.Seek(nextBlockPos, SeekOrigin.Begin);   //Go back to previous nextChunk
                if (chunks.Last() != chunk)
                {
                    //nextBlockPos = oldFile.Position;
                    decompressedFile.Write(BitConverter.GetBytes(lastBlock));
                }
                else
                    decompressedFile.Write(BitConverter.GetBytes(0));
                decompressedFile.Seek(lastBlock, SeekOrigin.Begin);

                startingBlock = lastBlock;
            }

            decompressedFile.Close();
        }

        private void Decompress()
        {
            int totalItems = _hashes.Count;
            int current = 0;
            Loader.NewBar();

            Parallel.ForEach(_hashes.Values, (item) =>
            {
                int localCurrent = Interlocked.Increment(ref current);
                Loader.DisplayLoadingBar("Decompressing", localCurrent, totalItems);

                try
                {
                    byte[] bwtDecoded = new byte[item.DecompressedLength];

                    ZLib.Decompress(item.Data, bwtDecoded);

                    byte[] decompressedData = Utility.BwtDecompress.Decompress(bwtDecoded);

                    StackDataReader reader = new StackDataReader(decompressedData);
                    item.Extra1 = (int)reader.ReadUInt32LE();  // Width
                    item.Extra2 = (int)reader.ReadUInt32LE();  // Height
                    item.Data = reader.ReadArray(reader.Remaining);

                    item.CompressedLength = decompressedData.Length;
                    item.DecompressedLength = decompressedData.Length;
                    item.Flag = 0;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error decompressing item: {ex.Message}");
                }
            });
        }

        public static byte[] TrimEnd(byte[] array)
        {
            int lastIndex = Array.FindLastIndex(array, b => b != 0);

            Array.Resize(ref array, lastIndex + 1);

            return array;
        }
        public static ulong CreateHash(string s)
        {
            uint eax, ecx, edx, ebx, esi, edi;
            eax = ecx = edx = ebx = esi = edi = 0;
            ebx = edi = esi = (uint)s.Length + 0xDEADBEEF;
            int i = 0;

            for (i = 0; i + 12 < s.Length; i += 12)
            {
                edi = (uint)(s[i + 7] << 24 | s[i + 6] << 16 | s[i + 5] << 8 | s[i + 4]) + edi;
                esi = (uint)(s[i + 11] << 24 | s[i + 10] << 16 | s[i + 9] << 8 | s[i + 8]) + esi;
                edx = (uint)(s[i + 3] << 24 | s[i + 2] << 16 | s[i + 1] << 8 | s[i]) - esi;
                edx = edx + ebx ^ esi >> 28 ^ esi << 4;
                esi += edi;
                edi = edi - edx ^ edx >> 26 ^ edx << 6;
                edx += esi;
                esi = esi - edi ^ edi >> 24 ^ edi << 8;
                edi += edx;
                ebx = edx - esi ^ esi >> 16 ^ esi << 16;
                esi += edi;
                edi = edi - ebx ^ ebx >> 13 ^ ebx << 19;
                ebx += esi;
                esi = esi - edi ^ edi >> 28 ^ edi << 4;
                edi += ebx;
            }

            if (s.Length - i > 0)
            {
                switch (s.Length - i)
                {
                    case 12:
                        esi += (uint)s[i + 11] << 24;
                        goto case 11;

                    case 11:
                        esi += (uint)s[i + 10] << 16;
                        goto case 10;

                    case 10:
                        esi += (uint)s[i + 9] << 8;
                        goto case 9;

                    case 9:
                        esi += s[i + 8];
                        goto case 8;

                    case 8:
                        edi += (uint)s[i + 7] << 24;
                        goto case 7;

                    case 7:
                        edi += (uint)s[i + 6] << 16;
                        goto case 6;

                    case 6:
                        edi += (uint)s[i + 5] << 8;
                        goto case 5;

                    case 5:
                        edi += s[i + 4];
                        goto case 4;

                    case 4:
                        ebx += (uint)s[i + 3] << 24;
                        goto case 3;

                    case 3:
                        ebx += (uint)s[i + 2] << 16;
                        goto case 2;

                    case 2:
                        ebx += (uint)s[i + 1] << 8;
                        goto case 1;

                    case 1:
                        ebx += s[i];

                        break;
                }

                esi = (esi ^ edi) - (edi >> 18 ^ edi << 14);
                ecx = (esi ^ ebx) - (esi >> 21 ^ esi << 11);
                edi = (edi ^ ecx) - (ecx >> 7 ^ ecx << 25);
                esi = (esi ^ edi) - (edi >> 16 ^ edi << 16);
                edx = (esi ^ ecx) - (esi >> 28 ^ esi << 4);
                edi = (edi ^ edx) - (edx >> 18 ^ edx << 14);
                eax = (esi ^ edi) - (edi >> 8 ^ edi << 24);

                return (ulong)edi << 32 | eax;
            }

            return (ulong)esi << 32 | eax;
        }
    }

}
