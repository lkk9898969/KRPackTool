namespace KartRider.Common.Utilities;

public sealed class ByteArraySegment
{
    public ByteArraySegment(byte[] pBuffer, bool pEncrypted)
    {
        Buffer = pBuffer;
        Length = Buffer.Length;
        Encrypted = pEncrypted;
    }

    public ByteArraySegment(byte[] pBuffer, int pStart, int pLength)
    {
        Buffer = pBuffer;
        Start = pStart;
        Length = pLength;
    }

    public byte[] Buffer { get; set; }

    public bool Encrypted { get; } = true;

    public int Length { get; private set; }

    public int Start { get; private set; }

    public bool Advance(int pLength)
    {
        Start += pLength;
        Length -= pLength;
        return Length > 0 ? false : true;
    }
}