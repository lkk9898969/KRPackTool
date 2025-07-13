using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using KartLibrary.Consts;
using KartLibrary.Data;
using KartLibrary.Xml;

namespace KartLibrary.File;

public class PackFolderManager
{
    public CountryCode regionCode = CountryCode.None;

    //private List<PackFolderInfo> RootFolder { get; init; } = new List<PackFolderInfo>();

    private PackFolderInfo RootFolder = new();
    public bool Initizated { get; private set; }

    private LinkedList<Rho> RhoPool { get; } = new();

    private LinkedList<Rho5> Rho5Pool { get; } = new();

    public void OpenDataFolder(string aaaPkFilePath)
    {
        var fileInfo = new FileInfo(aaaPkFilePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException(aaaPkFilePath);
        Reset();
        var fileStream = new FileStream(aaaPkFilePath, FileMode.Open);
        var br = new BinaryReader(fileStream);
        var dataLen = br.ReadInt32();
        var aaapkData = br.ReadKRData(dataLen);
        fileStream.Close();
        var bxmlDoc = new BinaryXmlDocument();
        bxmlDoc.Read(Encoding.GetEncoding("UTF-16"), aaapkData);
        var rootTag = bxmlDoc.RootTag;
        var ProcessQue = new Queue<ProcessObj>();
        if (rootTag.Name != "PackFolder" || rootTag.GetAttribute("name") != "KartRider")
            throw new NotSupportedException("aaa.pk file not support.");
        foreach (var subtag in rootTag.Children)
            ProcessQue.Enqueue(new ProcessObj
            {
                Obj = subtag,
                Parent = null,
                Path = ""
            });
        var fileNameSet = new HashSet<string>();
        while (ProcessQue.Count > 0)
        {
            var currectProcObj = ProcessQue.Dequeue();
            var currentTag = currectProcObj.Obj;
            switch (currentTag.Name)
            {
                case "PackFolder":
                    string subname = currentTag.GetAttribute("name");
                    var newSubFolder = new PackFolderInfo
                    {
                        FolderName = currectProcObj.Parent is null ? $"{subname}" : subname,
                        FullName = currectProcObj.Parent is null ? $"{subname}" : $"{currectProcObj.Path}/{subname}",
                        ParentFolder = currectProcObj.Parent
                    };
                    foreach (var subTag in currentTag.Children)
                    {
                        var newProcObj = new ProcessObj
                        {
                            Path = newSubFolder.FullName,
                            Parent = newSubFolder,
                            Obj = subTag
                        };
                        ProcessQue.Enqueue(newProcObj);
                    }

                    if (currectProcObj.Parent is not null)
                        currectProcObj.Parent.Folders.Add(newSubFolder);
                    else
                        RootFolder.Folders.Add(newSubFolder);
                    break;
                case "RhoFolder":
                    string name = currentTag.GetAttribute("name");
                    string fileName = currentTag.GetAttribute("fileName");
                    uint rhoFileKey = uint.Parse(currentTag.GetAttribute("key"));
                    if (fileNameSet.Contains(fileName))
                        continue;
                    fileNameSet.Add(fileName);
                    var NewFolder = new PackFolderInfo
                    {
                        FolderName = currectProcObj.Parent is null ? $"{name}" : name,
                        FullName = currectProcObj.Parent is null ? $"{name}" : $"{currectProcObj.Path}/{name}",
                        ParentFolder = currectProcObj.Parent
                    };
                    if (name == "")
                    {
                        NewFolder = currectProcObj.Parent;
                    }
                    else
                    {
                        if (currectProcObj.Parent is null)
                            RootFolder.Folders.Add(NewFolder);
                        else
                            currectProcObj.Parent.Folders.Add(NewFolder);
                    }

                    var rhoFile = new Rho($"{fileInfo.DirectoryName}\\{fileName}", rhoFileKey);
                    var dirQue = new Queue<(PackFolderInfo, RhoDirectory)>();
                    var rootDir = rhoFile.RootDirectory;
                    dirQue.Enqueue((NewFolder, rootDir));
                    while (dirQue.Count > 0)
                    {
                        var curObj = dirQue.Dequeue();
                        var currectPackFolder = curObj.Item1;
                        foreach (var file in curObj.Item2.GetFiles())
                        {
                            var newFileInfo = new PackFileInfo
                            {
                                FileName = file.FullFileName,
                                FileSize = file.FileSize,
                                FullName = $"{currectPackFolder.FullName}/{file.FullFileName}",
                                OriginalFile = file,
                                PackFileType = PackFileType.RhoFile
                            };
                            currectPackFolder.Files.Add(newFileInfo);
                        }

                        foreach (var dir in curObj.Item2.GetDirectories())
                        {
                            var subDirInfo = new PackFolderInfo
                            {
                                FolderName = dir.DirectoryName,
                                FullName = $"{currectPackFolder.FullName}/{dir.DirectoryName}",
                                ParentFolder = currectPackFolder
                            };
                            currectPackFolder.Folders.Add(subDirInfo);
                            dirQue.Enqueue((subDirInfo, dir));
                        }
                    }

                    RhoPool.AddLast(rhoFile);
                    break;
            }
        }

        var ZETA_Folders = GetDirectories("zeta");
        if (Array.Exists(ZETA_Folders, x => x.FolderName == "kr"))
            regionCode = CountryCode.KR;
        else if (Array.Exists(ZETA_Folders, x => x.FolderName == "cn"))
            regionCode = CountryCode.CN;
        else if (Array.Exists(ZETA_Folders, x => x.FolderName == "tw"))
            regionCode = CountryCode.TW;

        foreach (var file in Directory.GetFiles(fileInfo.DirectoryName, "*.rho5"))
        {
            var rho5File = new Rho5(file, regionCode);
            Rho5Pool.AddLast(rho5File);
            foreach (var rho5FileInfo in rho5File.Files)
            {
                var path = rho5FileInfo.FullPath;
                var path_part = path.Split('/');
                var currentFolder = new PackFolderInfo
                {
                    Folders = RootFolder.Folders,
                    FolderName = "",
                    FullName = "",
                    ParentFolder = null
                };
                var depth = path_part.Length;
                foreach (var part in path_part)
                {
                    if (depth <= 1)
                    {
                        currentFolder.Files.Add(new PackFileInfo
                        {
                            FileName = part,
                            FileSize = rho5FileInfo.DecompressedSize,
                            FullName = currentFolder.FullName == "" ? part : $"{currentFolder.FullName}/{part}",
                            PackFileType = PackFileType.Rho5File,
                            OriginalFile = rho5FileInfo
                        });
                    }
                    else
                    {
                        var foundFolder = currentFolder.Folders.Find(x => x.FolderName == part);
                        if (foundFolder is null)
                            currentFolder.Folders.Add(
                                foundFolder = new PackFolderInfo
                                {
                                    FolderName = part,
                                    ParentFolder = currentFolder,
                                    FullName = currentFolder.FullName == "" ? part : $"{currentFolder.FullName}/{part}"
                                });
                        currentFolder = foundFolder;
                    }

                    depth--;
                }
            }
        }

        for (var i = 0; i < RootFolder.Folders.Count; i++)
            RootFolder.Folders[i].ParentFolder = RootFolder;
        Initizated = true;
    }

    public async Task OpenDataFolderAsync(string aaaPkFilePath, ProgressChangedEventHandler? onProgressChange = null)
    {
    }

    public void OpenSingleFile(string rhoFile, CountryCode regionCode)
    {
        var fileInfo = new FileInfo(rhoFile);
        if (!fileInfo.Exists)
            throw new FileNotFoundException(rhoFile);
        Reset();
        if (rhoFile.EndsWith(".rho5"))
        {
            OpenRho5File(rhoFile, regionCode);
            return;
        }

        var rho = new Rho(rhoFile);
        var dirQue = new Queue<(PackFolderInfo, RhoDirectory)>();

        var rootFolder = new PackFolderInfo
        {
            FolderName = fileInfo.Name,
            FullName = $"{fileInfo.Name}",
            ParentFolder = null
        };
        RootFolder.Folders.Add(rootFolder);
        dirQue.Enqueue((rootFolder, rho.RootDirectory));

        while (dirQue.Count > 0)
        {
            var curObj = dirQue.Dequeue();
            var currectPackFolder = curObj.Item1;

            foreach (var file in curObj.Item2.GetFiles())
            {
                var newFileInfo = new PackFileInfo
                {
                    FileName = file.FullFileName,
                    FileSize = file.FileSize,
                    FullName = $"{currectPackFolder.FullName}/{file.FullFileName}",
                    OriginalFile = file,
                    PackFileType = PackFileType.RhoFile
                };
                currectPackFolder.Files.Add(newFileInfo);
            }

            foreach (var dir in curObj.Item2.GetDirectories())
            {
                var subDirInfo = new PackFolderInfo
                {
                    FolderName = dir.DirectoryName,
                    FullName = $"{currectPackFolder.FullName}/{dir.DirectoryName}",
                    ParentFolder = currectPackFolder
                };
                currectPackFolder.Folders.Add(subDirInfo);
                dirQue.Enqueue((subDirInfo, dir));
            }
        }

        RhoPool.AddLast(rho);
        for (var i = 0; i < RootFolder.Folders.Count; i++)
            RootFolder.Folders[i].ParentFolder = RootFolder;
        Initizated = true;
    }

    private void OpenRho5File(string file, CountryCode regionCode)
    {
        var rho = new Rho5(file, regionCode);
        Rho5Pool.AddLast(rho);
        var files = rho.Files;
        foreach (var rho5FileInfo in files)
        {
            var array = rho5FileInfo.FullPath.Split('/');
            var packFolderInfo = new PackFolderInfo
            {
                Folders = RootFolder.Folders,
                FolderName = "",
                FullName = "",
                ParentFolder = null
            };
            var num = array.Length;
            var array2 = array;
            foreach (var part in array2)
            {
                if (num <= 1)
                {
                    packFolderInfo.Files.Add(new PackFileInfo
                    {
                        FileName = part,
                        FileSize = rho5FileInfo.DecompressedSize,
                        FullName = packFolderInfo.FullName == "" ? part : packFolderInfo.FullName + "/" + part,
                        PackFileType = PackFileType.Rho5File,
                        OriginalFile = rho5FileInfo
                    });
                }
                else
                {
                    var packFolderInfo2 = packFolderInfo.Folders.Find(x => x.FolderName == part);
                    if ((object)packFolderInfo2 == null)
                    {
                        var folders = packFolderInfo.Folders;
                        var obj = new PackFolderInfo
                        {
                            FolderName = part,
                            ParentFolder = packFolderInfo,
                            FullName = packFolderInfo.FullName == "" ? part : packFolderInfo.FullName + "/" + part
                        };
                        packFolderInfo2 = obj;
                        folders.Add(obj);
                    }

                    packFolderInfo = packFolderInfo2;
                }

                num--;
            }
        }
    }

    public void OpenMultipleFiles(params string[] rhoFiles)
    {
        Reset();
        foreach (var rhoFile in rhoFiles)
        {
            var fileInfo = new FileInfo(rhoFile);
            if (!fileInfo.Exists)
                throw new FileNotFoundException(rhoFile);
            var rho = new Rho(rhoFile);
            var dirQue = new Queue<(PackFolderInfo, RhoDirectory)>();

            var rootFolder = new PackFolderInfo
            {
                FolderName = fileInfo.Name,
                FullName = $"{fileInfo.Name}",
                ParentFolder = null
            };
            RootFolder.Folders.Add(rootFolder);
            dirQue.Enqueue((rootFolder, rho.RootDirectory));

            while (dirQue.Count > 0)
            {
                var curObj = dirQue.Dequeue();
                var currectPackFolder = curObj.Item1;

                foreach (var file in curObj.Item2.GetFiles())
                {
                    var newFileInfo = new PackFileInfo
                    {
                        FileName = file.FullFileName,
                        FileSize = file.FileSize,
                        FullName = $"{currectPackFolder.FullName}/{file.FullFileName}",
                        OriginalFile = file,
                        PackFileType = PackFileType.RhoFile
                    };
                    currectPackFolder.Files.Add(newFileInfo);
                }

                foreach (var dir in curObj.Item2.GetDirectories())
                {
                    var subDirInfo = new PackFolderInfo
                    {
                        FolderName = dir.DirectoryName,
                        FullName = $"{currectPackFolder.FullName}/{dir.DirectoryName}",
                        ParentFolder = currectPackFolder
                    };
                    currectPackFolder.Folders.Add(subDirInfo);
                    dirQue.Enqueue((subDirInfo, dir));
                }
            }

            RhoPool.AddLast(rho);
            for (var i = 0; i < RootFolder.Folders.Count; i++)
                RootFolder.Folders[i].ParentFolder = RootFolder;
            Initizated = true;
        }
    }

    public void Reset()
    {
        Initizated = false;
        while (RhoPool.Count > 0)
        {
            RhoPool.First?.Value.Dispose();
            RhoPool.RemoveFirst();
        }

        while (Rho5Pool.Count > 0)
        {
            Rho5Pool.First?.Value.Dispose();
            Rho5Pool.RemoveFirst();
        }

        RootFolder = new PackFolderInfo
        {
            FolderName = "",
            FullName = "",
            ParentFolder = null
        };
    }

    public PackFolderInfo[]? GetDirectories(string Path)
    {
        if (Path == "")
            return RootFolder.Folders.ToArray();
        var path_sp = Path.Split('/');
        var currentFindFolders = RootFolder.Folders;
        foreach (var sp in path_sp)
        {
            var findFolder = currentFindFolders.Find(x => x.FolderName == sp);
            if (findFolder is null)
                return null;
            currentFindFolders = findFolder.Folders;
        }

        return currentFindFolders.ToArray();
    }

    public PackFileInfo[]? GetFiles(string Path)
    {
        if (Path == "")
            return new PackFileInfo[0];
        var path_sp = Path.Split('/');
        var currentFolder = new PackFolderInfo
        {
            Folders = RootFolder.Folders
        };
        var depth = path_sp.Length;
        foreach (var path in path_sp)
        {
            if (currentFolder == null)
                return null;
            if (depth == 1)
                return currentFolder.Files.ToArray();
            currentFolder = currentFolder.Folders.Find(x => x.FolderName == path);
            depth--;
        }

        return null;
    }

    public PackFileInfo? GetFile(string Path)
    {
        if (Path == "")
            return null;
        var path_sp = Path.Split('/');
        var currentFolder = new PackFolderInfo
        {
            Folders = RootFolder.Folders
        };
        var depth = path_sp.Length;
        var find_files = new List<PackFileInfo>();
        foreach (var path in path_sp)
        {
            if (currentFolder == null)
                return null;
            if (depth == 1)
            {
                find_files = currentFolder.Files;
                break;
            }

            currentFolder = currentFolder.Folders.Find(x => x.FolderName == path);
            depth--;
        }

        var output_file = find_files.Find(x => x.FileName == path_sp[^1]);
        if (output_file is not null)
            return output_file;
        return null;
    }

    public PackFolderInfo GetRootFolder()
    {
        return (PackFolderInfo)RootFolder.Clone();
    }

    private struct ProcessObj
    {
        public string Path;

        public BinaryXmlTag Obj;

        public PackFolderInfo Parent;
    }
}

public class PackFolderInfo : ICloneable
{
    // Constructors
    public PackFolderInfo()
    {
        FolderName = "";
        FullName = "";
        ParentFolder = null;
    }

    public PackFolderInfo(string folderName, string fullName, PackFolderInfo? parentFolder,
        IEnumerable<PackFolderInfo> folders, IEnumerable<PackFileInfo> files)
    {
        FolderName = folderName;
        FullName = fullName;
        ParentFolder = parentFolder;
        Folders = new List<PackFolderInfo>(folders);
        Files = new List<PackFileInfo>(files);
    }

    // Properties
    public string FolderName { get; set; }
    public string FullName { get; set; }
    public PackFolderInfo? ParentFolder { get; internal set; }
    internal List<PackFolderInfo> Folders { get; init; } = new();
    internal List<PackFileInfo> Files { get; init; } = new();

    public object Clone()
    {
        var clone_obj = new PackFolderInfo
        {
            FolderName = FolderName,
            FullName = FullName,
            ParentFolder = ParentFolder
        };
        var queue = new Queue<(PackFolderInfo parent, PackFolderInfo proc_folder)>();
        foreach (var sub_folder in Folders) queue.Enqueue((clone_obj, sub_folder));
        foreach (var sub_file in Files) clone_obj.Files.Add((PackFileInfo)sub_file.Clone());
        while (queue.Count > 0)
        {
            var curent_proc_obj = queue.Dequeue();
            var old_obj = curent_proc_obj.proc_folder;
            var new_obj_par = curent_proc_obj.parent;
            var cur_clone_obj = new PackFolderInfo
            {
                FolderName = old_obj.FolderName,
                FullName = old_obj.FullName,
                ParentFolder = new_obj_par
            };
            new_obj_par.Folders.Add(cur_clone_obj);
            foreach (var obj_sub_folder in curent_proc_obj.proc_folder.Folders)
                queue.Enqueue((cur_clone_obj, obj_sub_folder));
            foreach (var obj_sub_file in curent_proc_obj.proc_folder.Files)
                cur_clone_obj.Files.Add((PackFileInfo)obj_sub_file.Clone());
        }

        return clone_obj;
    }

    public PackFileInfo[] GetFilesInfo()
    {
        return Files.ToArray();
    }

    public PackFolderInfo[] GetFoldersInfo()
    {
        return Folders.ToArray();
    }

    // Operator Overloads
    public static bool operator ==(PackFolderInfo objA, PackFolderInfo objB)
    {
        return objA is not null && objB is not null && objA.FullName is not null && objB.FullName is not null &&
               objA.FullName == objB.FullName;
    }

    public static bool operator !=(PackFolderInfo objA, PackFolderInfo objB)
    {
        return !(objA is not null && objB is not null && objA.FullName is not null && objB.FullName is not null &&
                 objA.FullName == objB.FullName);
    }

    // Overrides
    public override bool Equals(object? obj)
    {
        if (obj is PackFolderInfo folderInfo) return folderInfo == this;

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode() + FullName.GetHashCode();
    }
}

public class PackFileInfo : ICloneable
{
    public string FileName { get; set; }
    public string FullName { get; set; }
    public int FileSize { get; set; }
    public PackFileType PackFileType { get; set; }
    public object OriginalFile { get; set; }

    public object Clone()
    {
        var clone_obj = new PackFileInfo
        {
            FileName = FileName,
            FullName = FullName,
            FileSize = FileSize,
            PackFileType = PackFileType,
            OriginalFile = OriginalFile
        };
        return clone_obj;
    }

    public byte[] GetData()
    {
        if (PackFileType == PackFileType.RhoFile && OriginalFile is RhoFileInfo fileInfo) return fileInfo.GetData();

        if (PackFileType == PackFileType.Rho5File && OriginalFile is Rho5FileInfo file5Info) return file5Info.GetData();

        return null;
    }

    //Operator Overloads
    public static bool operator ==(PackFileInfo objA, PackFileInfo objB)
    {
        return objA is not null && objB is not null && objA.FullName is not null && objB.FullName is not null &&
               objA.FullName == objB.FullName;
    }

    public static bool operator !=(PackFileInfo objA, PackFileInfo objB)
    {
        return !(objA is not null && objB is not null && objA.FullName is not null && objB.FullName is not null &&
                 objA.FullName == objB.FullName);
    }

    // Overrides
    public override bool Equals(object? obj)
    {
        if (obj is PackFileInfo fileInfo) return fileInfo == this;

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode() - FullName.GetHashCode();
    }
}

public enum PackFileType
{
    RhoFile,
    Rho5File
}