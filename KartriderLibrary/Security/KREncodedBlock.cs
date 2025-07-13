using System;
using System.IO;
using System.Net;
using Ionic.Zlib;
using KartRider.Common.Utilities;
using KartRider.IO.Packet;

namespace KartRider.Common.Security;

public class KREncodedBlock
{
    public enum EncodeFlag : byte
    {
        ZLib = 1,
        KartCrypto = 2
    }

    public static byte[] Decode(byte[] inputBytes)
    {
        byte[] numArray;
        uint num;
        uint num1;
        var inPacket = new InPacket(inputBytes);
        if (inPacket.ReadByte() == 83)
        {
            var encodeFlag = (EncodeFlag)inPacket.ReadByte();
            var num2 = inPacket.ReadUInt();
            if ((int)(encodeFlag & EncodeFlag.KartCrypto) != 0)
                num = inPacket.ReadUInt();
            else
                num = 0;
            var num3 = num;
            if ((int)(encodeFlag & EncodeFlag.ZLib) != 0)
                num1 = inPacket.ReadUInt();
            else
                num1 = 0;
            var num4 = num1;
            var array = inPacket.ReadBytes(inPacket.Available);
            if ((int)(encodeFlag & EncodeFlag.KartCrypto) != 0) array = KRCrypto.ApplyCrypto(array, num3);
            if ((int)(encodeFlag & EncodeFlag.ZLib) != 0)
            {
                if (array[0] == 120 ? false : array[1] != 218) throw new Exception("Invalid magic header! (zlib)");
                var num5 = BitConverter.ToInt32(array, array.Length - 4);
                var numArray1 = new byte[array.Length - 2];
                Buffer.BlockCopy(array, 2, numArray1, 0, numArray1.Length);
                using (var memoryStream = new MemoryStream())
                {
                    using (var memoryStream1 = new MemoryStream(numArray1))
                    {
                        using (var deflateStream = new DeflateStream(memoryStream1, CompressionMode.Decompress))
                        {
                            deflateStream.CopyTo(memoryStream);
                            deflateStream.Close();
                            array = memoryStream.ToArray();
                        }
                    }
                }

                if (num4 != (ulong)array.Length) throw new Exception("Length was not equal");
                if (Adler32Helper.GenerateSimpleAdler32(array) != IPAddress.HostToNetworkOrder(num5))
                    throw new Exception("Invalid checksum!");
            }

            if (Adler32Helper.GenerateAdler32(array) != num2) throw new Exception("Checksums didnt match.");
            if ((int)(encodeFlag & EncodeFlag.ZLib) == 0 ? false : num4 != (ulong)array.Length)
                throw new Exception("Lengths did not match");
            numArray = array;
        }
        else
        {
            numArray = inputBytes;
        }

        return numArray;
    }

    public static byte[] Encode(byte[] inputBytes, EncodeFlag flag, uint? kartCryptoKey)
    {
        byte[] array;
        using (var outPacket = new OutPacket())
        {
            outPacket.WriteByte(83);
            outPacket.WriteByte((byte)flag);
            outPacket.WriteUInt(Adler32Helper.GenerateAdler32(inputBytes));
            if ((int)(flag & EncodeFlag.KartCrypto) != 0)
            {
                if (!kartCryptoKey.HasValue) kartCryptoKey = Adler32Helper.GenerateAdler32(inputBytes);
                outPacket.WriteUInt(kartCryptoKey.Value);
            }

            if ((int)(flag & EncodeFlag.ZLib) != 0)
                using (var memoryStream = new MemoryStream())
                {
                    using (var binaryWriter = new BinaryWriter(memoryStream))
                    {
                        using (var memoryStream1 = new MemoryStream())
                        {
                            using (var deflateStream = new DeflateStream(memoryStream1, CompressionMode.Compress))
                            {
                                using (var memoryStream2 = new MemoryStream(inputBytes))
                                {
                                    memoryStream2.CopyTo(deflateStream);
                                }

                                deflateStream.Close();
                                outPacket.WriteInt(inputBytes.Length);
                                var numArray = memoryStream1.ToArray();
                                binaryWriter.Write(new byte[] { 120, 218 });
                                binaryWriter.Write(numArray);
                                binaryWriter.Write(
                                    IPAddress.HostToNetworkOrder(Adler32Helper.GenerateSimpleAdler32(inputBytes)));
                                inputBytes = memoryStream.ToArray();
                            }
                        }
                    }
                }

            if ((int)(flag & EncodeFlag.KartCrypto) != 0)
                inputBytes = KRCrypto.ApplyCrypto(inputBytes, kartCryptoKey.Value);
            outPacket.WriteBytes(inputBytes);
            array = outPacket.ToArray();
        }

        return array;
    }
}