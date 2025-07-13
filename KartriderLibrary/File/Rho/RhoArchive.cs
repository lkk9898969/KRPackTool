using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zlib;
using KartLibrary.Encrypt;
using KartLibrary.IO;
using Adler = KartLibrary.IO.Adler;

namespace KartLibrary.File;

/// <summary lang="en-us">
///     <see cref="RhoFile" /> represents a Rho type archive. You can open and save Rho file with this class.
/// </summary>
/// <summary lang="zh-tw">
///     <see cref="RhoFile" />用來表示一個Rho檔案。你能藉此類型來開啟及儲存Rho類型檔案.
/// </summary>
public partial class RhoArchive : IRhoArchive<RhoFolder, RhoFile>
{
    #region Constructors

    /// <summary>
    ///     Constructs a new instance of <see cref="RhoArchive" />.
    /// </summary>
    public RhoArchive()
    {
        RootFolder = new RhoFolder();
        _fileHandlers = new Dictionary<uint, RhoFileHandler>();
        _dataInfoMap = new Dictionary<uint, RhoDataInfo>();
        IsClosed = true;
    }

    #endregion

    #region Structs

    private class DataSavingInfo
    {
        public readonly RhoDataInfo DataInfo = new();
        public byte[] Data;
        public IDataSource DataSource;
        public RhoFile File;
    }

    #endregion

    #region Members

    private readonly int _layerVersion = 1; // 1.0 = 0, 1.1 = 1
    private FileStream _rhoStream;

    private Dictionary<uint, RhoDataInfo> _dataInfoMap;

    private Dictionary<uint, RhoFileHandler> _fileHandlers;

    private uint _rhoKey;

    private uint _dataChecksum;

    private bool _disposed;

    #endregion

    #region Properties

    /// <summary>
    ///     Root folder of current <see cref="RhoArchive" />
    /// </summary>
    public RhoFolder RootFolder { get; }

    public bool IsClosed { get; }

    /// <summary>
    ///     If this instance is locked,
    ///     calling any method of this instance is not allowed until the asynchronous method that sets lock is done.
    /// </summary>
    public bool IsLocked { get; }

    #endregion

    #region Methods

    /// <summary>
    ///     Save current <see cref="RhoArchive" /> instance to Rho file.
    /// </summary>
    /// <param name="filePath"></param>
    /// <exception cref="Exception"></exception>
    public void SaveTo(string filePath)
    {
        const uint dataInfoWhiteningKey = 0x3a9213ac;
        var fullName = Path.GetFullPath(filePath);
        var fullDirName = Path.GetDirectoryName(fullName) ?? "";
        if (!Directory.Exists(fullDirName)) throw new Exception("directory not exists.");
        if (_rhoStream is not null)
        {
            var curRhoFullName = Path.GetFullPath(_rhoStream.Name);
            if (curRhoFullName == fullDirName)
                System.IO.File.Copy(curRhoFullName, $"{curRhoFullName}.bak");
        }

        var outFileName = Path.GetFileNameWithoutExtension(fullName);
        var outRhoKey = RhoKey.GetRhoKey(outFileName);

        var dataSavingQueue = new Queue<DataSavingInfo>();
        var usedIndex = new HashSet<uint>();
        var dataEndOffset = 0;
        storeFolderAndFiles(RootFolder, dataSavingQueue, usedIndex, ref dataEndOffset, outRhoKey);
        if (_rhoStream is not null)
        {
            _rhoStream.Close();
            releaseAllHandlers();
        }

        uint outDataHash = 0;
        foreach (var dataSavingInfo in dataSavingQueue)
            outDataHash = Adler.Adler32Combine(outDataHash, dataSavingInfo.Data, 0, dataSavingInfo.Data.Length);
        if (_dataInfoMap is not null)
            _dataInfoMap.Clear();
        else
            _dataInfoMap = new Dictionary<uint, RhoDataInfo>(dataSavingQueue.Count);
        if (_fileHandlers is null)
            _fileHandlers = new Dictionary<uint, RhoFileHandler>(dataSavingQueue.Count);
        // Begin write to out file.
        var outFileStream = new FileStream(fullName, FileMode.Create);
        var dataInfoSize = (dataSavingQueue.Count * 0x20 + 0xFF) & 0x7FFFFF00;
        var dataBeginOffset = 0x100 + dataInfoSize;
        dataEndOffset += dataBeginOffset;

        // Write Identifier Text
        using var outWriter = new BinaryWriter(outFileStream);
        outWriter.Write(Encoding.Unicode.GetBytes(_rhLayerIdentifiers[_layerVersion]));
        outFileStream.Seek(0x40, SeekOrigin.Begin);
        outWriter.Write(Encoding.Unicode.GetBytes(_rhLayerSecondText));

        // Write Header
        outFileStream.Seek(0x80, SeekOrigin.Begin);
        var rhoHeaderData = new byte[0x80]; //Without header checksum
        var dataInfoKey10 = new byte[0x20];
        using (var memStream = new MemoryStream(0x7C))
        {
            var memWriter = new BinaryWriter(memStream);
            memWriter.Write(_layerVersion | 0x10000);
            memWriter.Write(dataSavingQueue.Count);
            memWriter.Write(dataInfoWhiteningKey);
            if (_layerVersion == 0)
            {
                memWriter.Write(dataInfoKey10);
            }
            else if (_layerVersion == 1)
            {
                memWriter.Write(1);
                memWriter.Write(outRhoKey - 0x397E40C3);
                memWriter.Write(outDataHash);
                memWriter.Write(0xFC1F9778);
                memWriter.Write(0x7E);
            }

            memStream.Seek(0, SeekOrigin.Begin);
            memStream.Read(rhoHeaderData, 4, (int)memStream.Length);
        }

        var rhoHeaderChksum = Adler.Adler32(0, rhoHeaderData, 4, 0x7C);
        Array.Copy(BitConverter.GetBytes(rhoHeaderChksum), 0, rhoHeaderData, 0, 0x04);
        if (_layerVersion == 0)
            RhoEncrypt.EncryptData(outRhoKey, rhoHeaderData, 0, rhoHeaderData.Length);
        else if (_layerVersion == 1)
            rhoHeaderData = RhoEncrypt.EncryptHeaderInfo(rhoHeaderData, outRhoKey);
        outWriter.Write(rhoHeaderData);

        // Write Data Info
        var dataInfoKey11 = dataInfoWhiteningKey ^ outRhoKey;

        outFileStream.Seek(0x100, SeekOrigin.Begin);
        foreach (var dataSavingInfo in dataSavingQueue)
        {
            var dataInfoEncData = new byte[0x20];
            using (var memStream = new MemoryStream(0x20))
            {
                var memWriter = new BinaryWriter(memStream);
                memWriter.Write(dataSavingInfo.DataInfo.Index);
                memWriter.Write((int)((dataSavingInfo.DataInfo.Offset + dataBeginOffset) >> 8));
                memWriter.Write(dataSavingInfo.DataInfo.DataSize);
                memWriter.Write(dataSavingInfo.DataInfo.UncompressedSize);
                memWriter.Write((int)dataSavingInfo.DataInfo.BlockProperty);
                memWriter.Write(dataSavingInfo.DataInfo.Checksum);

                memStream.Seek(0, SeekOrigin.Begin);
                memStream.Read(dataInfoEncData, 0, dataInfoEncData.Length);
            }

            var rhoDataInfo = new RhoDataInfo();
            rhoDataInfo.Index = dataSavingInfo.DataInfo.Index;
            rhoDataInfo.Offset = dataSavingInfo.DataInfo.Offset + dataBeginOffset;
            rhoDataInfo.DataSize = dataSavingInfo.DataInfo.DataSize;
            rhoDataInfo.UncompressedSize = dataSavingInfo.DataInfo.UncompressedSize;
            rhoDataInfo.BlockProperty = dataSavingInfo.DataInfo.BlockProperty;
            rhoDataInfo.Checksum = dataSavingInfo.DataInfo.Checksum;
            _dataInfoMap.Add(rhoDataInfo.Index, rhoDataInfo);

            if (_layerVersion == 0)
                dataInfoEncData = RhoEncrypt.EncryptBlockInfoOld(dataInfoEncData, dataInfoKey10);
            else if (_layerVersion == 1)
                dataInfoEncData = RhoEncrypt.EncryptHeaderInfo(dataInfoEncData, dataInfoKey11++);
            var dbgg = RhoEncrypt.DecryptHeaderInfo(dataInfoEncData, dataInfoKey11 - 1);
            outWriter.Write(dataInfoEncData);
        }

        // Write Data
        while (dataSavingQueue.Count > 0)
        {
            var dataSavingInfo = dataSavingQueue.Dequeue();
            outFileStream.Seek(dataSavingInfo.DataInfo.Offset + dataBeginOffset, SeekOrigin.Begin);
            outFileStream.Write(dataSavingInfo.Data, 0, dataSavingInfo.Data.Length);
            if (dataSavingInfo.File is not null)
            {
                var file = dataSavingInfo.File;
                var fileHandler = new RhoFileHandler(this, file.FileEncryptionProperty, dataSavingInfo.DataInfo.Index,
                    file.Size, RhoKey.GetFileKey(outRhoKey, file.NameWithoutExt, file.getExtNum()));
                if (file.DataSource is not null)
                    file.DataSource.Dispose();
                _fileHandlers.Add(dataSavingInfo.DataInfo.Index, fileHandler);
                file.DataSource = new RhoDataSource(fileHandler);
            }
        }

        if (outFileStream.Position != dataEndOffset)
        {
            outFileStream.Seek(dataEndOffset - 1, SeekOrigin.Begin);
            outFileStream.WriteByte(0x00);
        }

        outFileStream.Close();
        _rhoStream = new FileStream(fullName, FileMode.Open);

        // send applied changes event to RhoFolder instances
        var folderQueue = new Queue<RhoFolder>();
        folderQueue.Enqueue(RootFolder);
        while (folderQueue.Count > 0)
        {
            var folder = folderQueue.Dequeue();
            folder.appliedChanges();
            foreach (var subFolder in folder.Folders)
                folderQueue.Enqueue(subFolder);
        }
    }

    public void Close()
    {
        //if (_closed)
        //    throw new Exception("This archive is close or is not open from a file.");
        if (_rhoStream is not null && _rhoStream.CanRead)
            _rhoStream.Close();
        _rhoStream?.Dispose();
        _fileHandlers.Clear();
        RootFolder.Clear();
    }

    public void Dispose()
    {
        if (!IsClosed)
            Close();
        releaseAllHandlers();
    }

    internal byte[] getData(RhoFileHandler handler)
    {
        if (!_dataInfoMap.ContainsKey(handler._fileDataIndex))
            throw new Exception("handler corrupted.");
        return getData(handler._fileDataIndex, handler._key);
    }

    private byte[] getData(uint dataIndex, uint key)
    {
        if (!_dataInfoMap.ContainsKey(dataIndex))
            throw new Exception("index not exist.");
        var clonedRhoStream = new FileStream(_rhoStream.SafeFileHandle, FileAccess.Read);
        var dataInfo = _dataInfoMap[dataIndex];

        clonedRhoStream.Seek(dataInfo.Offset, SeekOrigin.Begin);
        var outData = new byte[dataInfo.DataSize];
        clonedRhoStream.Read(outData, 0, dataInfo.DataSize);

        if ((dataInfo.BlockProperty & RhoBlockProperty.Compressed) != RhoBlockProperty.None)
            using (var memStream = new MemoryStream(outData))
            {
                outData = new byte[dataInfo.UncompressedSize];
                var decompressStream = new ZlibStream(memStream, CompressionMode.Decompress);
                var readed = decompressStream.Read(outData, 0, outData.Length);
            }

        if ((dataInfo.BlockProperty & RhoBlockProperty.PartialEncrypted) != RhoBlockProperty.None)
            RhoEncrypt.DecryptData(key, outData, 0, outData.Length);
        if (dataInfo.BlockProperty == RhoBlockProperty.PartialEncrypted)
        {
            var secDatainfo = _dataInfoMap.ContainsKey(dataIndex + 1) ? _dataInfoMap[dataIndex + 1] : null;
            if (secDatainfo is not null)
            {
                Array.Resize(ref outData, outData.Length + secDatainfo.DataSize);
                clonedRhoStream.Read(outData, dataInfo.DataSize, secDatainfo.DataSize);
            }
        }

        return outData;
    }

    private void storeFolderAndFiles(RhoFolder folder, Queue<DataSavingInfo> savingInfo, HashSet<uint> usedIndex,
        ref int dataOffset, uint outRhoKey)
    {
        if (folder.Name == "" && folder.Parent is not null)
            throw new Exception("folder name couldn't be empty.");
        var folderDataIndex = folder.getFolderDataIndex();
        while (usedIndex.Contains(folderDataIndex))
            folderDataIndex += 0x5F03E367;
        byte[] folderData;

        var fileSavingInfoQueue = new Queue<DataSavingInfo>();

        // Encode folder
        using (var memStream = new MemoryStream())
        {
            var memWriter = new BinaryWriter(memStream);
            var subFolders = folder.Folders;
            var subFiles = folder.Files;
            memWriter.Write(subFolders.Count);
            foreach (var subFolder in subFolders)
            {
                var subFolderDataIndex = subFolder.getFolderDataIndex();
                memWriter.WriteNullTerminatedText(subFolder.Name, true);
                memWriter.Write(subFolderDataIndex);
            }

            memWriter.Write(subFiles.Count);
            foreach (var subFile in subFiles)
            {
                if (subFile.DataSource is null)
                    throw new Exception("data source is null.");

                var extNum = subFile.getExtNum();
                var fileKey = RhoKey.GetFileKey(outRhoKey, subFile.NameWithoutExt, extNum);
                var fileSize = subFile.Size;
                var fileDataIndex = subFile.getDataIndex(folderDataIndex);
                var fileData = subFile.DataSource.GetBytes();
                uint fileChksum = 0;

                while (usedIndex.Contains(fileDataIndex) || usedIndex.Contains(fileDataIndex + 1))
                    fileDataIndex += 0x4D21CB4F;

                if (subFile.FileEncryptionProperty == RhoFileProperty.Encrypted ||
                    subFile.FileEncryptionProperty == RhoFileProperty.CompressedEncrypted)
                {
                    fileChksum = Adler.Adler32(0, fileData, 0, fileData.Length);
                    RhoEncrypt.EncryptData(fileKey, fileData, 0, fileData.Length);
                }
                else if (subFile.FileEncryptionProperty == RhoFileProperty.PartialEncrypted)
                {
                    RhoEncrypt.EncryptData(fileKey, fileData, 0, Math.Min(0x100, fileData.Length));
                }

                if (subFile.FileEncryptionProperty == RhoFileProperty.CompressedEncrypted ||
                    subFile.FileEncryptionProperty == RhoFileProperty.Compressed)
                {
                    using var ms = new MemoryStream();
                    using (var compressStream = new ZlibStream(ms, CompressionMode.Compress,
                               CompressionLevel.BestCompression, true))
                    {
                        compressStream.Write(fileData, 0, fileData.Length);
                    }

                    fileData = ms.ToArray();
                }

                memWriter.WriteNullTerminatedText(subFile.NameWithoutExt, true);
                memWriter.Write(extNum);
                memWriter.Write((int)subFile.FileEncryptionProperty);
                memWriter.Write(fileDataIndex);
                memWriter.Write(fileSize);

                var fileSavingInfo = new DataSavingInfo();
                fileSavingInfo.File = subFile;
                if (subFile.FileEncryptionProperty == RhoFileProperty.PartialEncrypted)
                {
                    fileSavingInfo.Data = new byte[Math.Min(0x100, fileData.Length)];
                    fileSavingInfo.DataInfo.Index = fileDataIndex;
                    fileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.PartialEncrypted;
                    fileSavingInfo.DataInfo.DataSize = fileSavingInfo.Data.Length;
                    fileSavingInfo.DataInfo.UncompressedSize = fileSavingInfo.Data.Length;
                    fileSavingInfo.DataInfo.Checksum = 0;
                    Array.Copy(fileData, 0, fileSavingInfo.Data, 0, fileSavingInfo.Data.Length);
                    usedIndex.Add(fileDataIndex);
                    fileSavingInfoQueue.Enqueue(fileSavingInfo);
                    if (fileData.Length > 0x100)
                    {
                        var secFileSavingInfo = new DataSavingInfo();
                        secFileSavingInfo.Data = new byte[fileData.Length - 0x100];
                        secFileSavingInfo.DataInfo.Index = fileDataIndex + 1;
                        secFileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.None;
                        secFileSavingInfo.DataInfo.DataSize = secFileSavingInfo.Data.Length;
                        secFileSavingInfo.DataInfo.UncompressedSize = secFileSavingInfo.Data.Length;
                        secFileSavingInfo.DataInfo.Checksum = 0;
                        Array.Copy(fileData, 0x100, secFileSavingInfo.Data, 0, secFileSavingInfo.Data.Length);
                        usedIndex.Add(fileDataIndex + 1);
                        fileSavingInfoQueue.Enqueue(secFileSavingInfo);
                    }
                }
                else
                {
                    fileSavingInfo.Data = fileData;
                    fileSavingInfo.DataInfo.Index = fileDataIndex;
                    fileSavingInfo.DataInfo.Checksum = fileChksum;
                    fileSavingInfo.DataInfo.DataSize = fileData.Length;
                    fileSavingInfo.DataInfo.UncompressedSize = fileSize;
                    switch (subFile.FileEncryptionProperty)
                    {
                        case RhoFileProperty.None:
                            fileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.None;
                            break;
                        case RhoFileProperty.Encrypted:
                            fileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.FullEncrypted;
                            break;
                        case RhoFileProperty.Compressed:
                            fileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.Compressed;
                            break;
                        case RhoFileProperty.CompressedEncrypted:
                            fileSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.CompressedEncrypted;
                            break;
                    }

                    usedIndex.Add(fileDataIndex);
                    fileSavingInfoQueue.Enqueue(fileSavingInfo);
                }
            }

            folderData = memStream.ToArray();
        }

        var folderDataDecChksum = Adler.Adler32(0, folderData, 0, folderData.Length);
        var folderKey = RhoKey.GetDirectoryDataKey(outRhoKey);
        RhoEncrypt.EncryptData(folderKey, folderData, 0, folderData.Length);

        var folderSavingInfo = new DataSavingInfo();
        folderSavingInfo.Data = folderData;
        folderSavingInfo.DataInfo.Offset = dataOffset;
        folderSavingInfo.DataInfo.Index = folderDataIndex;
        folderSavingInfo.DataInfo.Checksum = folderDataDecChksum;
        folderSavingInfo.DataInfo.DataSize = folderData.Length;
        folderSavingInfo.DataInfo.UncompressedSize = folderData.Length;
        folderSavingInfo.DataInfo.BlockProperty = RhoBlockProperty.FullEncrypted;
        usedIndex.Add(folderDataIndex);
        savingInfo.Enqueue(folderSavingInfo);
        dataOffset = (dataOffset + folderSavingInfo.DataInfo.DataSize + 0xFF) & 0x7FFFFF00;
        foreach (var subFolder in folder.Folders)
            storeFolderAndFiles(subFolder, savingInfo, usedIndex, ref dataOffset, outRhoKey);
        while (fileSavingInfoQueue.Count > 0)
        {
            var fileSavingInfo = fileSavingInfoQueue.Dequeue();
            fileSavingInfo.DataInfo.Offset = dataOffset;
            savingInfo.Enqueue(fileSavingInfo);
            dataOffset = (dataOffset + fileSavingInfo.DataInfo.DataSize + 0xFF) & 0x7FFFFF00;
        }
    }

    private void releaseAllHandlers()
    {
        foreach (var handler in _fileHandlers.Values)
            handler.releaseHandler();
        _fileHandlers.Clear();
    }

    #endregion
}

// Static
public partial class RhoArchive
{
    #region Constants

    internal readonly string[] _rhLayerIdentifiers = new[]
    {
        "Rh layer spec 1.0",
        "Rh layer spec 1.1"
    };

    internal const string _rhLayerSecondText = "KartRider (veblush & dew)"; //Kartrider Rh layer author.

    #endregion
}