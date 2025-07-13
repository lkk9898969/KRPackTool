namespace KartLibrary.File;

public class RhoDataSource : IDataSource
{
    #region Constructors

    internal RhoDataSource(RhoFileHandler fileHandler)
    {
        _disposed = false;
        _fileHandler = fileHandler;
    }

    #endregion

    #region Members

    private bool _disposed;
    private readonly RhoFileHandler _fileHandler;

    #endregion

    #region Properties

    public int Size => _fileHandler._size;

    #endregion

    #region Methods


    public byte[] GetBytes()
    {
        var data = _fileHandler.getData();
        return data;
    }


    public void Dispose()
    {
        _disposed = true;
    }

    #endregion
}