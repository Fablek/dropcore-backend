using DropcoreApi.Core.Auth;
using DropcoreApi.Core.Models;
using DropcoreApi.Core.Shared;
using DropcoreApi.Core.Types;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using SharpCompress.Common;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
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

builder.Services.AddScoped(_ => new MongoClient("mongodb://localhost:27017").GetDatabase("dropcore"));

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

builder.Services
    .AddScoped<AuthService>()
    .AddScoped<AccountsService>();

builder.Services
    .AddScoped<IAccountsRepository, AccountsMongoRepository>()
    .AddSingleton<IPasswordHasher, ShaPasswordHasher>()
    .AddSingleton(new ShaPasswordHashConfig(Secret.FromUtf8String("secret"), Secret.FromUtf8String("secret")))
    .AddSingleton<IAuthTokenWriter, JwtTokenWriter>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app
    .UseAuthentication()
    .UseAuthorization();

app.MapGet("/hello", () => Results.Text($"Hello {DateTime.Now}")).AllowAnonymous();
app.MapGet("/helloauth", (ClaimsPrincipal claims) => Results.Text($"Hello {claims.Claims.Single(c => c.Type == "user-id").Value} {DateTime.Now}")).RequireAuthorization();

app.MapPost("/register", async ([FromQuery] bool setauthtoken, [FromBody] RegisterRequestDto request, HttpContext http, IAuthTokenWriter authTokenWriter, AccountsService accountsService) =>
{
    var account = await accountsService.Register(request.Username, request.Password);

    var token = authTokenWriter.GenerateAuthToken(account);

    if (setauthtoken)
        SetAuthToken(http, token);

    return Results.Ok(new
    {
        AuthToken = token
    });
}).AllowAnonymous();

app.MapPost("/login", async (HttpContext http, [FromBody] LoginRequestDto request, AuthService authService) =>
{
    var token = await authService.Authenticate(request.Username, request.Password);
    SetAuthToken(http, token);

    return Results.Ok(new
    {
        AuthToken = token
    });
}).AllowAnonymous();

static void SetAuthToken(HttpContext http, AuthToken token)
{
    http.Response.Cookies.Append("auth", token.Token, new CookieOptions
    {
        Expires = DateTimeOffset.UtcNow.AddDays(7),
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
    });
}

app.Run();

public record LoginRequestDto(
    string Username,
    string Password
);

public record RegisterRequestDto(
    string Username, 
    string Password
);

public class AccountsMongoRepository(IMongoDatabase db) : IAccountsRepository
{
    readonly IMongoCollection<AccountMongoEntity> _usersCollection = db.GetCollection<AccountMongoEntity>("users");

    public async Task Create(Account entity)
    {
        if (await GetByUniqueId(entity.UniqueId) is not null || await GetByUsername(entity.Username) is not null)
            throw new Exception($"Can not create user with username '{entity.Username.Value}' and unique id {entity.UniqueId.Guid}");

        await _usersCollection.InsertOneAsync(AccountMongoEntity.FromModel(entity));
    }

    public async Task Delete(DropcoreApi.Core.Types.UniqueId id)
    {
        await _usersCollection.DeleteOneAsync(a => a.UniqueId == id.Guid);
    }

    public async Task<Account?> GetByUniqueId(DropcoreApi.Core.Types.UniqueId id)
    {
        var account = await _usersCollection.Find(a => a.UniqueId == id.Guid).SingleOrDefaultAsync();

        return account?.ToModel();
    }

    public async Task<Account?> GetByUsername(Username username)
    {
        var account = await _usersCollection.Find(a => a.Username == username.Value).SingleOrDefaultAsync();

        return account?.ToModel();
    }

    public async Task Update(Account entity)
    {
        await _usersCollection.FindOneAndUpdateAsync(a => a.UniqueId == entity.UniqueId.Guid, Builders<AccountMongoEntity>.Update
            .Set(a => a.Username, entity.Username)
            .Set(a => a.PasswordHashBase64, entity.PasswordHash.Base64)
        );
    }

    class AccountMongoEntity {
        public ObjectId Id { get; set; }

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid UniqueId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHashBase64 { get; set; } = string.Empty;

        public static AccountMongoEntity FromModel(Account account) => new() { Id = ObjectId.Empty, UniqueId = account.UniqueId.Guid, Username = account.Username.Value, PasswordHashBase64 = account.PasswordHash.Base64 };
        public Account ToModel() => new(UniqueId, Username, Secret.FromBase64String(PasswordHashBase64));
    }
}