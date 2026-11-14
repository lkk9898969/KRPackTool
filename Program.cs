using System;
using System.IO;
using System.Text;
using KartLibrary.Consts;
using KartRider;

namespace KRPackTool;

public static class Program
{
    private const string CountryCodeFile = "CountryCode.ini";
    private const string PackToolFile = "PackTool";
    public static CountryCode CC = CountryCode.TW;

    [STAThread]
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
        var Load_CC = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CountryCodeFile);
        if (File.Exists(Load_CC))
        {
            var textValue = File.ReadAllText(Load_CC);
            CC = (CountryCode)Enum.Parse(typeof(CountryCode), textValue);
        }
        else
        {
            using (var streamWriter = new StreamWriter(Load_CC, false))
            {
                streamWriter.Write(CC.ToString());
            }
        }

        RhoPacker.PackTool(args);
    }

    internal static CountryCode parseCountryCode(string dir)
    {
        var Load_CC = Path.Combine(dir, CountryCodeFile);
        var cc = CC;
        if (File.Exists(Load_CC))
        {
            var textValue = File.ReadAllText(Load_CC);
            var success = Enum.TryParse(textValue, true, out cc);
            if (success)
                return cc;
        }

        Load_CC = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CountryCodeFile);
        if (File.Exists(Load_CC))
        {
            var textValue = File.ReadAllText(Load_CC);
            cc = (CountryCode)Enum.Parse(typeof(CountryCode), textValue);
        }

        return cc;
    }

    // 讀取 PackTool 設定檔中「是否建立自己名稱的資料夾」的設定（key 為 RHO5 / LPK）。
    // 搜尋順序仿照 CountryCode.ini：先找 arg 所在目錄，再找程式目錄；都沒有有效值時預設為 true。
    internal static bool parseCreateOwnFolder(string dir, string key)
    {
        foreach (var baseDir in new[] { dir ?? string.Empty, AppDomain.CurrentDomain.BaseDirectory })
        {
            var value = ReadIniValue(Path.Combine(baseDir, PackToolFile), key);
            if (value != null && bool.TryParse(value, out var result))
                return result;
        }

        return true;
    }

    // 簡易 .ini 讀取：忽略空行、註解（; 或 #）與 [Section]，key 不分大小寫。
    private static string ReadIniValue(string path, string key)
    {
        if (!File.Exists(path))
            return null;
        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == ';' || line[0] == '#' || line[0] == '[')
                continue;
            var eq = line.IndexOf('=');
            if (eq <= 0)
                continue;
            if (string.Equals(line.Substring(0, eq).Trim(), key, StringComparison.OrdinalIgnoreCase))
                return line.Substring(eq + 1).Trim();
        }

        return null;
    }

}