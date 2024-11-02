
using Back2Britain.Utility;

namespace Back2Britain
{
    public class Cliloc
    {
        public static void ReadCliloc(FileInfo item, String dir)
        {
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

                item.MoveTo(dir + "\\backup\\" + item.Name);
                var output = BwtDecompress.Decompress(buf);
                var decodedFile = new FileStream(dir + item.Name, FileMode.Create, FileAccess.Write);
                decodedFile.Write(output, 0, output.Length);
                decodedFile.Close();
            }
        }
    }
}
