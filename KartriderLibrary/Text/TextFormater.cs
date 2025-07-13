using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KartLibrary.Text;

public class TextFormater
{
    private readonly List<TextFormat> TextFormats = new();
    public int LevelDelta { get; set; } = 1;


    public void AddString(int Level, TextAlign align, string Text)
    {
        var Lines = Regex.Split(Text, "\\r\\n");
        foreach (var Line in Lines)
            TextFormats.Add(new TextFormat
            {
                Level = Level,
                Text = Line,
                Align = align
            });
    }

    public string StartFormat()
    {
        var TopLine = new List<string>();
        var BottomLine = new List<string>();
        foreach (var tf in TextFormats)
            switch (tf.Align)
            {
                case TextAlign.Top:
                    TopLine.Add($"{"".PadLeft(LevelDelta * tf.Level, ' ')}{tf.Text}");
                    break;
                case TextAlign.Bottom:
                    BottomLine.Add($"{"".PadLeft(LevelDelta * tf.Level, ' ')}{tf.Text}");
                    break;
            }

        var output = new List<string>();
        foreach (var tl in TopLine) output.Add(tl);
        BottomLine.Reverse();
        foreach (var tl in BottomLine) output.Add(tl);
        return string.Join("\r\n", output);
    }
}

public struct TextFormat
{
    public int Level { get; set; }

    public string Text { get; set; }

    public TextAlign Align { get; set; }
}

public enum TextAlign
{
    Top,
    Bottom
}