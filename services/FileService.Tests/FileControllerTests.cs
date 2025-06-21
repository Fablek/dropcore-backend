using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using FileService.Controllers;
using FileService.Data;
using FileService.DTOs;
using FileService.Models;

public class FilesControllerTests
{
    private FileDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<FileDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FileDbContext(options);
    }

    private ClaimsPrincipal GetMockUser(string email = "test@example.com", string id = "user-id-123")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.NameIdentifier, id)
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private IFormFile GetMockFile(string fileName = "test.txt", string content = "hello world")
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };
    }

    private HttpClient GetMockHttpClient(Func<HttpRequestMessage, HttpResponseMessage> responseFunc)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) => responseFunc(request));

        return new HttpClient(mockHandler.Object)
        {
            BaseAddress = new Uri("http://fake/")
        };
    }

    private FilesController CreateController(HttpClient httpClient, ClaimsPrincipal user)
    {
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new FilesController(factoryMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }

    [Fact]
    public async Task Upload_Should_Save_Metadata_When_User_Has_Space()
    {
        var db = GetInMemoryDb();
        var file = GetMockFile();
        var dto = new FileUploadDto { File = file };
        var user = GetMockUser();

        var httpClient = GetMockHttpClient(req =>
        {
            var url = req.RequestUri!.ToString();
            if (url.Contains("/users/") && req.Method == HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new UserDto { Email = "test@example.com", UsedSpace = 0, SpaceLimit = 100000 })
                };
            if (url.Contains("storage-node") && req.Method == HttpMethod.Post)
                return new HttpResponseMessage(HttpStatusCode.OK);
            if (url.Contains("users/increase"))
                return new HttpResponseMessage(HttpStatusCode.OK);
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var controller = CreateController(httpClient, user);
        var result = await controller.Upload(dto, db);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var metadata = Assert.IsType<FileMetadata>(okResult.Value);
        Assert.Equal("test.txt", metadata.FileName);
    }

    [Fact]
    public async Task Upload_Should_Reject_When_User_Exceeds_Space()
    {
        var db = GetInMemoryDb();
        var file = GetMockFile();
        var dto = new FileUploadDto { File = file };
        var user = GetMockUser();

        var httpClient = GetMockHttpClient(req =>
        {
            if (req.RequestUri!.ToString().Contains("/users/") && req.Method == HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new UserDto { Email = "test@example.com", UsedSpace = 999999, SpaceLimit = 1000000 })
                };
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var controller = CreateController(httpClient, user);
        var result = await controller.Upload(dto, db);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Not enough available space.", badRequest.Value);
    }

    [Fact]
    public async Task Upload_Should_Return_500_When_UserService_Fails()
    {
        var db = GetInMemoryDb();
        var file = GetMockFile();
        var dto = new FileUploadDto { File = file };
        var user = GetMockUser();

        var httpClient = GetMockHttpClient(req =>
        {
            if (req.RequestUri!.ToString().Contains("/users/") && req.Method == HttpMethod.Get)
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var controller = CreateController(httpClient, user);
        var result = await controller.Upload(dto, db);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, status.StatusCode);
        Assert.Equal("UserService validation failed", status.Value);
    }
}