using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace KartLibrary.File;

public class RhoDirectory
{
    public static Dictionary<RhoFileProperty, Dictionary<string, int>> counter = new();

    public RhoDirectory(Rho BaseRho)
    {
        this.BaseRho = BaseRho;
        DirectoryName = "";
    }

    public Rho BaseRho { get; set; }
    public string DirectoryName { get; set; }
    public RhoDirectory Parent { get; set; }
    public Dictionary<string, RhoDirectory> Directories { get; private set; }
    public Dictionary<string, RhoFileInfo> Files { get; private set; }
    internal uint DirIndex { get; set; }

    internal void GetFromDirInfo(byte[] DirInfoData)
    {
        using (var ms = new MemoryStream(DirInfoData))
        {
            var msReader = new BinaryReader(ms);
            var DirCount = msReader.ReadInt32();
            Directories = new Dictionary<string, RhoDirectory>(DirCount);
            for (var i = 0; i < DirCount; i++)
            {
                var dir = new RhoDirectory(BaseRho);
                var strBuilder = new StringBuilder();
                var tempChar = (char)msReader.ReadInt16();
                while (tempChar != 0)
                {
                    strBuilder.Append(tempChar);
                    tempChar = (char)msReader.ReadInt16();
                }

                var dirInd = msReader.ReadUInt32();
                dir.DirectoryName = strBuilder.ToString();
                dir.DirIndex = dirInd;
                Directories.Add(dir.DirectoryName, dir);
            }

            var FileCount = msReader.ReadInt32();
            Files = new Dictionary<string, RhoFileInfo>(FileCount);
            for (var i = 0; i < FileCount; i++)
            {
                var rfi = new RhoFileInfo(BaseRho);
                var strBuilder = new StringBuilder();
                var tempChar = (char)msReader.ReadInt16();
                while (tempChar != 0)
                {
                    strBuilder.Append(tempChar);
                    tempChar = (char)msReader.ReadInt16();
                }

                rfi.Name = strBuilder.ToString();
                strBuilder.Clear();
                var extInt = msReader.ReadUInt32();
                rfi.FileProperty = (RhoFileProperty)msReader.ReadInt32();
                rfi.FileBlockIndex = msReader.ReadUInt32();
                rfi.FileSize = msReader.ReadInt32();
                for (var j = 0; j < 4; j++)
                {
                    tempChar = (char)((extInt >> (j << 3)) & 0xFF);
                    if (tempChar != '\0')
                        strBuilder.Append(tempChar);
                }

                rfi.Extension = strBuilder.ToString();
                Files.Add(rfi.FullFileName, rfi);
                if (!counter.ContainsKey(rfi.FileProperty))
                    counter.Add(rfi.FileProperty, new Dictionary<string, int>());
                if (!counter[rfi.FileProperty].ContainsKey(rfi.Extension))
                    counter[rfi.FileProperty].Add(rfi.Extension, 0);
                counter[rfi.FileProperty][rfi.Extension]++;
            }
        }
    }

    public RhoDirectory GetDirectory(string DirFileName)
    {
        if (Directories.ContainsKey(DirFileName))
            return Directories[DirFileName];
        return null;
    }

    public RhoFileInfo GetFile(string FileName)
    {
        if (Files.ContainsKey(FileName))
            return Files[FileName];
        return null;
    }

    public RhoDirectory[] GetDirectories()
    {
        return Directories.Values.ToArray();
    }

    public RhoFileInfo[] GetFiles()
    {
        return Files.Values.ToArray();
    }

    public override int GetHashCode()
    {
        return (int)DirIndex;
    }
}