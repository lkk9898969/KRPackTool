using System;
using System.Text.RegularExpressions;

namespace KartLibrary.File;

public class Rho5File
{
    #region Constructors

    public Rho5File()
    {
        _name = "";
        NameWithoutExt = "";
        _fullname = "";
        DataSource = null;
        _dataPackID = -1;
        _originalSource = null;
        _originalName = "";
    }

    #endregion

    #region Members

    internal int _dataPackID;

    private string _name;
    private string _fullname;

    private string _originalName;
    private IDataSource? _originalSource;

    private bool _disposed;

    #endregion

    #region Properties




    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            var fileNamePattern = new Regex(@"^(.*)\..*");
            var match = fileNamePattern.Match(_name);
            if (match.Success)
                NameWithoutExt = match.Groups[1].Value;
            else
                NameWithoutExt = _name;
        }
    }

    public string FullName => _name;

    public string NameWithoutExt { get; private set; }

    public int Size => DataSource?.Size ?? 0;

    public IDataSource? DataSource { get; set; }

    public bool HasDataSource => DataSource is not null;


    #endregion

    #region Methods

    public byte[] GetBytes()
    {
        if (DataSource is null)
            throw new InvalidOperationException("There are no any data source.");
        return DataSource.GetBytes();
    }

    #endregion
}