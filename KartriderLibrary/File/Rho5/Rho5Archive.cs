using Ionic.Zlib;
using KartLibrary.Consts;
using KartLibrary.Encrypt;
using KartLibrary.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace KartLibrary.File;

/// <summary>
///     Rho5Archive represent a set of same data pack Rho5 files.
/// </summary>
public class Rho5Archive
{
    #region Constructors

    public Rho5Archive()
    {
        _fileHandlers = new Dictionary<string, Rho5FileHandler>();
        _rho5Streams = new Dictionary<int, FileStream>();
        _dataBeginPoses = new Dictionary<int, int>();
    }

    #endregion

    #region Structs

    private class DataSavingInfo
    {
        public byte[] Data;
        public Rho5File? File;
    }

    #endregion

    #region Members

    // private FileStream? _rho5Stream;
    private readonly Dictionary<int, FileStream> _rho5Streams;
    private readonly Dictionary<string, Rho5FileHandler> _fileHandlers;
    private readonly Dictionary<int, int> _dataBeginPoses;

    #endregion

    #region Properties


    public bool IsClosed { get; }

    #endregion

    #region Methods

    public void SaveFolder(string dir, string dataPackName, string output, CountryCode code, int dataPackID = 0)
    {
        var files = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories);
        var queue = new Queue<Rho5File>();
        var array = files;
        foreach (var text in array)
        {
            var item = new Rho5File
            {
                DataSource = new FileDataSource(text),
                Name = text.Replace(dir, "").Replace("\\", "/")
            };
            queue.Enqueue(item);
        }

        saveSingleFileTo(output, dataPackName, dataPackID, getMixingString(code), queue, 1992294400, false);
    }

    public void Dispose()
    {
        foreach (var rho5Stream in _rho5Streams.Values)
        {
            if (rho5Stream.CanRead)
                rho5Stream.Close();
            rho5Stream.Dispose();
        }

        _rho5Streams.Clear();
        releaseAllHandles();
    }

    private void saveSingleFileTo(string dataPackPath, string dataPackName, int dataPackID, string mixingStr,
        Queue<Rho5File> allFileQueue, int maxSize, bool reopen)
    {
        var fullName = getDataPackFilePath(dataPackPath, dataPackName, dataPackID);
        var fullDirName = Path.GetDirectoryName(fullName) ?? "";
        if (!Directory.Exists(fullDirName)) throw new Exception("directory not exists.");
        var outFileName = Path.GetFileName(fullName);

        var tmpMemStream = new MemoryStream(Math.Min(maxSize, 21943040));
        var outEncryptStream = new Rho5EncryptStream(tmpMemStream);
        var outEncryptWriter = new BinaryWriter(outEncryptStream);

        if (_rho5Streams.ContainsKey(dataPackID) && _rho5Streams[dataPackID].Name == fullName) reopen = true;

        // Enqueue all files 
        maxSize = (int)Math.Round(maxSize * 1.05); // * 1.3. dataLenSum is sum of "uncompressed data" length.
        var filesInfoDataLen = 0;
        var dataLenSum = 0;
        var fileQueue = new Queue<Rho5File>();
        while (allFileQueue.Count > 0 && dataLenSum <= maxSize)
        {
            if (dataLenSum >= maxSize)
                break;
            var file = allFileQueue.Dequeue();
            filesInfoDataLen += 0x28 + (file.FullName.Length << 1);
            if (!file.HasDataSource)
                throw new Exception();
            dataLenSum += file.Size;
            fileQueue.Enqueue(file);
        }

        // Writes

        var headerOffset = getHeaderOffset(outFileName);
        var filesInfoOffset = headerOffset + getFilesInfoOffset(outFileName);

        Debug.Print($"{outFileName} {headerOffset} {filesInfoOffset}");

        outEncryptStream.SetLength(headerOffset);
        outEncryptStream.Seek(headerOffset, SeekOrigin.Begin);
        outEncryptStream.SetToHeaderKey(outFileName, mixingStr);
        var headerCrc = 2 + fileQueue.Count;
        outEncryptWriter.Write(headerCrc);
        outEncryptWriter.Write((byte)2);
        outEncryptWriter.Write(fileQueue.Count);

        outEncryptWriter.Flush();

        var filesInfoStream = new MemoryStream(filesInfoDataLen);
        var filesInfoWriter = new BinaryWriter(filesInfoStream);

        var dataBeginOffset = (filesInfoOffset + filesInfoDataLen + 0x3FF) & 0x7FFFFC00;
        var dataOffset = dataBeginOffset;
        outEncryptStream.SetLength(dataBeginOffset);
        while (fileQueue.Count > 0)
        {
            var file = fileQueue.Dequeue();
            if (!file.HasDataSource)
                throw new Exception();
            var data = file.GetBytes();
            byte[] processedData;
            var fileChksum = MD5.HashData(data);
            var encryptKey = Rho5Key.GetPackedFileKey(fileChksum, Rho5Key.GetFileKey_U1(mixingStr), file.FullName);
            using (var memStream = new MemoryStream())
            {
                var compressStream = new ZlibStream(memStream, CompressionMode.Compress, true);
                compressStream.Write(data, 0, data.Length);
                compressStream.Flush();
                compressStream.Close();
                processedData = memStream.ToArray();
                var encryptStream = new Rho5EncryptStream(memStream, encryptKey);
                encryptStream.Seek(0, SeekOrigin.Begin);
                encryptStream.Write(processedData, 0, Math.Min(processedData.Length, 0x400));
                encryptStream.Flush();
                processedData = memStream.ToArray();
            }

            var fileInfoChksum = 7 + ((dataOffset - dataBeginOffset) >> 10) + data.Length + processedData.Length;
            var array = fileChksum;
            foreach (var b in array)
                fileInfoChksum += b;
            filesInfoWriter.WriteKRString(file.FullName);
            filesInfoWriter.Write(fileInfoChksum);
            filesInfoWriter.Write(0x00000007);
            filesInfoWriter.Write((dataOffset - dataBeginOffset) >> 10);
            filesInfoWriter.Write(data.Length);
            filesInfoWriter.Write(processedData.Length);
            filesInfoWriter.Write(fileChksum);

            outEncryptStream.SetKey(encryptKey);
            outEncryptStream.Seek(dataOffset, SeekOrigin.Begin);
            outEncryptStream.Write(processedData, 0, processedData.Length);
            outEncryptStream.Flush();

            if (reopen)
            {
                var fileHandler = new Rho5FileHandler(this, dataPackID, (dataOffset - dataBeginOffset) >> 10,
                    data.Length, processedData.Length, encryptKey, fileChksum);
                file.DataSource = new Rho5DataSource(fileHandler);
                if (_fileHandlers.ContainsKey(file.FullName))
                {
                    _fileHandlers[file.FullName].releaseHandler();
                    _fileHandlers.Remove(file.FullName);
                }

                _fileHandlers.Add(file.FullName, fileHandler);
            }

            dataOffset = (dataOffset + processedData.Length + 0x3FF) & 0x7FFFFC00;
            outEncryptStream.SetLength(dataOffset);
        }

        outEncryptStream.Seek(filesInfoOffset, SeekOrigin.Begin);
        outEncryptStream.SetToFilesInfoKey(outFileName, mixingStr);
        var filesInfoData = filesInfoStream.ToArray();
        filesInfoStream.Close();
        outEncryptStream.Write(filesInfoData, 0, filesInfoData.Length);

        outEncryptStream.Flush();

        if (reopen)
        {
            if (_rho5Streams.ContainsKey(dataPackID))
            {
                _rho5Streams[dataPackID].Dispose();
                _rho5Streams.Remove(dataPackID);
            }

            if (_dataBeginPoses.ContainsKey(dataPackID)) _dataBeginPoses.Remove(dataPackID);
        }

        using (var outFileStream = new FileStream(fullName, FileMode.Create))
        {
            tmpMemStream.WriteTo(outFileStream);
        }

        if (reopen)
        {
            _rho5Streams.Add(dataPackID, new FileStream(fullName, FileMode.Open, FileAccess.Read));
            _dataBeginPoses.Add(dataPackID, dataBeginOffset);
        }

        tmpMemStream.Dispose();
    }

    private int getHeaderOffset(string fileName)
    {
        fileName = fileName.ToLower();
        var sum = 0;
        foreach (var c in fileName) sum += c;
        var mpl = (sum * 0xA41A41A5L) >> 32;
        var result = sum - (int)mpl;
        result >>= 1;
        result += (int)mpl;
        result >>= 8;
        result *= 0x138;
        result = sum - result + 0x1E;
        return result;
    }

    private int getFilesInfoOffset(string fileName)
    {
        fileName = fileName.ToLower();
        var sum = 0;
        foreach (var c in fileName) sum += c;
        sum *= 3;
        var mpl = (sum * 0x3521CFB3L) >> 32;
        var result = sum - (int)mpl;
        result >>= 1;
        result += (int)mpl;
        result >>= 7;
        result *= 0xD4;
        result = sum - result + 0x2A;
        return result;
    }

    private string getMixingString(CountryCode region)
    {
        switch (region)
        {
            case CountryCode.KR:
                return "y&errfV6GRS!e8JL";
            case CountryCode.CN:
                return "d$Bjgfc8@dH4TQ?k";
            case CountryCode.TW:
                return "t5rHKg-g9BA7%=qD";
            default:
                throw new Exception("");
        }
    }

    private string getDataPackFilePath(string dataPackPath, string dataPackName, int dataPackID)
    {
        return Path.Combine(Path.GetFullPath(dataPackPath), $"{dataPackName}_{dataPackID:00000}.rho5");
    }

    private void releaseAllHandles()
    {
        foreach (var fileHandler in _fileHandlers.Values)
            fileHandler.releaseHandler();
        _fileHandlers.Clear();
    }

    internal byte[] getData(Rho5FileHandler handler)
    {
        if (!_rho5Streams.ContainsKey(handler._dataPackID) || !_dataBeginPoses.ContainsKey(handler._dataPackID))
            throw new Exception("Invalid data pack id in file handler.");
        var rho5Stream = _rho5Streams[handler._dataPackID];
        if (rho5Stream is null || !rho5Stream.CanRead)
            throw new Exception("");
        var clonedStream = new FileStream(rho5Stream.SafeFileHandle, FileAccess.Read);
        var decryptStream = new Rho5DecryptStream(clonedStream, handler._key);
        var offset = _dataBeginPoses[handler._dataPackID] + (handler._offset << 10);
        decryptStream.Seek(offset, SeekOrigin.Begin);
        var compressedData = new byte[handler._compressedSize];
        var decompressedData = new byte[handler._decompressedSize];
        decryptStream.Read(compressedData, 0, compressedData.Length >= 0x400 ? 0x400 : compressedData.Length);
        if (compressedData.Length >= 0x400)
            clonedStream.Read(compressedData, 0x400, compressedData.Length - 0x400);
        using (var memStream = new MemoryStream(compressedData))
        {
            decryptStream = new Rho5DecryptStream(memStream, handler._key);
            var decompressStream = new ZlibStream(decryptStream, CompressionMode.Decompress);
            decompressStream.Read(decompressedData, 0, decompressedData.Length);
        }

        return decompressedData;
    }

    #endregion
}