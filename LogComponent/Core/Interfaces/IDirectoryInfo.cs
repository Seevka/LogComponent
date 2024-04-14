namespace LogComponent.Core.Interfaces;

public interface IDirectoryInfo
{
    public string Folder { get; }

    public void EnsureFolderCreated();
}
