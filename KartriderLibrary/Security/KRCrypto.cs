using System;

namespace KartRider.Common.Security;

public class KRCrypto
{
    public static byte[] ApplyCrypto(byte[] input, uint key)
    {
        var numArray = new byte[input.Length];
        Buffer.BlockCopy(input, 0, numArray, 0, input.Length);
        var i = 0;
        var numArray1 = new uint[17];
        var numArray2 = new byte[68];
        numArray1[0] = key ^ 2222193601;
        for (i = 1; i < 16; i++) numArray1[i] = numArray1[i - 1] - 2072773695;
        for (i = 0; i <= 16; i++) Buffer.BlockCopy(BitConverter.GetBytes(numArray1[i]), 0, numArray2, i * 4, 4);
        for (i = 0; i + 64 <= numArray.Length; i += 64)
        for (var j = 0; j < 16; j++)
            Buffer.BlockCopy(BitConverter.GetBytes(numArray1[j] ^ BitConverter.ToUInt32(numArray, i + 4 * j)), 0,
                numArray, i + 4 * j, 4);

        for (var k = i; k < numArray.Length; k++) numArray[k] = (byte)(numArray[k] ^ numArray2[k - i]);
        return numArray;
    }
}