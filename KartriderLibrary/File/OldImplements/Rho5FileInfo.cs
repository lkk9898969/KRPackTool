using System.Diagnostics;
using System.IO;
using System.Text;
using Ionic.Zlib;
using KartLibrary.Encrypt;

namespace KartLibrary.File;

public class Rho5FileInfo
{
    internal Rho5 BaseRho5 { get; init; }
    public string FullPath { get; set; }
    public int Offset { get; set; }
    public int CompressedSize { get; set; }
    public int DecompressedSize { get; set; }
    public int Unknown { get; set; }
    public int FileInfoChecksum { get; set; }
    public byte[] Key { get; set; }

    public byte[] GetData()
    {
        var data = new byte[CompressedSize];
        var outdata = new byte[DecompressedSize];
        var decryptKey = Rho5Key.GetPackedFileKey(Key, Rho5Key.GetFileKey_U1(BaseRho5.anotherData), FullPath);
        var decryptStream = new Rho5DecryptStream(BaseRho5.BaseStream, decryptKey);
        decryptStream.Seek(Offset * 0x400 + BaseRho5.DataBaseOffset, SeekOrigin.Begin);
        decryptStream.Read(data, 0, data.Length >= 0x400 ? 0x400 : data.Length);
        if (data.Length >= 0x400)
            BaseRho5.BaseStream.Read(data, 0x400, data.Length - 0x400);
        new Rho5DecryptStream(new MemoryStream(data), decryptKey).Read(data, 0, data.Length);
        using var memoryStream = new MemoryStream(data);
        new ZlibStream(memoryStream, CompressionMode.Decompress).Read(outdata, 0, outdata.Length);
        return outdata;
    }

    private void dump_data(byte[] data)
    {
        var sb = new StringBuilder();
        foreach (var b in data)
            sb.Append($"{b:x2} ");
        sb.Append("\n");
        Debug.Write(sb.ToString());
    }
}