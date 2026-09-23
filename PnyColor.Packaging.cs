using System;
using System.IO;
using System.Reflection;

// All bridge files travel inside the application, including the portable EXE.
static class BridgePackage
{
    public static string PluginDirectory
    {
        get
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (string.IsNullOrEmpty(documents)) throw new IOException(UiLocale.Get("bridge.documents.error"));
            return Path.Combine(documents, "WhirlwindFX", "Plugins");
        }
    }

    static byte[] Payload(string name)
    {
        using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (input == null) throw new IOException("Missing embedded resource: " + name);
            using (MemoryStream output = new MemoryStream()) { input.CopyTo(output); return output.ToArray(); }
        }
    }

    static bool Same(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }

    internal static bool InstallTo(string directory)
    {
        byte[] plugin = Payload("PNYColor.Bridge.js");
        string target = Path.Combine(directory, "PNY_Color_Bridge.js");
        if (File.Exists(target) && Same(File.ReadAllBytes(target), plugin)) return false;
        Directory.CreateDirectory(directory);
        string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllBytes(temp, plugin);
        try
        {
            // Keep replaced custom/older versions out of SignalRGB's *.js scan.
            if (File.Exists(target)) File.Replace(temp, target, target + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "." + Guid.NewGuid().ToString("N") + ".bak");
            else File.Move(temp, target);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        return true;
    }

    internal static void PreparePreferences(bool signalInstalled)
    {
        PreparePreferences(signalInstalled, File.Exists(AppPaths.LegacyPreferences));
    }

    static void PreparePreferences(bool signalInstalled, bool legacyExists)
    {
        if (File.Exists(ColorPreferences.FilePath) || legacyExists) return;
        ColorPreferences initial = new ColorPreferences();
        initial.White = 0;
        initial.Effect = signalInstalled ? LightEffect.SignalRGB : LightEffect.Estatica;
        initial.Save();
    }

    public static bool Prepare()
    {
        // Detect before creating the plugin directory ourselves.
        bool signalInstalled = Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VortxEngine"))
            || Directory.Exists(Path.GetDirectoryName(PluginDirectory));
        bool changed = InstallTo(PluginDirectory);
        PreparePreferences(signalInstalled);
        return changed;
    }

    public static void SelfTest()
    {
        string root = Path.Combine(Path.GetTempPath(), "PNYColor-package-" + Guid.NewGuid().ToString("N"));
        string pluginFolder = Path.Combine(root, "Documents com espaços e acentos", "Plugins");
        string priorData = Environment.GetEnvironmentVariable("PNY_COLOR_DATA_DIR");
        try
        {
            if (!InstallTo(pluginFolder)) throw new Exception("Fresh bridge installation failed.");
            string target = Path.Combine(pluginFolder, "PNY_Color_Bridge.js");
            if (!Same(File.ReadAllBytes(target), Payload("PNYColor.Bridge.js"))) throw new Exception("Bridge payload differs.");
            if (InstallTo(pluginFolder)) throw new Exception("Identical bridge was unnecessarily replaced.");
            File.WriteAllText(target, "custom previous plugin");
            if (!InstallTo(pluginFolder)) throw new Exception("Bridge repair failed.");
            string[] backups = Directory.GetFiles(pluginFolder, "*.bak");
            if (backups.Length != 1 || File.ReadAllText(backups[0]) != "custom previous plugin") throw new Exception("Previous plugin was not preserved.");
            File.Delete(target);
            if (!InstallTo(pluginFolder)) throw new Exception("Missing plugin was not recovered.");
            Environment.SetEnvironmentVariable("PNY_COLOR_DATA_DIR", Path.Combine(root, "settings"));
            PreparePreferences(false, true);
            if (File.Exists(ColorPreferences.FilePath)) throw new Exception("Legacy preference would be ignored.");
            PreparePreferences(false, false);
            if (ColorPreferences.Load().Effect != LightEffect.Estatica) throw new Exception("Standalone default is not static.");
            string original = File.ReadAllText(ColorPreferences.FilePath);
            PreparePreferences(true, false);
            if (File.ReadAllText(ColorPreferences.FilePath) != original) throw new Exception("Existing selection changed.");
            File.Delete(ColorPreferences.FilePath);
            PreparePreferences(true, false);
            if (ColorPreferences.Load().Effect != LightEffect.SignalRGB || ColorPreferences.Load().White != 0) throw new Exception("Clean integration default failed.");
            Console.WriteLine("PASS: embedded bridge, fresh install, Unicode path, repeat install, backup and missing-file recovery; no GPU access.");
            Console.WriteLine("PASS: clean preferences with/without SignalRGB; saved and legacy selections preserved.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("PNY_COLOR_DATA_DIR", priorData);
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
