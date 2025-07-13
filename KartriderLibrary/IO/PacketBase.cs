using System;
using System.Text;

namespace KartRider.IO.Packet;

public abstract class PacketBase : IDisposable
{
    public abstract int Length { get; }

    public abstract int Position { get; set; }

    public virtual void Dispose()
    {
    }

    public abstract byte[] ToArray();

    public override string ToString()
    {
        var stringBuilder = new StringBuilder();
        var array = ToArray();
        for (var i = 0; i < array.Length; i++) stringBuilder.AppendFormat("{0:X2} ", array[i]);
        return stringBuilder.ToString();
    }
}