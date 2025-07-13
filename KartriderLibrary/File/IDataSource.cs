using System;

namespace KartLibrary.File;

public interface IDataSource : IDisposable
{
    int Size { get; }
    byte[] GetBytes();

}