using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

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

builder.Services.AddScoped(_ => new MongoClient(SettingsHelper.GetMongoDbConnectionString()).GetDatabase(SettingsHelper.GetMongoDbName()));

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

            IssuerSigningKey = new SymmetricSecurityKey(SettingsHelper.GetAuthTokenKey().Bytes)
        };

        config.Validate();
    });

builder.Services
    .AddScoped<AuthService>()
    .AddScoped<AccountsService>()
    .AddScoped<IAccountsRepository, AccountsMongoRepository>()
    .AddSingleton<IPasswordHasher, ShaPasswordHasher>()
    .AddSingleton(new ShaPasswordHashConfig(SettingsHelper.GetPasswordHashSalt(), SettingsHelper.GetPasswordHashPeper()))
    .AddSingleton<IAuthTokenWriter, JwtTokenWriter>();

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

app.MapGet("/hello", LifeCheckersEndpoints.Hello).AllowAnonymous().RequireCors();
app.MapGet("/helloauth", LifeCheckersEndpoints.HelloWithAuth).RequireAuthorization().RequireCors();

app.MapPost("/register", AccountsEndpoints.Register).AllowAnonymous().RequireCors();
app.MapPost("/login", AccountsEndpoints.Login).AllowAnonymous().RequireCors();

app.Run();

public static class SettingsHelper
{
    public static Secret GetPasswordHashSalt() => Secret.FromUtf8String(GetProperty("DP_PWD_HASH_SALT"));
    public static Secret GetPasswordHashPeper() => Secret.FromUtf8String(GetProperty("DP_PWD_HASH_PEPER"));
    public static Secret GetAuthTokenKey() => Secret.FromUtf8String(GetProperty("DP_AUTH_TOKEN_KEY"));
    public static string GetMongoDbConnectionString() => GetProperty("DP_MONGODB_CONNECTION_STRING");
    public static string GetMongoDbName() => GetProperty("DP_MONGODB_DB_NAME");

    static string GetProperty(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
            throw new Exception($"Not set env variable '{name}'");

        return value;
    }
}