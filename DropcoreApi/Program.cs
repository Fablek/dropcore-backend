using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(config =>
{
    config.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyOrigin().AllowAnyMethod();
    });

    config.DefaultPolicyName = "default";
});

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen(options =>
    {
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
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
            ValidateAudience = false,
            ValidateIssuer = false,

            ValidateLifetime = true,

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my secret for jwt token 123456 long long long long long"))
        };

        config.Validate();
    });

builder.Services
    .AddScoped<AuthService>()
    .AddScoped<AccountsService>()
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

app
    .UseCors()
    .UseAuthentication()
    .UseAuthorization();

app.MapGet("/hello", LifeCheckersEndpoints.Hello).AllowAnonymous().RequireCors();
app.MapGet("/helloauth", LifeCheckersEndpoints.HelloWithAuth).RequireAuthorization().RequireCors();

app.MapPost("/register", AccountsEndpoints.Register).AllowAnonymous().RequireCors();
app.MapPost("/login", AccountsEndpoints.Login).AllowAnonymous().RequireCors();

app.Run();
