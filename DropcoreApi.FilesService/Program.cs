using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(config =>
{
    config.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services
    .AddAuthorization()
    .AddAuthentication(config =>
    {
        config.DefaultAuthenticateScheme =
        config.DefaultScheme =
        config.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer("Bearer", config =>
    {
        config.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateAudience = true,
            ValidAudience = "localhost",
            ValidateIssuer = true,
            ValidIssuer = "localhost",

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my secret for jwt token 123456 long long long long long"))
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app
    .UseHttpsRedirection()
    .UseAuthentication()
    .UseAuthorization();

static string GetRootPath() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dropcore-files");

var root = new DirectoryInfo(GetRootPath());

DirectoryInfo GetUserDirectoryInfo(ClaimsPrincipal claimsPrincipal, params string[] path)
{
    var userId = claimsPrincipal.Claims.Single(c => c.Type == "user-id").Value;

    var homeDirectory = new DirectoryInfo(Path.Combine(root.FullName, userId.ToString()));

    if (!homeDirectory.Exists)
        homeDirectory.Create();

    return new DirectoryInfo(Path.Combine([homeDirectory.FullName, .. path]));
}

app.MapPost("/directory", (ClaimsPrincipal claimsPrincipal, [FromBody] CreateDirectoryRequestDto request) =>
{
    var target = GetUserDirectoryInfo(claimsPrincipal, request.ParentPath, request.Name);

    target.Create();
}).DisableAntiforgery().RequireAuthorization();

app.MapDelete("/directory", (ClaimsPrincipal claimsPrincipal, [FromBody] DeleteDirectoryRequestDto request) =>
{
    var target = GetUserDirectoryInfo(claimsPrincipal, request.Path);

    target.Delete(recursive: true);
}).DisableAntiforgery().RequireAuthorization();

app.MapGet("/directory", (ClaimsPrincipal claimsPrincipal, [FromQuery] string path) =>
{
    var target = GetUserDirectoryInfo(claimsPrincipal, path);

    if (target.Exists)
        return Results.Ok(DirectoryInfoResponseDto.FromDirectoryInfo(target));

    return Results.NotFound();
}).DisableAntiforgery().RequireAuthorization();

app.MapPost("/file", (ClaimsPrincipal claimsPrincipal, [FromBody] CreateFileRequestDto request) =>
{
    var directory = GetUserDirectoryInfo(claimsPrincipal, request.ParentPath);
    var fileInfo = new FileInfo(Path.Combine(directory.FullName, $"{request.Name}{request.Extension}"));

    directory.Create();
    fileInfo.Create().Close();
}).DisableAntiforgery().RequireAuthorization();

app.MapDelete("/file", (ClaimsPrincipal claimsPrincipal, [FromQuery] string path) =>
{
    var target = new FileInfo(Path.Combine(GetUserDirectoryInfo(claimsPrincipal).FullName, path));

    if (target.Exists)
    {
        target.Delete();
        return Results.Ok();
    }

    return Results.NotFound();
}).DisableAntiforgery().RequireAuthorization();

app.MapGet("/file", (ClaimsPrincipal claimsPrincipal, [FromQuery] string path) =>
{
    var file = new FileInfo(Path.Combine(GetUserDirectoryInfo(claimsPrincipal).FullName, path));

    if (file.Exists)
        return Results.Ok(FileInfoResponseDto.FromFileInfo(file));

    return Results.NotFound();
}).DisableAntiforgery().RequireAuthorization();

app.MapPost("/file/upload/byform", async (ClaimsPrincipal claimsPrincipal, [FromForm] string parentPath, IFormFile file) =>
{
    var fileInfo = new FileInfo(Path.Combine(GetUserDirectoryInfo(claimsPrincipal, parentPath).FullName, file.FileName));

    using var fileStream = fileInfo.OpenWrite();
    using var inputFileStream = file.OpenReadStream();

    var buffer = new byte[1024 * 16];
    var length = 0;

    do
    {
        length = await inputFileStream.ReadAsync(buffer);
        await fileStream.WriteAsync(buffer, 0, length);
    } while (length == buffer.Length);
}).RequireAuthorization().DisableAntiforgery();

app.MapPost("/file/upload", async (ClaimsPrincipal claimsPrincipal, [FromQuery] string path, HttpRequest httpRequest) =>
{
    var fileInfo = new FileInfo(Path.Combine(GetUserDirectoryInfo(claimsPrincipal).FullName, path));

    using var file = fileInfo.OpenWrite();

    var buffer = new byte[1024 * 16];
    var length = 0;

    do
    {
        length = await httpRequest.Body.ReadAsync(buffer);
        await file.WriteAsync(buffer, 0, length);
    } while (length == buffer.Length);
}).RequireAuthorization();

app.MapGet("/file/download", (ClaimsPrincipal claimsPrincipal, [FromQuery] string path) =>
{
    var target = new FileInfo(Path.Combine(GetUserDirectoryInfo(claimsPrincipal).FullName, path));

    if (target.Exists)
        return Results.File(target.FullName);

    return Results.NotFound();
}).DisableAntiforgery().RequireAuthorization();

app.Run();

public record CreateDirectoryRequestDto(string ParentPath, string Name);
public record DeleteDirectoryRequestDto(string Path);

public record CreateFileRequestDto(string ParentPath, string Name, string Extension);

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
        var relativePath = fileInfo.FullName.Substring(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dropcore-files").Length + 37);

        return new FileInfoResponseDto(
            Name: fileInfo.Name,
            Extension: fileInfo.Extension,
            Path: relativePath,
            SizeInBytes: fileInfo.Length,
            UploadLink: $"https://localhost:443/file/upload/byform?parentPath={relativePath}",
            DownloadLink: $"https://localhost:443/file/download?parentPath={relativePath}"
        );
    }
}

public record DirectoryInfoResponseDto(
    string Name,
    string Path,

    ShortDirectoryInfoResponseDto[] DirectoriesNames,
    FileInfoResponseDto[] Files
)
{
    public static DirectoryInfoResponseDto FromDirectoryInfo(DirectoryInfo directoryInfo)
    {
        var relativePath = directoryInfo.FullName.Substring(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dropcore-files").Length + 37);

        return new DirectoryInfoResponseDto(
            Name: directoryInfo.Name,
            Path: relativePath,
            DirectoriesNames: directoryInfo.GetDirectories().Select(ShortDirectoryInfoResponseDto.FromDirectoryInfo).ToArray(),
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

// eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyLWlkIjoiZGU4ZjU2MjMtY2UwYS00NjgyLTk2YzctNDMyMTQ5Y2RiZTcyIiwibmJmIjoxNzQ5NTc0OTI4LCJleHAiOjE3NDk2NjEzMjgsImlzcyI6ImxvY2FsaG9zdCIsImF1ZCI6ImxvY2FsaG9zdCJ9.EFod16rOUalPLSrkoURfhhLfIYSMRgdhXPJqjqw__OI
