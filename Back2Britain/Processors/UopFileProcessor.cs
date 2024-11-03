using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back2Britain.Processors
{
    internal class UopFileProcessor
    {
        public static void ProcessFile(FileInfo item)
        {
            CustomUOFile? gumpData = null;

            using (FileStream fs = new FileStream(item.FullName, FileMode.Open))
            {
                gumpData = new CustomUOFile(fs, item, true);

            }
            gumpData.ProcessFile();
        }
    }
}
