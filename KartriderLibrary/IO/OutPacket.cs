using System;
using System.IO;
using System.Text;

namespace KartLibrary.IO;

public class OutPacket : IDisposable
{
    private MemoryStream m_stream;

    public OutPacket(int size = 64)
    {
        m_stream = new MemoryStream(size);
        Disposed = false;
    }


    public bool Disposed { get; private set; }

    public int Length => (int)m_stream.Position;

    public int Position
    {
        get => (int)m_stream.Position;
        set => m_stream.Position = value;
    }

    public new void Dispose()
    {
        Disposed = true;
        if (m_stream != null) m_stream.Dispose();
        m_stream = null;
    }

    private void Append(long value, int byteCount)
    {
        for (var i = 0; i < byteCount; i++)
        {
            m_stream.WriteByte((byte)value);
            value >>= 8;
        }
    }

    private void ThrowIfDisposed()
    {
        if (Disposed) throw new ObjectDisposedException(GetType().FullName);
    }

    public byte[] ToArray()
    {
        ThrowIfDisposed();
        return m_stream.ToArray();
    }


    public void WriteByte(byte value = 0)
    {
        ThrowIfDisposed();
        m_stream.WriteByte(value);
    }

    public void WriteInt(int value = 0)
    {
        ThrowIfDisposed();
        Append(value, 4);
    }

    public void WriteShort(short value = 0)
    {
        ThrowIfDisposed();
        Append(value, 2);
    }

    public void WriteString(string value)
    {
        if (value == null) throw new ArgumentNullException("value");
        WriteInt(value.Length);
        WriteString(value, value.Length);
    }

    public void WriteString(string value, int length)
    {
        int i;
        if (value == null || length < 0 ? true : length > value.Length) throw new ArgumentNullException("value");
        var bytes = Encoding.Unicode.GetBytes(value);
        for (i = 0; (i < value.Length) & (i < length); i++)
        {
            var num = i * 2;
            WriteByte(bytes[num]);
            WriteByte(bytes[num + 1]);
        }

        while (i < length)
        {
            WriteShort();
            i++;
        }
    }
}