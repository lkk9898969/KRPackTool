using System;

namespace KartLibrary.File;

public class RhoFileHandler
{
    private readonly RhoArchive _archive;
    private bool _disposed;
    internal uint _fileDataIndex;
    internal RhoFileProperty _fileProperty;
    internal uint _key;
    private bool _released;
    internal int _size;

    internal RhoFileHandler(RhoArchive archive, RhoFileProperty fileProperty, uint fileDataIndex, int size, uint key)
    {
        _fileDataIndex = fileDataIndex;
        _fileProperty = fileProperty;
        _archive = archive;
        _size = size;
        _key = key;

        _released = false;
        _disposed = false;
    }

    internal byte[] getData()
    {
        if (_released)
            throw new Exception("this handle was released.");
        return _archive.getData(this);
    }

    internal void releaseHandler()
    {
        _released = true;
    }
}