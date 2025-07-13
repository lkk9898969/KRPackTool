using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using KartNew.Utilities;
using KartRider.Common.Utilities;

namespace KartRider.IO.Packet;

public class OutPacket : PacketBase, IDisposable
{
    private MemoryStream m_stream;

    public OutPacket(int size = 64)
    {
        m_stream = new MemoryStream(size);
        Disposed = false;
    }

    public OutPacket(string rttiVal) : this()
    {
        WriteUInt(Adler32Helper.GenerateAdler32(Encoding.ASCII.GetBytes(rttiVal)));
    }

    public bool Disposed { get; private set; }

    public override int Length => (int)m_stream.Position;

    public override int Position
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

    public override byte[] ToArray()
    {
        ThrowIfDisposed();
        return m_stream.ToArray();
    }

    public void WriteBool(bool value)
    {
        ThrowIfDisposed();
        WriteByte((byte)(value ? 1 : 0));
    }

    public void WriteByte(byte value = 0)
    {
        ThrowIfDisposed();
        m_stream.WriteByte(value);
    }

    public void WriteBytes(params byte[] value)
    {
        ThrowIfDisposed();
        m_stream.Write(value, 0, value.Length);
    }

    public void WriteEncByte(byte value)
    {
        WriteBytes(CryptoConstants.encryptBytes(new[] { value }));
    }

    public void WriteEncFloat(float value)
    {
        WriteBytes(CryptoConstants.encryptBytes(BitConverter.GetBytes(value)));
    }

    public void WriteEncInt(int value)
    {
        WriteBytes(CryptoConstants.encryptBytes(BitConverter.GetBytes(value)));
    }

    public void WriteEncUInt(uint value)
    {
        WriteBytes(CryptoConstants.encryptBytes(BitConverter.GetBytes(value)));
    }

    public void WriteEndPoint(IPEndPoint endpoint)
    {
        if (endpoint != null)
        {
            WriteEndPoint(endpoint.Address, (ushort)endpoint.Port);
        }
        else
        {
            WriteInt();
            WriteUShort();
        }
    }

    public void WriteEndPoint(IPAddress ip, ushort port)
    {
        WriteBytes(ip.GetAddressBytes());
        WriteUShort(port);
    }

    public void WriteFloat(float value = 0f)
    {
        WriteBytes(BitConverter.GetBytes(value));
    }

    public void WriteHexString(string value)
    {
        if (value == null) throw new ArgumentNullException("value");
        value = value.Replace(" ", "");
        for (var i = 0; i < value.Length; i += 2) WriteByte(byte.Parse(value.Substring(i, 2), NumberStyles.HexNumber));
    }

    public void WriteInt(int value = 0)
    {
        ThrowIfDisposed();
        Append(value, 4);
    }

    public void WriteLong(long value = 0L)
    {
        ThrowIfDisposed();
        Append(value, 8);
    }

    public void WriteSByte(sbyte value = 0)
    {
        WriteByte((byte)value);
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

    public void WriteTime(DateTime time)
    {
        WriteTime(time == DateTime.MinValue ? -1 : time.Ticks);
    }

    public void WriteTime(long ticks)
    {
        if (ticks != -1)
        {
            var dateTime = new DateTime(ticks);
            WriteShort((short)(TimeUtil.GetDays(dateTime) - 1));
            WriteShort((short)(dateTime.Second / 4 + dateTime.Minute * 15 + dateTime.Hour * 900));
        }
        else
        {
            WriteShort(-1);
            WriteShort();
        }
    }

    public void WriteUInt(uint value = 0)
    {
        WriteInt((int)value);
    }

    public void WriteULong(ulong value = 0L)
    {
        WriteLong((long)value);
    }

    public void WriteUShort(ushort value = 0)
    {
        WriteShort((short)value);
    }
}