using KartLibrary.IO;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace KartLibrary.File;

public class RhoFile : IRhoFile, IModifiableRhoFile
{
    #region Constructors

    public RhoFile()
    {
        _parentFolder = null;
        _name = "";
        NameWithoutExt = "";
        _fullname = "";
        DataSource = null;
        _originalSource = null;
        _originalName = "";
    }

    #endregion

    #region Members

    internal RhoFolder? _parentFolder;

    private string _name;
    private string _fullname;
    private uint? _extNum;
    private uint? _dataIndexBase;

    private string _originalName;
    private IDataSource? _originalSource;

    private bool _disposed;

    #endregion

    #region Properties

    public RhoFolder? Parent => _parentFolder;

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            _extNum = null;
            _dataIndexBase = null;
            var fileNamePattern = new Regex(@"^(.*)\..*");
            var match = fileNamePattern.Match(_name);
            if (match.Success)
                NameWithoutExt = match.Groups[1].Value;
            else
                NameWithoutExt = _name;
        }
    }

    public string FullName => Parent is not null ? $"{Parent.FullName}/{_name}" : _name;

    public string NameWithoutExt { get; private set; }

    public int Size => DataSource?.Size ?? 0;

    public IDataSource? DataSource { get; set; }

    public RhoFileProperty FileEncryptionProperty { get; set; }

    public bool HasDataSource => DataSource is not null;

    internal bool IsModified => _originalName != _name || _originalSource != DataSource;

    #endregion

    #region Methods

    public Stream CreateStream()
    {
        if (DataSource is null)
            throw new InvalidOperationException("There are no any data source.");
        return DataSource.CreateStream();
    }

    public void WriteTo(Stream stream)
    {
        if (DataSource is null)
            throw new InvalidOperationException("There are no any data source.");
        DataSource.WriteTo(stream);
    }

    public async Task WriteToAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (DataSource is null)
            throw new InvalidOperationException("There are no any data source.");
        await DataSource.WriteToAsync(stream, cancellationToken);
    }

    public void WriteTo(byte[] array, int offset, int count)
    {
        if (DataSource is null)
            throw new InvalidOperationException("There are no any data source.");
        WriteTo(array, offset, count);
    }

    public async Task WriteToAsync(byte[] array, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (DataSource is null)
            throw new InvalidOperationException("There are no any data source.");
        await WriteToAsync(array, offset, count, cancellationToken);
    }

    public byte[] GetBytes()
    {
        if (DataSource is null)
            throw new InvalidOperationException("There are no any data source.");
        return DataSource.GetBytes();
    }

    public async Task<byte[]> GetBytesAsync(CancellationToken cancellationToken = default)
    {
        if (DataSource is null)
            throw new InvalidOperationException("There are no any data source.");
        return await DataSource.GetBytesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _parentFolder = null;
        DataSource?.Dispose();
    }

    public override string ToString()
    {
        return $"RhoFile:{FullName}";
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
            if (disposing)
            {
            }
    }

    internal uint getExtNum()
    {
        if (_extNum is null)
        {
            var spiltStrs = _name.Split('.');
            if (spiltStrs.Length > 0)
            {
                var ext = spiltStrs[^1];
                var extEncData = Encoding.ASCII.GetBytes(ext);
                var extNumEncData = new byte[4];
                Array.Copy(extEncData, extNumEncData, Math.Min(4, extEncData.Length));
                _extNum = BitConverter.ToUInt32(extNumEncData);
            }
            else
            {
                _extNum = 0;
            }
        }

        return _extNum.Value;
    }

    internal uint getDataIndex(uint folderDataIndex)
    {
        if (_dataIndexBase is null)
        {
            var fileNameEncData = Encoding.Unicode.GetBytes(NameWithoutExt);
            var fileNameChksum = Adler.Adler32(0, fileNameEncData, 0, fileNameEncData.Length);
            var extNum = getExtNum();
            _dataIndexBase = fileNameChksum + extNum;
        }

        if (folderDataIndex == 0xFFFFFFFFu)
            folderDataIndex = 0;
        return _dataIndexBase.Value + folderDataIndex;
    }

    internal void appliedChanges()
    {
        _originalName = _name;
        _originalSource = DataSource;
    }

    #endregion
}