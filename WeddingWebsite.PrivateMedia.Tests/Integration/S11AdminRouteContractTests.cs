using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeddingWebsite.Components;
using Xunit;

namespace WeddingWebsite.PrivateMedia.Tests.Integration;

public class S11AdminRouteContractTests
{
    private static readonly Regex ExplicitAdminAttributeRegex = new(
        @"@attribute\s*\[\s*Authorize\s*\(\s*Roles\s*=\s*""Admin""\s*\)\s*\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    public static IEnumerable<object[]> AdminRouteCases()
    {
        yield return [new AdminRouteCase("WeddingWebsite/Components/Pages/Admin/Admin.razor", "/Admin")];
        yield return [new AdminRouteCase("WeddingWebsite/Components/Pages/Admin/ManageAccount.razor", "/Admin/Account/test-user")];
        yield return [new AdminRouteCase("WeddingWebsite/Components/Pages/Admin/ManageGuest.razor", "/Admin/Account/test-user/test-guest")];
        yield return [new AdminRouteCase("WeddingWebsite/Components/Pages/Rsvp/RsvpResponseTable.razor", "/Admin/Rsvp/Table")];
        yield return [new AdminRouteCase("WeddingWebsite/Components/Pages/Rsvp/RsvpFormAdmin.razor", "/admin/rsvp/test-user/test-guest")];
        yield return [new AdminRouteCase("WeddingWebsite/Components/Pages/Auth/Register.razor", "/Account/Register")];
        yield return [new AdminRouteCase("WeddingWebsite/Components/Pages/Admin/TodoList.razor", "/Todo")];
        yield return [new AdminRouteCase("WeddingWebsite/Components/Pages/Registry/NewRegistryItem.razor", "/Registry/New")];
        yield return [new AdminRouteCase("WeddingWebsite/Components/Pages/Registry/ManageRegistryItem.razor", "/Registry/Edit/test-item")];
    }

    [Theory]
    [MemberData(nameof(AdminRouteCases))]
    public void S11_Bullet1_RouteHasExplicitAdminAuthorizeAttribute(AdminRouteCase routeCase)
    {
        var source = File.ReadAllText(ToAbsolutePath(routeCase.FilePath));
        Assert.True(
            ExplicitAdminAttributeRegex.IsMatch(source),
            $"Missing explicit @attribute [Authorize(Roles = \"Admin\")] in {routeCase.FilePath}");
    }

    [Theory]
    [MemberData(nameof(AdminRouteCases))]
    public async Task S11_Bullet2_NonAdminAuthenticatedUser_IsBlockedByRouteBoundary(AdminRouteCase routeCase)
    {
        using var factory = CreateFactory(new AuthCaseOptions(Authenticated: true, Roles: ["User"]));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(routeCase.RoutePath);
        var blocked = response.StatusCode == HttpStatusCode.Unauthorized
                      || response.StatusCode == HttpStatusCode.Forbidden
                      || response.StatusCode == HttpStatusCode.Redirect;
        Assert.True(blocked, $"Expected route to block non-admin user: {routeCase.RoutePath}. Actual: {(int)response.StatusCode}.");

        if (response.StatusCode == HttpStatusCode.Redirect)
        {
            Assert.NotNull(response.Headers.Location);
        }
    }

    private static string ToAbsolutePath(string relativePath) =>
        Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static WebApplicationFactory<App> CreateFactory(AuthCaseOptions options)
    {
        return new WebApplicationFactory<App>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddAuthentication(auth =>
                    {
                        auth.DefaultAuthenticateScheme = RouteRoleAuthHandler.SchemeName;
                        auth.DefaultChallengeScheme = RouteRoleAuthHandler.SchemeName;
                        auth.DefaultForbidScheme = RouteRoleAuthHandler.SchemeName;
                    }).AddScheme<AuthenticationSchemeOptions, RouteRoleAuthHandler>(
                        RouteRoleAuthHandler.SchemeName,
                        _ => { });

                    services.PostConfigureAll<RouteRoleAuthHandlerOptions>(authOptions =>
                    {
                        authOptions.Authenticated = options.Authenticated;
                        authOptions.Roles = options.Roles;
                    });
                });
            });
    }

    public sealed record AdminRouteCase(string FilePath, string RoutePath);
    private sealed record AuthCaseOptions(bool Authenticated, string[] Roles);

    private sealed class RouteRoleAuthHandlerOptions
    {
        public bool Authenticated { get; set; }
        public string[] Roles { get; set; } = [];
    }

    private sealed class RouteRoleAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<RouteRoleAuthHandlerOptions> testOptions)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "S11RouteRoleAuth";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!testOptions.Value.Authenticated)
            {
                return Task.FromResult(AuthenticateResult.Fail("Unauthenticated test request."));
            }

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "s11-test-user") };
            claims.AddRange(testOptions.Value.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

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

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return Task.CompletedTask;
        }
    }
}
