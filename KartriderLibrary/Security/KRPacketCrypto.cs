using System;

namespace KartRider.Common.Security;

public static class KRPacketCrypto
{
    public static uint HashDecrypt(byte[] pData, uint nLength, uint nKey)
    {
        var num = nKey ^ 347277256;
        var num1 = nKey ^ 2361332396;
        var num2 = nKey ^ 604215233;
        var num3 = nKey ^ 4089260480;
        var num4 = 0;
        uint num5 = 0;
        var i = 0;
        for (i = 0; (ulong)i < nLength >> 4; i++)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4) ^ num), 0, pData, num4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 4) ^ num1), 0, pData, num4 + 4,
                4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 8) ^ num2), 0, pData, num4 + 8,
                4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 12) ^ num3), 0, pData, num4 + 12,
                4);
            num5 = num5 ^ BitConverter.ToUInt32(pData, num4 + 12) ^ BitConverter.ToUInt32(pData, num4 + 8) ^
                   BitConverter.ToUInt32(pData, num4 + 4) ^ BitConverter.ToUInt32(pData, num4);
            num4 += 16;
        }

        i *= 16;
        num4 = 0;
        var bytes = BitConverter.GetBytes(num);
        var numArray = BitConverter.GetBytes(num1);
        var bytes1 = BitConverter.GetBytes(num2);
        var numArray1 = BitConverter.GetBytes(num3);
        var numArray2 = new byte[16];
        Buffer.BlockCopy(bytes, 0, numArray2, 0, 4);
        Buffer.BlockCopy(numArray, 0, numArray2, 4, 4);
        Buffer.BlockCopy(bytes1, 0, numArray2, 8, 4);
        Buffer.BlockCopy(numArray1, 0, numArray2, 12, 4);
        while ((ulong)i < nLength)
        {
            ref var numPointer = ref pData[i];
            numPointer = (byte)(numPointer ^ numArray2[num4]);
            num5 = (uint)(num5 ^ (pData[i] << (num4 & 31)));
            i++;
            num4++;
        }

        return num5;
    }

    public static uint HashEncrypt(byte[] pData, uint nLength, uint nKey)
    {
        var num = nKey ^ 347277256;
        var num1 = nKey ^ 2361332396;
        var num2 = nKey ^ 604215233;
        var num3 = nKey ^ 4089260480;
        var num4 = 0;
        uint num5 = 0;
        var i = 0;
        for (i = 0; (ulong)i < nLength >> 4; i++)
        {
            num5 = num5 ^ BitConverter.ToUInt32(pData, num4 + 12) ^ BitConverter.ToUInt32(pData, num4 + 8) ^
                   BitConverter.ToUInt32(pData, num4 + 4) ^ BitConverter.ToUInt32(pData, num4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4) ^ num), 0, pData, num4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 4) ^ num1), 0, pData, num4 + 4,
                4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 8) ^ num2), 0, pData, num4 + 8,
                4);
            Buffer.BlockCopy(BitConverter.GetBytes(BitConverter.ToUInt32(pData, num4 + 12) ^ num3), 0, pData, num4 + 12,
                4);
            num4 += 16;
        }

        i *= 16;
        num4 = 0;
        var bytes = BitConverter.GetBytes(num);
        var numArray = BitConverter.GetBytes(num1);
        var bytes1 = BitConverter.GetBytes(num2);
        var numArray1 = BitConverter.GetBytes(num3);
        var numArray2 = new byte[16];
        Buffer.BlockCopy(bytes, 0, numArray2, 0, 4);
        Buffer.BlockCopy(numArray, 0, numArray2, 4, 4);
        Buffer.BlockCopy(bytes1, 0, numArray2, 8, 4);
        Buffer.BlockCopy(numArray1, 0, numArray2, 12, 4);
        while ((ulong)i < nLength)
        {
            num5 = (uint)(num5 ^ (pData[i] << (num4 & 31)));
            ref var numPointer = ref pData[i];
            numPointer = (byte)(numPointer ^ numArray2[num4]);
            i++;
            num4++;
        }

        return num5;
    }
}