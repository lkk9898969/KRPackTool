using System;

namespace KartLibrary.File;

public interface IRhoFile : IDisposable
{
    string Name { get; }

    string FullName { get; }

    int Size { get; }

    bool HasDataSource { get; }
}