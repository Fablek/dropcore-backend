namespace FileService.Models;

public class FileMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FileName { get; set; } = null!;

    public string StorageFileName { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSize { get; set; }

    public string OwnerId { get; set; } = null!;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
