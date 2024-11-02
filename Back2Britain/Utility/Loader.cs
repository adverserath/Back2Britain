using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back2Britain.Utility
{
    public class Loader
    {
        public static void DisplayLoadingBar(string title, int current, int total)
        {
            int barWidth = 50; // Width of the loading bar
            double percent = (double)current / total;
            int completedBlocks = (int)(percent * barWidth);

            // Create the loading bar string
            string loadingBar = new string('#', completedBlocks).PadRight(barWidth);

            // Display the progress
            Console.Write($"\r{title} [{loadingBar}] {percent:P0}");
        }
        public static void NewBar()
        {
            Console.WriteLine();
        }
    }
}
