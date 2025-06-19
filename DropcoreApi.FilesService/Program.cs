using DropcoreApi.Core.Types;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(config =>
{
    config.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod();
    });
});

if (builder.Environment.IsDevelopment())
{
    builder.Services
        .AddEndpointsApiExplorer()
        .AddSwaggerGen(options =>
        {
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme()
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme,
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
}

builder.Services
    .AddAuthorization()
    .AddAuthentication(config =>
    {
        config.DefaultAuthenticateScheme =
        config.DefaultScheme =
        config.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, config =>
    {
        config.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateLifetime = true,

            IssuerSigningKey = new SymmetricSecurityKey(SettingsHelper.GetAuthTokenSecret().Bytes)
        };

        config.Validate();
    });

builder.Services
    .AddSingleton<UserFilesStructureService>()
    .AddSingleton(new FilesConfig(
        RootDirectory: SettingsHelper.GetRootDirectory()
    )
);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app
    .UseCors()
    .UseAuthentication()
    .UseAuthorization();

app.MapPost("/directory", DirectoriesEndpoints.CreateDirectory).DisableAntiforgery().RequireAuthorization().RequireCors();
app.MapDelete("/directory", DirectoriesEndpoints.DeleteDirectory).DisableAntiforgery().RequireAuthorization().RequireCors();
app.MapGet("/directory", DirectoriesEndpoints.GetDirectory).DisableAntiforgery().RequireAuthorization().RequireCors();

app.MapPost("/file", FilesEndpoints.CreateFile).DisableAntiforgery().RequireAuthorization().RequireCors();
app.MapDelete("/file", FilesEndpoints.DeleteFile).DisableAntiforgery().RequireAuthorization().RequireCors();
app.MapGet("/file", FilesEndpoints.GetFileInfo).DisableAntiforgery().RequireAuthorization().RequireCors();

app.MapGet("/file/download", FilesEndpoints.DownloadFile).DisableAntiforgery().RequireAuthorization().RequireCors();
app.MapPost("/file/upload/byform", FilesEndpoints.UploadFileByForm).RequireAuthorization().DisableAntiforgery().RequireCors();
app.MapPost("/file/upload", FilesEndpoints.UploadFile).RequireAuthorization().RequireCors();

app.Run();

public static class SettingsHelper
{
    public static string GetBaseUrl() => GetProperty("DP_BASE_URL");
    public static Secret GetAuthTokenSecret() => Secret.FromUtf8String(GetProperty("DP_AUTH_TOKEN_KEY"));
    public static DirectoryInfo GetRootDirectory() => new(GetProperty("DP_ROOT_DIR"));

    static string GetProperty(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
            throw new Exception($"Not set env variable '{name}'");

        return value;
    }
}