using System;
using System.IO;
using System.Text;
using System.Xml;
using KartLibrary.IO;

namespace KartLibrary.Xml;

public class BinaryXmlDocument
{
    public BinaryXmlDocument()
    {
        RootTag = new BinaryXmlTag();
    }

    public BinaryXmlTag RootTag { get; private set; }

    public void Read(Encoding encoding, byte[] array)
    {
        var tag = new BinaryXmlTag();
        using (var ms = new MemoryStream(array))
        {
            var br = new BinaryReader(ms);
            RootTag = br.ReadBinaryXmlTag(encoding);
        }
    }

    public void ReadFromXml(string XML)
    {
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(XML);
        if (xmlDoc.ChildNodes.Count < 1)
            throw new Exception("there are no any nodes in this XML document.");
        if (xmlDoc.ChildNodes.Count > 1)
            throw new Exception("there are more than one root nodes in this XML document.");
        RootTag = (BinaryXmlTag)(xmlDoc.ChildNodes[0] ?? throw new Exception(""));
    }

    public void ReadFromXml(byte[] EncodedXML)
    {
        var xmlDoc = new XmlDocument();
        using (var ms = new MemoryStream(EncodedXML))
        {
            xmlDoc.Load(ms);
            if (xmlDoc.ChildNodes.Count < 1)
                throw new Exception("there are no any nodes in this XML document.");
            if (xmlDoc.ChildNodes.Count > 1)
                throw new Exception("there are more than one root nodes in this XML document.");
            RootTag = (BinaryXmlTag)(xmlDoc.ChildNodes[0] ?? throw new Exception(""));
        }
    }
}