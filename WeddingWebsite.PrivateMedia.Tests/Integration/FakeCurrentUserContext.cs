using System;
using System.Security.Claims;
using WeddingWebsite.Core;

namespace WeddingWebsite.PrivateMedia.Tests.Integration;

internal sealed class FakeCurrentUserContext(ClaimsPrincipal principal) : ICurrentUserContext
{
    public ClaimsPrincipal Principal { get; } = principal;

    public bool IsAuthenticated() => Principal.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => Principal.IsInRole(role);

    public void EnsureAuthenticated()
    {
        if (!IsAuthenticated())
        {
            throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }
    }

    public void EnsureInRole(string role)
    {
        EnsureAuthenticated();
        if (!IsInRole(role))
        {
            throw new UnauthorizedAccessException($"Role '{role}' is required for this operation.");
        }
    }
}
