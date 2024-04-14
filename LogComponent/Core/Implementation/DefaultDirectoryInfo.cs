using LogComponent.Core.Interfaces;

namespace LogComponent.Core.Implementation;

public sealed class DefaultDirectoryInfo : IDirectoryInfo
{
    public DefaultDirectoryInfo(string folder)
    {
        Folder = folder;
    }

    public string Folder { get; }

    public void EnsureFolderCreated()
    {
        if (!Directory.Exists(Folder))
        {
            Directory.CreateDirectory(Folder);
        }
    }
}
