
using Back2Britain.Utility;
using System.IO;

namespace Back2Britain
{
    public class Cliloc
    {
        public static void ReadCliloc(FileInfo item)
        {
            string backupLocation = Path.Combine(item.Directory.FullName, "backup", item.Name);

            using var fileStream = new FileStream(item.FullName, FileMode.Open, FileAccess.Read);

            int bytesRead;
            var totalRead = 0;
            var buf = new byte[fileStream.Length];
            while ((bytesRead = fileStream.Read(buf, totalRead, Math.Min(4096, buf.Length - totalRead))) > 0)
                totalRead += bytesRead;
            fileStream.Close();

            if (buf[3] == 0x8E)
            {
                Console.WriteLine("Decoding " + item.Name);
                Console.WriteLine("Moving to:" + backupLocation);
                Thread.Sleep(1000);

                var output = BwtDecompress.Decompress(buf);
                Console.WriteLine($"Writing {Path.Combine(item.Directory.FullName, item.Name)}");

                var decodedFile = new FileStream(Path.Combine(item.Directory.FullName, item.Name), FileMode.Create, FileAccess.Write);
                decodedFile.Write(output, 0, output.Length);
                decodedFile.Close();
            }
        }
    }
}
