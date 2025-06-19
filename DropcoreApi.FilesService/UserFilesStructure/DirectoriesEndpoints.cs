using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using static FilesEndpoints;

public static class DirectoriesEndpoints
{
    // Create

    public record CreateDirectoryRequestDto(string RootDirectoryPath, string Name);

    public static void CreateDirectory(ClaimsPrincipal claimsPrincipal, [FromBody] CreateDirectoryRequestDto request, UserFilesStructureService userFilesStructureService)
    {
        var directory = userFilesStructureService.GetUserDirectory(claimsPrincipal.GetUserUniqueId(), request.RootDirectoryPath, request.Name);

        directory.Create();
    }

    // Delete

    public static void DeleteDirectory(ClaimsPrincipal claimsPrincipal, [FromQuery] string path, UserFilesStructureService userFilesStructureService)
    {
        var directory = userFilesStructureService.GetUserDirectory(claimsPrincipal.GetUserUniqueId(), path);

        if (directory.Exists)
            directory.Delete(recursive: true);
    }

    // Get info

    public record DirectoryInfoResponseDto(
        string Name,
        string Path,

        ShortDirectoryInfoResponseDto[] Directories,
        FileInfoResponseDto[] Files
    )
    {
        public static DirectoryInfoResponseDto FromDirectoryInfo(DirectoryInfo directoryInfo)
        {
            var relativePath = directoryInfo.FullName.Substring(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dropcore-files").Length + 37);

            return new DirectoryInfoResponseDto(
                Name: directoryInfo.Name,
                Path: relativePath,
                Directories: directoryInfo.GetDirectories().Select(ShortDirectoryInfoResponseDto.FromDirectoryInfo).ToArray(),
                Files: directoryInfo.GetFiles().Select(FileInfoResponseDto.FromFileInfo).ToArray()
            );
        }
    }

    public record ShortDirectoryInfoResponseDto(
        string Name,
        string Path
    )
    {
        public static ShortDirectoryInfoResponseDto FromDirectoryInfo(DirectoryInfo directoryInfo)
        {
            var relativePath = directoryInfo.FullName.Substring(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dropcore-files").Length + 37);

            return new ShortDirectoryInfoResponseDto(
                Name: directoryInfo.Name,
                Path: relativePath
            );
        }
    }


    public static IResult GetDirectory(ClaimsPrincipal claimsPrincipal, [FromQuery] string? path, UserFilesStructureService userFilesStructureService)
    {
        var directory = userFilesStructureService.GetUserDirectory(claimsPrincipal.GetUserUniqueId(), path);

        if (directory.Exists)
            return Results.Ok(DirectoryInfoResponseDto.FromDirectoryInfo(directory));

        return Results.NotFound();
    }
}
