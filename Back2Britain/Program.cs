using Back2Britain;
using Back2Britain.Utility;

void ProcessDirectory()
{
    DirectoryInfo uo = new DirectoryInfo(Directory.GetCurrentDirectory());

    Console.WriteLine("Directory: " + uo.FullName);
    uo.CreateSubdirectory("backup");
    Thread.Sleep(1000);

    List<string> files = uo.EnumerateFiles("cliloc.*").Select(x => x.FullName).ToList();
    files.AddRange(
        uo.EnumerateFiles("gumpartLegacyMUL.uop").Select(x => x.FullName).ToList()
        );
    ZipFiles.Zip(files, uo.FullName + $"\\backup\\compressed-{DateTime.Now.Ticks}.zip");


    foreach (var item in uo.EnumerateFiles("cliloc.*"))
    {
        Cliloc.ReadCliloc(item);
    }

    foreach (var item in uo.EnumerateFiles("gumpartLegacyMUL.uop"))
    {
        CustomUOFile? gumpData = null;

        using (FileStream fs = new FileStream(item.FullName, FileMode.Open))
        {
            gumpData = new CustomUOFile(fs, item, true);

        }
        gumpData.ProcessFile();

    }
    Console.WriteLine(Environment.NewLine + "---Complete---");
    Console.ReadLine();

}

try
{
    ProcessDirectory();
}
catch (Exception)
{
    throw;
}
finally {  Console.Read(); }
