using System.Net.Sockets;

namespace KartRider.Common.Network;

public class SocketInfo
{
    public enum StateEnum
    {
        Header,
        Content
    }

    public readonly Socket Socket;

    public byte[] DataBuffer;

    public int Index;

    public bool NoEncryption;

    public StateEnum State;

    public SocketInfo(Socket socket, short headerLength) : this(socket, headerLength, false)
    {
    }

    public SocketInfo(Socket socket, short headerLength, bool noEncryption)
    {
        Socket = socket;
        State = StateEnum.Header;
        NoEncryption = noEncryption;
        DataBuffer = new byte[headerLength];
        Index = 0;
    }

    private SocketInfo()
    {
        DataBuffer = null;
    }
}