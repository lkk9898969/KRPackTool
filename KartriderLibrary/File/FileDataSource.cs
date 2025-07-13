using System.IO;

namespace KartLibrary.File;

public class FileDataSource : IDataSource
{
    private readonly string _fileName;
    private bool _disposed;

    public FileDataSource(string fileName)
    {
        if (!System.IO.File.Exists(fileName))
            throw new FileNotFoundException("file not found", fileName);
        _fileName = fileName;
        using (var tmpFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
        {
            Size = (int)tmpFileStream.Length;
        }

        _disposed = false;
    }


    public int Size { get; }


    public byte[] GetBytes()
    {
        using (var tmpFileStream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
        {
            var output = new byte[Size];
            tmpFileStream.Read(output);
            return output;
        }
    }
    public void Dispose()
    {
        _disposed = true;
    }
}