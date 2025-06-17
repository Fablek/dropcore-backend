using System.ComponentModel.DataAnnotations;

namespace FileService.Models;

public class FileMetadata
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = null!;
    public string StorageFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public string OwnerId { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
