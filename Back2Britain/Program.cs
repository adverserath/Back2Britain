using Back2Britain.Processors;
using Back2Britain.Utility;
using QRCoder;

DirectoryInfo uo = new DirectoryInfo(Directory.GetCurrentDirectory());
void ProcessDirectory()
{
    foreach (var item in uo.EnumerateFiles("cliloc.*"))
    {
        ClilocProcessor.ReadCliloc(item);
    }
    try
    {
        foreach (var item in uo.EnumerateFiles("gumpartLegacyMUL.uop"))
        {
            UopFileProcessor.ProcessFile(item);
        }
    }
    catch (Exception error)
    {
        Console.WriteLine(error.Message);
        Console.WriteLine("");
    }
    Loader.NewBar();
    Console.WriteLine("Finished. Remember to run this again on the next updates.");
}

void CreateBackup()
{
    if (uo == null)
    {
        throw new ArgumentNullException(nameof(uo), "Directory cannot be null.");
    }

    Console.WriteLine($"Directory: {uo.FullName}");

    string backupDirPath = Path.Combine(uo.FullName, "backup");
    DirectoryInfo backupDir = uo.CreateSubdirectory("backup");

    var filePatterns = new[] { "cliloc.*", "gumpartLegacyMUL.uop" };

    List<string> filesToBackup = filePatterns
        .SelectMany(pattern => uo.EnumerateFiles(pattern, SearchOption.TopDirectoryOnly))
        .Select(file => file.FullName)
        .ToList();

    if (!filesToBackup.Any())
    {
        Console.WriteLine("No files found to backup.");
        return;
    }

    string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
    string backupFileName = $"compressed-{timestamp}.zip";
    string backupFilePath = Path.Combine(backupDir.FullName, backupFileName);

    try
    {
        ZipFiles.Zip(filesToBackup, backupFilePath);
        Console.WriteLine($"Backup created successfully at: {backupFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while creating the backup: {ex.Message}");
    }
}

void ClosingMessage()
{
    Thread.Sleep(500);
    Console.WriteLine("\n\n");
    Thread.Sleep(1000);
    PrintQR();

}
void PrintQR()
{
    string link = "https://buymeacoffee.com/adverserath";

    using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
    {
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);

        using (AsciiQRCode qrCode = new AsciiQRCode(qrCodeData))
        {
            string qrCodeAscii = qrCode.GetGraphic(1);

            Thread.Sleep(1000);
            Console.WriteLine(link);
            Thread.Sleep(1000);
            Console.WriteLine("\nIf you are looking for a server to play, please consider Heritage at https://trueuo.com/.");
            Thread.Sleep(2500);

            Console.WriteLine(qrCodeAscii);

        }
    }
}
try
{
    CreateBackup();
    ProcessDirectory();
    ClosingMessage();
}
catch (Exception)
{
    throw;
}
finally { Console.Read(); }
