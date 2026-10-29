using System;
using System.Collections.Generic;
using System.IO;
using KartLibrary.Consts;
using KartLibrary.File;

namespace KRPackToolGUI;

/// <summary>Result of scanning a single .rho5 file for its internal file structure.</summary>
public sealed class Rho5Result
{
    public string FilePath { get; init; }

    /// <summary>File name only, e.g. <c>DataPack2_00000.rho5</c>. Used as the grouping key.</summary>
    public string FileName { get; init; }

    /// <summary>Region actually used to decrypt this file's name table.</summary>
    public CountryCode Region { get; set; }

    /// <summary>Internal paths using '/' as separator, e.g. <c>aaa/bbb.xml</c>. Sorted.</summary>
    public List<string> InternalPaths { get; set; } = new();

    /// <summary>Non-null when the file could not be opened / parsed.</summary>
    public string Error { get; set; }
}

/// <summary>
/// Reads the file structure of .rho5 archives through the same parser that
/// <c>RhoPacker.decode</c> uses: <see cref="PackFolderManager.OpenSingleFile"/> feeds
/// <c>Rho5</c>, and the resulting <see cref="PackFolderInfo"/> tree is walked here.
/// </summary>
public static class Rho5Scanner
{
    /// <param name="regionChoice">"AUTO", or a <see cref="CountryCode"/> name such as "KR" / "CN" / "TW".</param>
    public static Rho5Result Scan(string path, string regionChoice)
    {
        var result = new Rho5Result
        {
            FilePath = path,
            FileName = Path.GetFileName(path)
        };

        result.Region = string.Equals(regionChoice, "AUTO", StringComparison.OrdinalIgnoreCase)
            ? ResolveRegion(path, CountryCode.TW)
            : Enum.Parse<CountryCode>(regionChoice, ignoreCase: true);

        var manager = new PackFolderManager();
        try
        {
            manager.OpenSingleFile(path, result.Region);
            var acc = new List<string>();
            Collect(manager.GetRootFolder(), acc);
            acc.Sort(StringComparer.OrdinalIgnoreCase);
            result.InternalPaths = acc;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }
        finally
        {
            try { manager.Reset(); } catch { /* best-effort stream cleanup */ }
        }

        return result;
    }

    private static void Collect(PackFolderInfo folder, List<string> acc)
    {
        foreach (var file in folder.GetFilesInfo())
            acc.Add(file.FullName);
        foreach (var sub in folder.GetFoldersInfo())
            Collect(sub, acc);
    }

    /// <summary>Mirrors <c>Program.parseCountryCode</c>: a CountryCode.ini next to the file wins.</summary>
    private static CountryCode ResolveRegion(string rho5Path, CountryCode fallback)
    {
        try
        {
            var dir = Path.GetDirectoryName(rho5Path);
            if (dir is not null)
            {
                var ini = Path.Combine(dir, "CountryCode.ini");
                if (File.Exists(ini) &&
                    Enum.TryParse<CountryCode>(File.ReadAllText(ini).Trim(), ignoreCase: true, out var cc))
                    return cc;
            }
        }
        catch { /* fall through */ }

        return fallback;
    }
}
