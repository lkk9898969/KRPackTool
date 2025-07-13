using System;
using System.Collections.Generic;
using System.Text;
using KartLibrary.IO;

namespace KartLibrary.File;

public class RhoFolder : IRhoFolder<RhoFolder, RhoFile>, IModifiableRhoFolder<RhoFolder, RhoFile>
{
    #region Members

    private string _name;

    private Dictionary<string, RhoFile> _files;
    private Dictionary<string, RhoFolder> _folders;

    private uint _prevParentUpdatsCounter = 0xBAD_BEEFu;
    private uint _fullnameUpdatesCounter = 0x14325768u;
    private uint? _folderDataIndex;
    private bool _prevCounterInitialized;

    private string _parentFullname = "";

    private string _originalName;
    private readonly HashSet<RhoFile> _addedFiles;
    private HashSet<RhoFile> _removedFiles;
    private readonly HashSet<RhoFolder> _addedFolders;
    private HashSet<RhoFolder> _removedFolders;

    private readonly bool _isRootFolder;
    private bool _disposed;

    #endregion

    #region Properties

    public string Name
    {
        get => _name;
        set
        {
            if (_isRootFolder)
                throw new InvalidOperationException("cannot set the name of root folder.");
            _name = value;
            _folderDataIndex = null;
            var affectedFolders = new Queue<RhoFolder>();
            affectedFolders.Enqueue(this);
            while (affectedFolders.Count > 0)
            {
                var affectedFolder = affectedFolders.Dequeue();
                affectedFolder._fullnameUpdatesCounter += 0x4B9AD755u;
                foreach (var folder in affectedFolder.Folders)
                    affectedFolders.Enqueue(folder);
            }
        }
    }

    public string FullName
    {
        get
        {
            if (Parent is null)
                return _name;
            if (!_prevCounterInitialized || _prevParentUpdatsCounter != Parent._fullnameUpdatesCounter)
            {
                _parentFullname = Parent.FullName;
                _prevParentUpdatsCounter = Parent._fullnameUpdatesCounter;
                _prevCounterInitialized = true;
            }

            return Parent._name.Length > 0 ? $"{_parentFullname}/{_name}" : _name;
        }
    }

    public RhoFolder? Parent { get; private set; }

    IRhoFolder? IRhoFolder.Parent => Parent;

    IModifiableRhoFolder? IModifiableRhoFolder.Parent => Parent;

    public IReadOnlyCollection<RhoFile> Files => _files.Values;

    public IReadOnlyCollection<RhoFolder> Folders => _folders.Values;

    IReadOnlyCollection<IRhoFile> IRhoFolder.Files => Files;

    IReadOnlyCollection<IModifiableRhoFile> IModifiableRhoFolder.Files => Files;

    IReadOnlyCollection<IRhoFolder> IRhoFolder.Folders => Folders;

    IReadOnlyCollection<IModifiableRhoFolder> IModifiableRhoFolder.Folders => Folders;

    internal bool HasModified =>
        _addedFiles.Count > 0 ||
        _addedFolders.Count > 0 ||
        _removedFiles.Count > 0 ||
        _removedFolders.Count > 0 ||
        _originalName != _name ||
        checkIfFilesModified();

    #endregion

    #region Constructors

    public RhoFolder()
    {
        _originalName = "";
        _name = "";
        _files = new Dictionary<string, RhoFile>();
        _folders = new Dictionary<string, RhoFolder>();
        _addedFiles = new HashSet<RhoFile>();
        _addedFolders = new HashSet<RhoFolder>();
        _removedFiles = new HashSet<RhoFile>();
        _removedFolders = new HashSet<RhoFolder>();
        Parent = null;
        _disposed = false;
        _isRootFolder = false;
        _prevCounterInitialized = false;
    }

    internal RhoFolder(bool isRootFolder) : this()
    {
        _isRootFolder = true;
    }

    #endregion

    #region Methods

    public RhoFile? GetFile(string path)
    {
        var splittedPath = path.Split('/');
        var findFolders = this;
        for (var i = 0; i < splittedPath.Length - 1; i++)
        {
            var folderName = splittedPath[i];
            if (folderName == ".")
                continue;
            if (folderName == "..")
                if (findFolders.Parent is null)
                    return null;
                else
                    findFolders = findFolders.Parent;
            else if (!findFolders._folders.ContainsKey(folderName))
                return null;
            else
                findFolders = findFolders._folders[folderName];
        }

        if (splittedPath.Length >= 1 && findFolders._files.ContainsKey(splittedPath[^1]))
            return findFolders._files[splittedPath[^1]];

        return null;
    }

    public RhoFolder? GetFolder(string path)
    {
        var splittedPath = path.Split('/');
        var findFolder = this;
        for (var i = 0; i < splittedPath.Length; i++)
        {
            var folderName = splittedPath[i];
            if (folderName == ".")
                continue;
            if (folderName == "..")
                if (findFolder.Parent is null)
                    return null;
                else
                    findFolder = findFolder.Parent;
            else if (!findFolder._folders.ContainsKey(folderName))
                return null;
            else
                findFolder = findFolder._folders[folderName];
        }

        return findFolder;
    }

    public void AddFile(RhoFile file)
    {
        if (_files.ContainsKey(file.Name))
            throw new Exception($"File: {file.Name} is exist.");
        if (file._parentFolder is not null)
            throw new Exception("The parent of a file you want to add is not null.");
        if (_removedFiles.Contains(file))
            _removedFiles.Remove(file);
        else
            _addedFiles.Add(file);
        _files.Add(file.Name, file);
        file._parentFolder = this;
    }

    public void AddFile(string path, RhoFile file)
    {
        var splittedPath = path.Split('/');
        var findFolders = this;
        var folderNameList = new List<string>();
        for (var i = 0; i < splittedPath.Length; i++)
        {
            var folderName = splittedPath[i];
            folderNameList.Add(folderName);
            if (!findFolders._folders.ContainsKey(folderName))
                throw new Exception($"Folder: {string.Join('/', folderNameList.ToArray())} can not be found.");
            findFolders = findFolders._folders[folderName];
        }

        if (splittedPath.Length > 1)
        {
            var fileName = file.Name;
            if (!findFolders._files.ContainsKey(fileName))
            {
                if (file.Parent is not null) throw new Exception("The parent of adding file is in other folder.");

                findFolders.AddFile(file);
            }
            else
            {
                throw new Exception($"File: {path}/{fileName} is exist.");
            }
        }
        else
        {
            throw new Exception($"Path: {path} is invalid.");
        }
    }

    public void AddFolder(RhoFolder folder)
    {
        if (_folders.ContainsKey(folder.Name))
            throw new Exception($"Folder: {folder.Name} is exist.");
        if (folder.Parent is not null)
            throw new Exception("The parent of a folder you want to add is not null.");
        if (_removedFolders.Contains(folder))
            _removedFolders.Remove(folder);
        else
            _addedFolders.Add(folder);
        _folders.Add(folder.Name, folder);
        folder.Parent = this;
        folder._prevCounterInitialized = false;
        folder.Name = folder._name;
    }

    public void AddFolder(string path, RhoFolder folder)
    {
        var splittedPath = path.Split('/');
        var findFolders = this;
        var folderNameList = new List<string>();
        for (var i = 0; i < splittedPath.Length; i++)
        {
            var folderName = splittedPath[i];
            folderNameList.Add(folderName);
            if (!findFolders._folders.ContainsKey(folderName))
                throw new Exception($"Folder: {string.Join('/', folderNameList.ToArray())} can not be found.");
            findFolders = findFolders._folders[folderName];
        }

        if (splittedPath.Length > 1)
        {
            var folderName = folder.Name;
            if (!findFolders._folders.ContainsKey(folderName))
                findFolders.AddFolder(folder);
            else
                throw new Exception($"Folder: {path}/{folderName} is exist.");
        }
        else
        {
            throw new Exception($"Path: {path} is invalid.");
        }
    }

    public bool ContainsFolder(string path)
    {
        var splittedPath = path.Split('/');
        var findFolder = this;
        for (var i = 0; i < splittedPath.Length; i++)
        {
            var folderName = splittedPath[i];
            if (findFolder._folders.ContainsKey(folderName))
                findFolder = findFolder._folders[folderName];
            else
                return false;
        }

        return splittedPath.Length > 0;
    }

    public bool ContainsFile(string path)
    {
        var splittedPath = path.Split('/');
        var findFolder = this;
        for (var i = 0; i < splittedPath.Length - 1; i++)
        {
            var folderName = splittedPath[i];
            if (findFolder._folders.ContainsKey(folderName))
                findFolder = findFolder._folders[folderName];
            else
                return false;
        }

        if (splittedPath.Length > 0)
            return findFolder._files.ContainsKey(splittedPath[^1]);
        return false;
    }

    public bool RemoveFile(string fileFullName)
    {
        var splittedPath = fileFullName.Split('/');
        var findFolder = this;
        for (var i = 0; i < splittedPath.Length - 1; i++)
        {
            var folderName = splittedPath[i];
            if (findFolder._folders.ContainsKey(folderName))
                findFolder = findFolder._folders[folderName];
            else
                return false;
        }

        if (splittedPath.Length > 0 && findFolder._files.ContainsKey(splittedPath[^1]))
        {
            findFolder.RemoveFile(findFolder._files[splittedPath[^1]]);
            return true;
        }

        return false;
    }

    public bool RemoveFile(RhoFile fileToDelete)
    {
        if (_files.ContainsKey(fileToDelete.Name) && _files[fileToDelete.Name] == fileToDelete)
        {
            if (_addedFiles.Contains(fileToDelete))
                _addedFiles.Remove(fileToDelete);
            else
                _removedFiles.Add(fileToDelete);
            _files.Remove(fileToDelete.Name);
            fileToDelete._parentFolder = null;
            return true;
        }

        return false;
    }

    public bool RemoveFolder(string folderFullName)
    {
        var splittedPath = folderFullName.Split('/');
        var findFolder = this;
        for (var i = 0; i < splittedPath.Length - 1; i++)
        {
            var folderName = splittedPath[i];
            if (findFolder._folders.ContainsKey(folderName))
                findFolder = findFolder._folders[folderName];
            else
                return false;
        }

        if (splittedPath.Length > 0 && findFolder._folders.ContainsKey(splittedPath[^1]))
        {
            findFolder.RemoveFolder(findFolder._folders[splittedPath[^1]]);
            return true;
        }

        return false;
    }

    public bool RemoveFolder(RhoFolder folderToDelete)
    {
        if (_folders.ContainsKey(folderToDelete.Name) && _folders[folderToDelete.Name] == folderToDelete)
        {
            if (_addedFolders.Contains(folderToDelete))
                _addedFolders.Remove(folderToDelete);
            else
                _removedFolders.Add(folderToDelete);
            _folders.Remove(folderToDelete.Name);
            folderToDelete.Parent = null;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        foreach (var folder in _folders.Values)
            _removedFolders.Add(folder);
        foreach (var file in _files.Values)
            _removedFiles.Add(file);
        _folders.Clear();
        _files.Clear();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        if (disposing)
        {
            foreach (var folder in _folders.Values)
                folder.Dispose();
            foreach (var file in _files.Values)
                file.Dispose();
            foreach (var folder in _removedFolders)
                folder.Dispose();
            foreach (var file in _removedFiles)
                file.Dispose();
            _folders.Clear();
            _files.Clear();
            _removedFolders.Clear();
            _removedFiles.Clear();
        }

        _folders = null;
        _files = null;
        _removedFolders = null;
        _removedFiles = null;
        Parent = null;
        _disposed = true;
    }

    public override string ToString()
    {
        return $"RhoFolder:{FullName}";
    }

    IRhoFile? IRhoFolder.GetFile(string path)
    {
        return GetFile(path);
    }

    IRhoFolder? IRhoFolder.GetFolder(string path)
    {
        return GetFolder(path);
    }

    IModifiableRhoFile? IModifiableRhoFolder.GetFile(string path)
    {
        return GetFile(path);
    }

    IModifiableRhoFolder? IModifiableRhoFolder.GetFolder(string path)
    {
        return GetFolder(path);
    }

    void IModifiableRhoFolder.AddFile(IModifiableRhoFile file)
    {
        if (file is RhoFile rhoFile)
            AddFile(rhoFile);
        else
            throw new Exception("");
    }

    void IModifiableRhoFolder.AddFile(string path, IModifiableRhoFile file)
    {
        if (file is RhoFile rhoFile)
            AddFile(path, rhoFile);
        else
            throw new Exception("");
    }

    void IModifiableRhoFolder.AddFolder(IModifiableRhoFolder folder)
    {
        if (folder is RhoFolder rhoFolder)
            AddFolder(rhoFolder);
        else
            throw new Exception("");
    }

    void IModifiableRhoFolder.AddFolder(string path, IModifiableRhoFolder folder)
    {
        if (folder is RhoFolder rhoFolder)
            AddFolder(path, rhoFolder);
        else
            throw new Exception("");
    }

    internal uint getFolderDataIndex()
    {
        if (Parent is not null && _prevParentUpdatsCounter != Parent._fullnameUpdatesCounter)
            _folderDataIndex = null;
        if (_folderDataIndex is null)
        {
            if (_name.Length == 0 && Parent is null)
                return 0xFFFFFFFFu;
            var fullName = FullName;
            var fullNameEncData = Encoding.Unicode.GetBytes(fullName);
            _folderDataIndex = Adler.Adler32(0, fullNameEncData, 0, fullNameEncData.Length);
        }

        return _folderDataIndex.Value;
    }

    internal void appliedChanges()
    {
        _originalName = _name;
        _addedFiles.Clear();
        _addedFolders.Clear();
        _removedFiles.Clear();
        _removedFolders.Clear();
    }

    private bool checkIfFilesModified()
    {
        foreach (var file in _files.Values)
            if (file.IsModified)
                return true;
        return false;
    }

    #endregion
}