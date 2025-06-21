using Xunit;
using AuthService.Controllers;
using AuthService.Data;
using AuthService.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public class AuthControllerTests
{
    private class TestableAuthController : AuthController
    {
        public TestableAuthController(AuthDbContext context, IConfiguration config, HttpClient client)
            : base(context, config)
        {
            var field = typeof(AuthController).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(this, client);
        }
    }

    private AuthDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AuthDbContext(options);
    }

    private IConfiguration GetFakeConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "Jwt:Key", "supersecretkey1234567890" },
            { "Jwt:Issuer", "testissuer" },
            { "Jwt:Audience", "testaudience" }
        }).Build();

    [Fact]
    public async Task Register_Should_Create_User()
    {
        var context = GetInMemoryDb();
        var config = GetFakeConfig();

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\":1}")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://user-service:5000/")
        };

        var controller = new TestableAuthController(context, config, httpClient);

        var result = await controller.Register(new RegisterDto
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "password"
        });

        Assert.IsType<OkObjectResult>(result);
    }
}
