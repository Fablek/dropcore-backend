using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(config =>
{
    config.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod();
    });
});

builder.Services.AddSwaggerGen(options =>
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

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my secret for jwt token 123456 long long long long long"))
        };

        config.Validate();
    });

builder.Services
    .AddSingleton<UserFilesStructureService>()
    .AddSingleton(new FilesConfig(
        RootDirectory: new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "dropcore-files"))
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
