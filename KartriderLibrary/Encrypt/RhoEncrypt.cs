using System;

namespace KartLibrary.Encrypt;

public static class RhoEncrypt
{
    /// <summary>
    ///     Used to decrypt rho file data, or DataProcessed data.
    /// </summary>
    /// <param name="Data"></param>
    /// <param name="Key"></param>
    /// <returns></returns>
    public static byte[] DecryptData(uint Key, byte[] Data)
    {
        var extendedKey = RhoKey.ExtendKey(Key);
        var output = new byte[Data.Length];
        for (var i = 0; i < Data.Length; i++) output[i] = (byte)(Data[i] ^ extendedKey[i & 63]);
        return output;
    }

    public static void DecryptData(uint Key, byte[] Data, int Offset, int Length)
    {
        if (Offset + Length > Data.Length)
            throw new Exception("Over range.");
        var extendedKey = RhoKey.ExtendKey(Key);
        for (var i = 0; i < Length; i++)
        {
            var index = i + Offset;
            Data[index] = (byte)(Data[index] ^ extendedKey[index & 63]);
        }
    }

    /// <summary>
    ///     Used to decrypt rho header, and block info.
    /// </summary>
    /// <param name="Data"></param>
    /// <param name="Key"></param>
    /// <returns></returns>
    public static unsafe byte[] DecryptHeaderInfo(byte[] Data, uint Key)
    {
        var currentKey = Key;
        uint a = 0;
        var output = new byte[Data.Length];
        fixed (byte* wPtr = output, rPtr = Data)
        {
            var writePtr = (uint*)wPtr;
            var readPtr = (uint*)rPtr;
            for (var i = 0; i < Data.Length >> 2; i++)
            {
                var vector = RhoKey.GetVector(currentKey);
                var curData = readPtr[i];
                curData ^= vector;
                curData ^= a;
                writePtr[i] = curData;
                a += curData;
                currentKey++;
            }
        }

        return output;
    }

    public static byte[] DecryptBlockInfoOld(byte[] Data, byte[] key)
    {
        if (Data.Length != 0x20)
            throw new NotSupportedException("Exception: the length of Data is not 32 bytes.");
        var output = new byte[32];
        for (var i = 0; i < 32; i++)
            output[i] = (byte)(key[i] ^ Data[i]);
        return output;
    }


    public static void EncryptData(uint Key, byte[] Data, int Offset, int Length)
    {
        if (Offset + Length > Data.Length)
            throw new Exception("Over range.");
        var extendedKey = RhoKey.ExtendKey(Key);
        for (var i = 0; i < Length; i++)
        {
            var index = i + Offset;
            Data[index] = (byte)(Data[index] ^ extendedKey[index & 63]);
        }
    }

    /// <summary>
    ///     Used to encrypt rho header, and block info.
    /// </summary>
    /// <param name="Data"></param>
    /// <param name="Key"></param>
    /// <returns></returns>
    public static unsafe byte[] EncryptHeaderInfo(byte[] Data, uint Key)
    {
        var currentKey = Key;
        uint a = 0;
        var output = new byte[Data.Length];
        fixed (byte* wPtr = output, rPtr = Data)
        {
            var writePtr = (uint*)wPtr;
            var readPtr = (uint*)rPtr;
            for (var i = 0; i < Data.Length >> 2; i++)
            {
                var vector = RhoKey.GetVector(currentKey);
                var decData = readPtr[i];
                var encData = readPtr[i];
                encData ^= vector;
                encData ^= a;
                writePtr[i] = encData;
                a += decData;
                currentKey++;
            }
        }

        return output;
    }

    public static byte[] EncryptBlockInfoOld(byte[] Data, byte[] key)
    {
        if (Data.Length != 0x20)
            throw new NotSupportedException("Exception: the length of Data is not 32 bytes.");
        var output = new byte[32];
        for (var i = 0; i < 32; i++)
            output[i] = (byte)(key[i] ^ Data[i]);
        return output;
    }
}