using FileService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);

// Add services

builder.Services.AddControllers()
    .AddNewtonsoftJson(); // Je�li planujesz u�ywa� JSON z atrybutami Newtonsoft

builder.Services.AddDbContext<FileDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FileService", Version = "v1" });

    c.OperationFilter<FileUploadOperationFilter>();
});

builder.Services.AddHttpClient();

var app = builder.Build();

// Middleware

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FileService V1");
});

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
