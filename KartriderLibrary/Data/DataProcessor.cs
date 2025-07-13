using Ionic.Zlib;
using KartLibrary.Encrypt;
using System;
using System.IO;
using Adler = KartLibrary.IO.Adler;

namespace KartLibrary.Data;

// KR is means "KartRider and Raycity".
public static class DataProcessor
{
    // Extensions
    public static byte[] ReadKRData(this BinaryReader br, int TotalLength)
    {
        var initialPos = br.BaseStream.Position;
        var checkCode = br.ReadByte();
        if (checkCode != 0x53)
            throw new Exception("It is not KRData Format.");
        var ProcessMode = br.ReadByte();
        var Hash = br.ReadUInt32();
        var Encrypted = (ProcessMode & 2) == 2;
        var Compressed = (ProcessMode & 1) == 1;
        var EncryptKey = Encrypted ? br.ReadUInt32() : 0;
        var DecompressSize = Compressed ? br.ReadInt32() : 0;
        var originalData = br.ReadBytes((int)(TotalLength - (br.BaseStream.Position - initialPos)));
        var processedData = originalData;
        if (Encrypted) processedData = RhoEncrypt.DecryptData(EncryptKey, processedData);
        if (Compressed)
            using (var ms = new MemoryStream(processedData))
            {
                processedData = new byte[DecompressSize];
                var zs = new ZlibStream(ms, CompressionMode.Decompress);
                zs.Read(processedData, 0, processedData.Length);
            }

        var CheckHash = Adler.Adler32(0, processedData, 0, processedData.Length);
        if (CheckHash != Hash)
            throw new Exception("Exception: KRData hash is not qualified.");
        return processedData;
    }

    public static int WriteKRData(this BinaryWriter bw, byte[] Data, bool Encrypted, bool Compressed,
        uint EncryptKey = 0)
    {
        var initialPos = bw.BaseStream.Position;
        const byte checkCode = 0x53;
        var ProcessMode = (byte)((Encrypted ? 2 : 0) | (Compressed ? 1 : 0));
        var Hash = Adler.Adler32(0, Data, 0, Data.Length);
        ;
        var DecompressSize = Data.Length;
        var processedData = Data;
        if (Compressed)
        {
            using var memoryStream = new MemoryStream();
            using (var zlib = new ZlibStream(memoryStream, CompressionMode.Compress))
            {
                zlib.Write(processedData, 0, processedData.Length);
            }

            processedData = memoryStream.ToArray();
        }

        if (Encrypted) processedData = RhoEncrypt.DecryptData(EncryptKey, processedData);
        bw.Write(checkCode);
        bw.Write(ProcessMode);
        bw.Write(Hash);
        if (Encrypted)
            bw.Write(EncryptKey);
        if (Compressed)
            bw.Write(DecompressSize);
        bw.Write(processedData);
        return (int)(bw.BaseStream.Position - initialPos);
    }
}