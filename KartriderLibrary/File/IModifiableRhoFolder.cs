using System;
using System.Collections.Generic;

namespace KartLibrary.File;

public interface IModifiableRhoFolder : IDisposable
{

    IReadOnlyCollection<IModifiableRhoFile> Files { get; }

    IReadOnlyCollection<IModifiableRhoFolder> Folders { get; }

    string Name { get; set; }
}

/// <summary>
///     A generic <see cref="IModifiableRhoFolder" />. See also <seealso cref="IModifiableRhoFolder" />
/// </summary>
/// <typeparam name="TFolder">Type of folder can be stored in a instance of this type.</typeparam>
/// <typeparam name="TFile"></typeparam>
public interface IModifiableRhoFolder<TFolder, TFile> : IModifiableRhoFolder where TFolder : IModifiableRhoFolder
    where TFile : IModifiableRhoFile
{
    new IReadOnlyCollection<TFile> Files { get; }

    new IReadOnlyCollection<TFolder> Folders { get; }
}