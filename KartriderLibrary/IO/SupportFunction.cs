using System.IO;
using System.Text;
using KartLibrary.Xml;

namespace KartLibrary.IO;

internal static class SupportFunction
{
    public static string ReadText(this BinaryReader br, Encoding encoding)
    {
        var count = br.ReadInt32() << 1;
        var data = br.ReadBytes(count);
        return encoding.GetString(data);
    }

    public static BinaryXmlTag ReadBinaryXmlTag(this BinaryReader br, Encoding encoding)
    {
        var tag = new BinaryXmlTag();
        tag.Name = br.ReadText(encoding);
        //Text
        tag.Text = br.ReadText(encoding);
        //Attributes
        var attCount = br.ReadInt32();
        for (var i = 0; i < attCount; i++)
            tag.SetAttribute(br.ReadText(encoding), br.ReadText(encoding));
        //SubTags
        var SubCount = br.ReadInt32();
        for (var i = 0; i < SubCount; i++)
            tag.Children.Add(br.ReadBinaryXmlTag(encoding));
        return tag;
    }

    public static void WriteNullTerminatedText(this BinaryWriter br, string text, bool wideString)
    {
        if (!wideString)
        {
            var encData = Encoding.ASCII.GetBytes(text);
            br.Write(encData);
            br.Write((byte)0x00);
        }
        else
        {
            var encData = Encoding.Unicode.GetBytes(text);
            br.Write(encData);
            br.Write((short)0x00);
        }
    }

    public static void WriteKRString(this BinaryWriter bw, string str)
    {
        var len = str.Length;
        var strData = Encoding.GetEncoding("UTF-16").GetBytes(str);
        bw.Write(len);
        bw.Write(strData);
    }
}