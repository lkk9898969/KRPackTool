using System;

namespace KartLibrary.File;

public interface IModifiableRhoFile : IDisposable
{
    string Name { get; set; }

    string FullName { get; }

    int Size { get; }

    bool HasDataSource { get; }

    IDataSource DataSource { set; }
}