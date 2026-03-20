using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeddingWebsite.Components;
using Xunit;

namespace WeddingWebsite.PrivateMedia.Tests.Integration;

public class PrivateMediaEndpointTests : IAsyncLifetime
{
    private readonly List<string> _tempDirectories = [];
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"wedding-private-media-tests-{Guid.NewGuid():N}");
    private readonly string _validRelativePath = "albums/couple.jpg";
    private readonly byte[] _validBytes = [1, 2, 3, 4, 5, 6, 7, 8];
    private readonly byte[] _publicBackgroundBytes = [9, 10, 11, 12];

    public Task InitializeAsync()
    {
        _tempDirectories.Add(_tempRoot);
        Directory.CreateDirectory(Path.Combine(_tempRoot, "albums"));
        File.WriteAllBytes(Path.Combine(_tempRoot, _validRelativePath), _validBytes);
        File.WriteAllText(Path.Combine(_tempRoot, "albums", "notes.txt"), "not allowed");
        File.WriteAllBytes(Path.Combine(_tempRoot, "albums", "large.jpg"), Enumerable.Range(0, 32).Select(i => (byte)i).ToArray());
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        foreach (var directory in _tempDirectories.Where(Directory.Exists))
        {
            Directory.Delete(directory, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Photos_Unauthenticated_Request_UsesChallengeSemantics()
    {
        using var factory = CreateFactory(new TestFactoryOptions(Authenticated: false));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"/Photos/{_validRelativePath}");

        Assert.True(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Redirect);

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            Assert.True(response.Headers.Location is not null);
            Assert.Contains("/Account/Login", response.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Photos_Authenticated_ValidFile_ReturnsFile_WithSecurityHeaders()
    {
        using var factory = CreateFactory(new TestFactoryOptions(Authenticated: true));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync($"/Photos/{_validRelativePath}");
        var body = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Cache-Control", out var cacheValues));
        var cacheControl = string.Join(",", cacheValues!);
        Assert.Contains("private", cacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", cacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(_validBytes, body);
    }

    [Fact]
    public async Task Photos_Authenticated_DisallowedExtension_ReturnsNotFound()
    {
        using var factory = CreateFactory(new TestFactoryOptions(Authenticated: true));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Photos/albums/notes.txt");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Photos_Authenticated_PathTraversal_ReturnsNotFound()
    {
        using var factory = CreateFactory(new TestFactoryOptions(Authenticated: true));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Photos/../appsettings.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Photos_Authenticated_FileExceedsConfiguredMaxBytes_ReturnsNotFound()
    {
        using var factory = CreateFactory(new TestFactoryOptions(Authenticated: true, MaxBytes: 16));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Photos/albums/large.jpg");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Photos_Authenticated_MissingAllowedFile_ReturnsNotFound()
    {
        using var factory = CreateFactory(new TestFactoryOptions(Authenticated: true));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Photos/albums/does-not-exist.jpg");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Photos_Authenticated_EncodedOutOfRootEscape_ReturnsNotFound()
    {
        using var factory = CreateFactory(new TestFactoryOptions(Authenticated: true));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Photos/%2e%2e/%2e%2e/etc/passwd");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublicBackground_IsAccessible_WithoutAuthentication()
    {
        using var factory = CreateFactory(new TestFactoryOptions(Authenticated: false));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/bg/home-hero.jpg");
        var body = await response.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(_publicBackgroundBytes, body);
    }

    [Fact]
    public void Startup_Fails_When_PublicWwwrootPhotos_ContainsFiles()
    {
        var webRootWithPublicPhotos = CreateWebRoot(includePublicPhotosLeak: true);
        using var factory = CreateFactory(new TestFactoryOptions(Authenticated: true, WebRootPath: webRootWithPublicPhotos));

        var exception = Record.Exception(() =>
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }));

        Assert.NotNull(exception);
    }

    private WebApplicationFactory<App> CreateFactory(TestFactoryOptions options)
    {
        var webRoot = options.WebRootPath ?? CreateWebRoot();

        return new WebApplicationFactory<App>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.UseSetting(WebHostDefaults.WebRootKey, webRoot);
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    var overrideConfig = new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = "DataSource=Data/test-private-media.db;Cache=Shared",
                        ["PrivateMedia:RootPath"] = _tempRoot,
                        ["PrivateMedia:AllowedExtensions:0"] = ".jpg",
                        ["PrivateMedia:AllowedExtensions:1"] = ".jpeg",
                        ["PrivateMedia:AllowedExtensions:2"] = ".png",
                        ["PrivateMedia:AllowedExtensions:3"] = ".webp"
                    };

                    if (options.MaxBytes.HasValue)
                    {
                        overrideConfig["PrivateMedia:MaxFileSizeBytes"] = options.MaxBytes.Value.ToString();
                    }

                    configBuilder.AddInMemoryCollection(overrideConfig);
                });

                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName,
                        options => { });

                    services.PostConfigureAll<TestAuthHandlerOptions>(authOptions =>
                    {
                        authOptions.Authenticated = options.Authenticated;
                    });
                });
            });
    }

    private string CreateWebRoot(bool includePublicPhotosLeak = false)
    {
        var webRoot = Path.Combine(Path.GetTempPath(), $"wedding-webroot-tests-{Guid.NewGuid():N}");
        _tempDirectories.Add(webRoot);

        Directory.CreateDirectory(Path.Combine(webRoot, "bg"));
        File.WriteAllBytes(Path.Combine(webRoot, "bg", "home-hero.jpg"), _publicBackgroundBytes);

        if (includePublicPhotosLeak)
        {
            Directory.CreateDirectory(Path.Combine(webRoot, "Photos"));
            File.WriteAllBytes(Path.Combine(webRoot, "Photos", "leak.jpg"), [1, 2, 3]);
        }

        return webRoot;
    }

    private sealed record TestFactoryOptions(bool Authenticated, int? MaxBytes = null, string? WebRootPath = null);

    private sealed class TestAuthHandlerOptions
    {
        public bool Authenticated { get; set; }
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<TestAuthHandlerOptions> testOptions)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "TestAuth";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!testOptions.Value.Authenticated)
            {
                return Task.FromResult(AuthenticateResult.Fail("Unauthenticated test request."));
            }

            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "test-user-id") };
            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return Task.CompletedTask;
        }
    }
}
