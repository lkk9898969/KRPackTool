using System;
using System.Net;
using System.Text;
using KartNew.Utilities;

namespace KartRider.IO.Packet;

public class InPacket : PacketBase
{
    private byte[] _buffer;

    private int _index;

    public InPacket(byte[] packet)
    {
        _buffer = packet;
        _index = 0;
    }

    public int Available => _buffer.Length - _index;

    public override int Length => _buffer.Length;

    public override int Position
    {
        get => _index;
        set => _index = value;
    }

    private void CheckLength(int length)
    {
        if (_index + length > _buffer.Length ? true : length < 0) throw new PacketReadException("Not enough space");
    }

    public override void Dispose()
    {
        _buffer = null;
    }

    public bool ReadBool()
    {
        return ReadByte() == 1;
    }

    public byte ReadByte()
    {
        CheckLength(1);
        var numArray = _buffer;
        var num = _index;
        _index = num + 1;
        return numArray[num];
    }

    public byte[] ReadBytes(int count)
    {
        CheckLength(count);
        var numArray = new byte[count];
        Buffer.BlockCopy(_buffer, _index, numArray, 0, count);
        _index += count;
        return numArray;
    }


    public byte ReadEncodedByte()
    {
        return CryptoConstants.decodeBytes(new[] { ReadByte() })[0];
    }

    public float ReadEncodedFloat()
    {
        return BitConverter.ToSingle(
            CryptoConstants.decodeBytes(new[] { ReadByte(), ReadByte(), ReadByte(), ReadByte() }), 0);
    }

    public int ReadEncodedInt()
    {
        return BitConverter.ToInt32(
            CryptoConstants.decodeBytes(new[] { ReadByte(), ReadByte(), ReadByte(), ReadByte() }), 0);
    }

    public uint ReadEncodedUInt()
    {
        return BitConverter.ToUInt32(
            CryptoConstants.decodeBytes(new[] { ReadByte(), ReadByte(), ReadByte(), ReadByte() }), 0);
    }

    public IPEndPoint ReadEndPoint()
    {
        var pEndPoint = new IPEndPoint(new IPAddress(ReadBytes(4)), ReadUShort());
        return pEndPoint;
    }

    public uint ReadUInt()
    {
        return (uint)ReadInt();
    }

    public ulong ReadULong()
    {
        return (ulong)ReadLong();
    }

    public ushort ReadUShort()
    {
        return (ushort)ReadShort();
    }

    public sbyte ReadSByte()
    {
        return (sbyte)ReadByte();
    }

    public float ReadFloat()
    {
        return BitConverter.ToSingle(new[] { ReadByte(), ReadByte(), ReadByte(), ReadByte() }, 0);
    }

    public unsafe int ReadInt()
    {
        CheckLength(4);

        int value;

        fixed (byte* ptr = _buffer)
        {
            value = *(int*)(ptr + _index);
        }

        _index += 4;

        return value;
    }

    public unsafe long ReadLong()
    {
        CheckLength(8);

        long value;

        fixed (byte* ptr = _buffer)
        {
            value = *(long*)(ptr + _index);
        }

        _index += 8;

        return value;
    }

    public unsafe short ReadShort()
    {
        CheckLength(2);

        short value;

        fixed (byte* ptr = _buffer)
        {
            value = *(short*)(ptr + _index);
        }

        _index += 2;

        return value;
    }

    public string ReadString(bool ascii = false)
    {
        string str;
        var num = ReadInt();
        if (!ascii) num *= 2;
        CheckLength(num);
        str = !ascii ? Encoding.Unicode.GetString(ReadBytes(num)) : Encoding.ASCII.GetString(ReadBytes(num));
        return str;
    }

    public DateTime ReadTime()
    {
        DateTime dateTime;
        var num = ReadUShort();
        var num1 = ReadUShort();
        if (num != 65535)
        {
            var num2 = (uint)(num * 21600 + num1);
            var num3 = (int)(num2 / 21600);
            var year = TimeUtil.GetYear(ref num3) + 1900;
            var month = TimeUtil.GetMonth(ref num3, TimeUtil.IsLeapYear(year)) + 1;
            var num4 = (int)(num2 % 21600 / 900);
            var num5 = (int)(num2 % 21600 % 900 / 15);
            var num6 = (int)(4 * (num2 % 21600 % 900 % 15));
            dateTime = new DateTime(year, month, num3, num4, num5, num6);
        }
        else
        {
            dateTime = DateTime.MinValue;
        }

        return dateTime;
    }

    public void Skip(int count)
    {
        CheckLength(count);
        _index += count;
    }

    public override byte[] ToArray()
    {
        var numArray = new byte[_buffer.Length];
        Buffer.BlockCopy(_buffer, 0, numArray, 0, _buffer.Length);
        return numArray;
    }
}