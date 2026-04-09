using System;
using System.IO;
using System.Text;
using KartLibrary.Consts;
using KartRider;

namespace KRPackTool;

public static class Program
{
    private const string CountryCodeFile = "CountryCode.ini";
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

}