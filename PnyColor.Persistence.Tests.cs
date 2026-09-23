using System;
using System.IO;

static class PersistenceTests
{
    public static void Run()
    {
        string prior=Environment.GetEnvironmentVariable("PNY_COLOR_DATA_DIR");
        string isolated=Path.Combine(Path.GetTempPath(),"PNYColor-tests-"+Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("PNY_COLOR_DATA_DIR",isolated);
        try
        {
            ColorPreferences p=ColorPreferences.Parse("v2|#124FFF|0|75|0|4|#0080FF");p.Save();
            if(ColorPreferences.Load().Serialize()!=p.Serialize())throw new Exception("User data round-trip failed.");
            p.Brightness=50;p.Save();
            if(ColorPreferences.Load().Brightness!=50||!File.Exists(ColorPreferences.FilePath+".bak"))throw new Exception("Atomic preference replacement failed.");
            if(Path.GetDirectoryName(ColorPreferences.FilePath)!=isolated)throw new Exception("Preferences escaped test data directory.");
            Console.WriteLine("PASS: per-user preference path, directory creation, atomic save, reload and backup; no user settings changed.");
        }
        finally
        {
            if(File.Exists(Path.Combine(isolated,"cor-salva.txt")))File.Delete(Path.Combine(isolated,"cor-salva.txt"));
            if(File.Exists(Path.Combine(isolated,"cor-salva.txt.bak")))File.Delete(Path.Combine(isolated,"cor-salva.txt.bak"));
            if(Directory.Exists(isolated))Directory.Delete(isolated);
            Environment.SetEnvironmentVariable("PNY_COLOR_DATA_DIR",prior);
        }
    }
}
