using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using KartLibrary.Encrypt;
using KartLibrary.IO;

namespace KartLibrary.File;

public class Rho : IDisposable
{
    private readonly uint BlockWhiteningKey;

    private readonly (double, string)[] MagicString =
    {
        (1.0d, "Rh layer spec 1.0"),
        (1.1d, "Rh layer spec 1.1")
    };

    public Stream baseStream;

    private Dictionary<uint, RhoDataInfo> Blocks;

    public uint DataHash;

    public uint RhoFileKey;

    public Rho(string FileName, uint rhoFileKey = 0)
    {
        if (!System.IO.File.Exists(FileName))
            throw new FileNotFoundException($"Exception: Could't find the file:{FileName}.", FileName);
        //Test
        var fileStream = new FileStream(FileName, FileMode.Open);

        baseStream = new BufferedStream(fileStream, 4096); //Default: 4 KiB
        this.FileName = FileName;
        var reader = new BinaryReader(baseStream);
        var fileInfo = new FileInfo(FileName);
        if (rhoFileKey == 0)
            RhoFileKey = RhoKey.GetRhoKey(fileInfo.Name.Replace(".rho", ""));
        else
            RhoFileKey = rhoFileKey;
        // Read Magic String
        var magicStrBytes = reader.ReadBytes(0x22);
        var magicStr = Encoding.GetEncoding("UTF-16").GetString(magicStrBytes);
        var verIndex = Array.FindIndex(MagicString, x => x.Item2 == magicStr);
        if (verIndex == -1)
            throw new NotSupportedException($"Exception: This file:{FileName} is not supported.");
        Version = MagicString[verIndex].Item1;
        baseStream.Seek(0x80, SeekOrigin.Begin);
        // Part 2
        var part2Data = reader.ReadBytes(0x80);
        if (Version == 1.0d)
            RhoEncrypt.DecryptData(RhoFileKey, part2Data, 0, part2Data.Length);
        else if (Version == 1.1d)
            part2Data = RhoEncrypt.DecryptHeaderInfo(part2Data, RhoFileKey);
        var BlockCount = 0;
        var BlockInfoKeyOld = new byte[0]; // For 1.0 version
        uint BlockInfoKey = 0; // For 1.1 version
        using (var ms = new MemoryStream(part2Data))
        {
            var br = new BinaryReader(ms);
            var part2Hash = br.ReadUInt32();
            var checkHash = Adler.Adler32(0, part2Data, 4, 0x7C);
            if (part2Hash != checkHash)
                throw new NotSupportedException(
                    $"Exception: This file:{FileName} was modified. [ Part 2 Hash not euqal ]");
            var MagicCode = br.ReadInt32();
            if ((Version == 1.0d && MagicCode != 0x00010000) || (Version == 1.1d && MagicCode != 0x00010001))
                throw new NotSupportedException(
                    $"Exception: This file:{FileName} is not Rho File. [ Header check failure ]");
            BlockCount = br.ReadInt32(); // 10
            BlockWhiteningKey = br.ReadUInt32(); //14 // BlockInfoKey = RhoFileKey ^  BlockWhiteningKey. 
            BlockInfoKey = RhoFileKey ^ BlockWhiteningKey;
            //1.1: 14 bytes unknown
            if (Version == 1.0d)
            {
                BlockInfoKeyOld = br.ReadBytes(32);
            }
            else if (Version == 1.1d)
            {
                var u1a = br.ReadInt32(); //=1
                var u2a = br.ReadInt32(); //=RhoKey - 397E40C3
                DataHash = br.ReadUInt32(); // in aaa.pk file
                //Debug.Print($"DataHash: {DataHash:x8}");
            }

            var EndMagicCode = br.ReadInt32(); // = FC1F9778

            Blocks = new Dictionary<uint, RhoDataInfo>(BlockCount);
        }

        baseStream.Seek(0x100, SeekOrigin.Begin);
        // Part 3
        for (var i = 0; i < BlockCount; i++)
            if (Version == 1.0d)
            {
                var blockInfo = reader.ReadBlockInfo10(BlockInfoKeyOld);
                Blocks.Add(blockInfo.Index, blockInfo);
            }
            else if (Version == 1.1d)
            {
                var blockInfo = reader.ReadBlockInfo(BlockInfoKey);
                Blocks.Add(blockInfo.Index, blockInfo);
                BlockInfoKey++;
            }

        // Part 4
        RootDirectory = new RhoDirectory(this);
        RootDirectory.DirectoryName = "";
        RootDirectory.DirIndex = 0xFFFFFFFF;
        var processQueue = new Queue<RhoDirectory>();
        processQueue.Enqueue(RootDirectory);
        while (processQueue.Count > 0)
        {
            var curDir = processQueue.Dequeue();
            var dirData = reader.ReadBlock(this, curDir.DirIndex, RhoKey.GetDirectoryDataKey(RhoFileKey));

            curDir.GetFromDirInfo(dirData);
            foreach (var subdir in curDir.GetDirectories()) processQueue.Enqueue(subdir);
        }

        var trtt = new List<RhoDataInfo>();
        foreach (var blockKeyPair in Blocks) trtt.Add(blockKeyPair.Value);
    }

    public double Version { get; }
    public string FileName { get; private set; }

    public RhoDirectory RootDirectory { get; set; }

    public void Dispose()
    {
        baseStream.Close();
        baseStream.Dispose();
        Blocks = null;
    }

    public uint GetFileKey()
    {
        return RhoFileKey;
    }

    public uint GetDataHash()
    {
        return DataHash;
    }

    internal RhoDataInfo GetBlockInfo(uint Index)
    {
        if (!Blocks.ContainsKey(Index))
            return null;
        return Blocks[Index];
    }

    internal byte[] GetBlockData(uint BlockIndex, uint Key)
    {
        var reader = new BinaryReader(baseStream);
        var output = reader.ReadBlock(this, BlockIndex, Key);
        var adler = Adler.Adler32(0, output, 0, output.Length);
        return output;
    }

    public RhoFileInfo GetFile(string Path)
    {
        var PathSplit = Path.Split('/');
        var rd = RootDirectory;
        for (var i = 1; i < PathSplit.Length - 1; i++)
        {
            var curPathName = PathSplit[i].Trim();
            if (curPathName == "")
                continue;
            var nextDir = rd.GetDirectory(curPathName);
            if (nextDir is null)
                return null;
            rd = nextDir;
        }

        return rd.GetFile(PathSplit[PathSplit.Length - 1]);
    }

    ~Rho()
    {
        Dispose();
    }
}