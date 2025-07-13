using KartLibrary.Encrypt;
using System.Text;

namespace KartLibrary.File;

public class RhoFileInfo
{
    private string _ext = "";

    internal RhoFileInfo(Rho baseRho)
    {
        BaseRho = baseRho;
    }

    public Rho BaseRho { get; set; }
    public string Name { get; set; }

    public string Extension
    {
        get => _ext;
        set
        {
            _ext = value;
            ExtNum = getExtNum();
        }
    }

    public int ExtNum { get; private set; } = -1;

    public uint FileBlockIndex { get; set; }
    public RhoFileProperty FileProperty { get; set; }
    public int FileSize { get; set; }

    public string FullFileName
    {
        get
        {
            if (_ext == "")
                return Name;
            return $"{Name}.{_ext}";
        }
    }

    public byte[] GetData()
    {
        var DecryptKey = RhoKey.GetDataKey(BaseRho.GetFileKey(), this);
        var Data = BaseRho.GetBlockData(FileBlockIndex, DecryptKey);
        return Data;
    }

    internal int getExtNum()
    {
        var output = 0;
        var arr = Encoding.UTF8.GetBytes(_ext);
        for (var i = 0; i < arr.Length; i++) output |= arr[i] << (i << 3);
        return output;
    }

}