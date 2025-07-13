using System;

namespace KartLibrary.IO;

public static class Adler
{
    public const uint AdlerModulo = 65521;

    public static uint Adler32(uint adler, byte[] buffer, int offset, int count)
    {
        if (buffer.Length < offset + count)
            throw new Exception("buffer is small.");
        var a = adler & 0xFFFFu;
        var b = (adler >> 16) & 0xFFFFu;
        for (var i = 0; i < count; i++)
        {
            a = (a + buffer[offset + i]) % AdlerModulo;
            b = (b + a) % AdlerModulo;
        }

        return (b << 16) | a;
    }

    public static uint Adler32Combine(uint prevChksum, byte[] buffer, int offset, int count)
    {
        var a = prevChksum & 0xFFFFu;
        var b = (prevChksum >> 16) & 0xFFFFu;
        for (var i = 0; i < count; i++)
        {
            a = (a + buffer[offset + i]) % AdlerModulo;
            b = (b + a) % AdlerModulo;
        }

        return (b << 16) | a;
    }
}