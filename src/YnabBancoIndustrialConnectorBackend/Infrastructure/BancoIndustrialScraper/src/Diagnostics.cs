using System.Runtime.InteropServices;
using Microsoft.Playwright;

namespace BancoIndustrialScraper;

public class Diagnostics
{
  public static void RunDiagnostics()
  {
    Console.WriteLine("base directory: {0}", AppContext.BaseDirectory);
    DirectoryInfo assemblyDirectory = new(AppContext.BaseDirectory);
    Console.WriteLine("assembly directory exists: {0}, {1}", assemblyDirectory.FullName, assemblyDirectory.Exists);
    Console.WriteLine("dll exists in assembly directory: {0}, {1}",
      Path.Combine(assemblyDirectory.FullName, "Microsoft.Playwright.dll"),
      File.Exists(Path.Combine(assemblyDirectory.FullName,
        "Microsoft.Playwright.dll")));
    if (!assemblyDirectory.Exists || !File.Exists(Path.Combine(assemblyDirectory.FullName, "Microsoft.Playwright.dll")))
    {
      var assemblyLocation = typeof(Playwright).Assembly.Location;
      assemblyDirectory = new FileInfo(assemblyLocation).Directory;
      Console.WriteLine("assembly directory exists 2: {0}, {1}", assemblyDirectory.FullName, assemblyDirectory.Exists);
    }

    string executableFile = GetPath(assemblyDirectory.FullName);
    Console.WriteLine("executableFile exists: {0}, {1}", executableFile, File.Exists(executableFile));
  }
  
  private static string GetPath(string driversPath)
  {
    string platformId;
    string runnerName;
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
      platformId = "win32_x64";
      runnerName = "playwright.cmd";
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
    {
      runnerName = "playwright.sh";
      platformId = "mac";
    }
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    {
      runnerName = "playwright.sh";
      platformId = "linux";
    }
    else
    {
      throw new PlaywrightException("Unknown platform");
    }

    return Path.Combine(driversPath, ".playwright", "node", platformId, runnerName);
  }
}
