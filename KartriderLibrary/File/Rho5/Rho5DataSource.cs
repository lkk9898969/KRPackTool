namespace KartLibrary.File;

public class Rho5DataSource : IDataSource
{
    #region Constructors

    internal Rho5DataSource(Rho5FileHandler fileHandler)
    {
        _disposed = false;
        _fileHandler = fileHandler;
    }

    #endregion

    #region Members

    private bool _disposed;
    private readonly Rho5FileHandler _fileHandler;

    #endregion

    #region Properties

    public int Size => _fileHandler._decompressedSize;

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