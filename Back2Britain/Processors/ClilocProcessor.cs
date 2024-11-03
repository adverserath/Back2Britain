using Back2Britain.Utility;
using System.IO;

namespace Back2Britain.Processors
{
    public class ClilocProcessor
    {
        public static void ReadCliloc(FileInfo item)
        {
            if (item == null || !item.Exists)
            {
                throw new ArgumentException("File does not exist or is null.", nameof(item));
            }

            byte[] buffer;
            using (var fileStream = new FileStream(item.FullName, FileMode.Open, FileAccess.Read))
            {
                buffer = new byte[fileStream.Length];
                int totalRead = 0;
                int bytesRead;

                while ((bytesRead = fileStream.Read(buffer, totalRead, Math.Min(4096, buffer.Length - totalRead))) > 0)
                {
                    totalRead += bytesRead;
                }
            }

            if (buffer.Length > 3 && buffer[3] == 0x8E)
            {
                Console.WriteLine($"Decoding {item.Name}...");
                byte[] output = BwtDecompress.Decompress(buffer);

                Console.WriteLine($"Writing decompressed data to: {item.FullName}");

                using (var decodedFileStream = new FileStream(item.FullName, FileMode.Create, FileAccess.Write))
                {
                    decodedFileStream.Write(output, 0, output.Length);
                }
            }
            else
            {
                Console.WriteLine($"No decoding required for {item.Name}");
            }
        }
    }
}
