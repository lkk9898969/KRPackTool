using System;
using System.Collections.Generic;

namespace KartLibrary.File;

public interface IRhoFolder : IDisposable
{

    IReadOnlyCollection<IRhoFile> Files { get; }

    IReadOnlyCollection<IRhoFolder> Folders { get; }

    string Name { get; }

    string FullName { get; }
}

public interface IRhoFolder<TFolder, TFile> : IRhoFolder
    where TFolder : IRhoFolder<TFolder, TFile> where TFile : IRhoFile
{
    new IReadOnlyCollection<TFile> Files { get; }

    new IReadOnlyCollection<TFolder> Folders { get; }
}