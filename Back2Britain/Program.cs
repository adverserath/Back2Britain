using Back2Britain;
String dir = String.Empty;


DirectoryInfo uo = new DirectoryInfo(Directory.GetCurrentDirectory());

uo.CreateSubdirectory("backup").Delete(true);
uo.CreateSubdirectory("backup");

foreach (var item in uo.EnumerateFiles("cliloc.*"))
{
    Cliloc.ReadCliloc(item, dir);
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
Console.WriteLine("---Complete---");
Console.ReadLine();


