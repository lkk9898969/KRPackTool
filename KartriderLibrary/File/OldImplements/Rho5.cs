using KartLibrary.Consts;
using KartLibrary.Encrypt;
using KartLibrary.IO;
using System;
using System.IO;
using System.Text;

namespace KartLibrary.File;

public class Rho5 : IDisposable
{
    internal string anotherData = "";
    internal int DataBaseOffset;

    public Rho5()
    {
    }

    public Rho5(string FileName, CountryCode region)
    {
        BaseStream = new FileStream(FileName, FileMode.Open);
        var fileInfo = new FileInfo(FileName);
        anotherData = "";
        switch (region)
        {
            case CountryCode.KR:
                anotherData = "y&errfV6GRS!e8JL";
                break;
            case CountryCode.CN:
                anotherData = "d$Bjgfc8@dH4TQ?k";
                break;
            case CountryCode.TW:
                anotherData = "t5rHKg-g9BA7%=qD";
                break;
        }

        var decryptStream = new Rho5DecryptStream(BaseStream, fileInfo.Name, anotherData);
        var br = new BinaryReader(decryptStream);
        var headerOffset = GetHeaderOffset(fileInfo.Name);
        var fileNameOffset = headerOffset + GetFileNamesOffset(fileInfo.Name);
        decryptStream.Seek(headerOffset, SeekOrigin.Begin);
        var packageHeaderCrc = br.ReadInt32(); //d40
        PackageVersion = br.ReadByte(); //d39
        var fileCounts = br.ReadInt32(); //d48
        if (packageHeaderCrc != PackageVersion + fileCounts)
            throw new Exception("rho5 header crc mismatch.");
        decryptStream.Seek(fileNameOffset, SeekOrigin.Begin);
        //decryptStream.SetToFileInfoKey(fileInfo.Name, "t5rHKg-g9BA7%=qD"); china: d$Bjgfc8@dH4TQ?k korea: y&errfV6GRS!e8JL taiwan: t5rHKg-g9BA7%=qD
        decryptStream.SetToFilesInfoKey(fileInfo.Name, anotherData);
        Files = new Rho5FileInfo[fileCounts];
        for (var i = 0; i < fileCounts; i++)
        {
            var file = new Rho5FileInfo
            {
                FullPath = br.ReadText(Encoding.GetEncoding("UTF-16")),
                FileInfoChecksum = br.ReadInt32(),
                Unknown = br.ReadInt32(),
                Offset = br.ReadInt32(),
                DecompressedSize = br.ReadInt32(),
                CompressedSize = br.ReadInt32(),
                Key = br.ReadBytes(0x10),
                BaseRho5 = this
            };
            Files[i] = file;
        }

        DataBaseOffset = (((int)decryptStream.Position + 0x3FF) >> 10) << 10;
    }

    public byte PackageVersion { get; set; }
    public Stream BaseStream { get; set; }
    public Rho5FileInfo[] Files { get; } = new Rho5FileInfo[0];

    public void Dispose()
    {
        if (BaseStream is null)
            return;
        BaseStream.Close();
        BaseStream.Dispose();
    }

    private int GetHeaderOffset(string name)
    {
        name = name.ToLower();
        var sum = 0;
        foreach (var c in name) sum += c;
        var mpl = (sum * 0xA41A41A5L) >> 32;
        var result = sum - (int)mpl;
        result >>= 1;
        result += (int)mpl;
        result >>= 8;
        result *= 0x138;
        result = sum - result + 0x1E;
        return result;
    }

    private int GetFileNamesOffset(string name)
    {
        name = name.ToLower();
        var sum = 0;
        foreach (var c in name) sum += c;
        sum *= 3;
        var mpl = (sum * 0x3521CFB3L) >> 32;
        var result = sum - (int)mpl;
        result >>= 1;
        result += (int)mpl;
        result >>= 7;
        result *= 0xD4;
        result = sum - result + 0x2A;
        return result;
    }
}