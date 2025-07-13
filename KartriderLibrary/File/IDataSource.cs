using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace KartLibrary.File;

public interface IDataSource : IDisposable
{

    int Size { get; }

    Stream CreateStream();

    void WriteTo(Stream stream);

    Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default);

    byte[] GetBytes();

    Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default);
}