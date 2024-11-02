using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back2Britain.Utility
{
    public class ZipFiles
    {
        public static void Zip(List<string> files, string zipPath)
        {
            using (var zipStream = new FileStream(zipPath, FileMode.Create))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create))
            {
                foreach (var file in files)
                {
                    if (File.Exists(file))
                    {
                        archive.CreateEntryFromFile(file, Path.GetFileName(file));
                    }
                    else
                    {
                        Console.WriteLine($"File not found: {file}");
                    }
                }
            }
            Console.WriteLine($"Files have been zipped to: {zipPath}");
        }
    }
}
