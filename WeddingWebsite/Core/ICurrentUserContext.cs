using System.Security.Claims;

namespace WeddingWebsite.Core;

public interface ICurrentUserContext
{
    ClaimsPrincipal Principal { get; }
    bool IsAuthenticated();
    bool IsInRole(string role);
    void EnsureAuthenticated();
    void EnsureInRole(string role);
}