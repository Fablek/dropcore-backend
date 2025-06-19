using DropcoreApi.Core.Types;

public class UserFilesStructureService(FilesConfig config)
{
    public DirectoryInfo GetUserHomeDirectory(UniqueId userId) => new(Path.Combine(
        config.RootDirectory.FullName, userId.Guid.ToString()
    ));

    public DirectoryInfo GetUserDirectory(UniqueId userId, params string[] path) => new(Path.Combine([
        config.RootDirectory.FullName, userId.Guid.ToString(), .. FilterPathParts(path)
    ]));

    public FileInfo GetUserFileInfo(UniqueId userId, params string[] path) => new(Path.Combine([
        config.RootDirectory.FullName, userId.Guid.ToString() , .. FilterPathParts(path)
    ]));

    static IEnumerable<string> FilterPathParts(IEnumerable<string> parts) => parts.Where(p => !string.IsNullOrWhiteSpace(p) && p != "\\" && p != "/");
}