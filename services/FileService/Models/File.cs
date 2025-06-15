namespace FileService.Models;

public class FileMetadata
{
    public int Id { get; set; }
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string OwnerId { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
