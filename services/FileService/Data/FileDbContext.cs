using Microsoft.EntityFrameworkCore;
using FileService.Models;
using System.Collections.Generic;

namespace FileService.Data;

public class FileDbContext : DbContext
{
    public FileDbContext(DbContextOptions<FileDbContext> options) : base(options) { }

    public DbSet<FileMetadata> Files => Set<FileMetadata>();
}
