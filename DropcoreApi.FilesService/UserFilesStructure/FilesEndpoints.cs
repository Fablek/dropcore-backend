using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

public static class FilesEndpoints
{
    // Create

    public record CreateFileRequestDto(
        string DirectoryPath, 
        string Name
    );

    public static void CreateFile(ClaimsPrincipal claimsPrincipal, [FromBody] CreateFileRequestDto request, UserFilesStructureService userFilesStructureService)
    {
        var file = userFilesStructureService.GetUserFileInfo(claimsPrincipal.GetUserUniqueId(), request.DirectoryPath, request.Name);

        file.Directory!.Create();
        file.Create().Close();
    }

    // Delete

    public static void DeleteFile(ClaimsPrincipal claimsPrincipal, [FromQuery] string path, UserFilesStructureService userFilesStructureService)
    {
        var file = userFilesStructureService.GetUserFileInfo(claimsPrincipal.GetUserUniqueId(), path);

        if (file.Exists)
            file.Delete();
    }

    // Get info

    public record FileInfoResponseDto(
        string Name,
        string Extension,
        string Path,
        long SizeInBytes,

        string UploadLink,
        string DownloadLink
    )
    {
        public static FileInfoResponseDto FromFileInfo(FileInfo fileInfo)
        {
            var relativePath = fileInfo.FullName.Substring(SettingsHelper.GetRootDirectory().FullName.Length + 37);

            return new FileInfoResponseDto(
                Name: fileInfo.Name,
                Extension: fileInfo.Extension,
                Path: relativePath,
                SizeInBytes: fileInfo.Length,
                UploadLink: $"{SettingsHelper.GetBaseUrl()}/file/upload/byform?parentPath={relativePath}",
                DownloadLink: $"{SettingsHelper.GetBaseUrl()}/file/download?parentPath={relativePath}"
            );
        }
    }

    public static IResult GetFileInfo(ClaimsPrincipal claimsPrincipal, [FromQuery] string path, UserFilesStructureService userFilesStructureService)
    {
        var file = userFilesStructureService.GetUserFileInfo(claimsPrincipal.GetUserUniqueId(), path);

        return file.Exists ? Results.Ok(FileInfoResponseDto.FromFileInfo(file)) : Results.NotFound();
    }

    // Download

    public static IResult DownloadFile(ClaimsPrincipal claimsPrincipal, [FromQuery] string path, UserFilesStructureService userFilesStructureService)
    {
        var file = userFilesStructureService.GetUserFileInfo(claimsPrincipal.GetUserUniqueId(), path);
        return file.Exists ? Results.File(file.FullName) : Results.NotFound();
    }

    // Upload

    public static async Task UploadFile(ClaimsPrincipal claimsPrincipal, [FromQuery] string directoryPath, [FromQuery] string fileName, HttpRequest httpRequest, UserFilesStructureService userFilesStructureService)
    {
        var file = userFilesStructureService.GetUserFileInfo(claimsPrincipal.GetUserUniqueId(), directoryPath, fileName);

        await SaveStreamToLocalFile(file, httpRequest.Body);
    }

    public static async Task UploadFileByForm(ClaimsPrincipal claimsPrincipal, [FromForm] string directoryPath, IFormFile fromFormFile, UserFilesStructureService userFilesStructureService)
    {
        var localFile = userFilesStructureService.GetUserFileInfo(claimsPrincipal.GetUserUniqueId(), directoryPath, fromFormFile.FileName);

        using var fromFormFileStream = fromFormFile.OpenReadStream();
        await SaveStreamToLocalFile(localFile, fromFormFileStream);
    }

    static async Task SaveStreamToLocalFile(FileInfo file, Stream stream)
    {
        if (!file.Directory.Exists)
            file.Directory!.Create();

        using var fileStream = file.Open(FileMode.OpenOrCreate, FileAccess.Write);

        var buffer = new byte[1024 * 256];
        var length = 0;

        do
        {
            length = await stream.ReadAsync(buffer);
            await fileStream.WriteAsync(buffer, 0, length);
        } while (length == buffer.Length);
    }
}
