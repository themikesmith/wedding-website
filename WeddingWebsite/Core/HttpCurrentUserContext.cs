using System.Security.Claims;
using System.Threading;

namespace WeddingWebsite.Core;

public class HttpCurrentUserContext(IHttpContextAccessor httpContextAccessor) : ICurrentUserContext
{
    public ClaimsPrincipal Principal
    {
        get
        {
            var httpPrincipal = httpContextAccessor.HttpContext?.User;
            if (httpPrincipal?.Identity?.IsAuthenticated == true)
            {
                return httpPrincipal;
            }

            if (Thread.CurrentPrincipal is ClaimsPrincipal threadPrincipal &&
                threadPrincipal.Identity?.IsAuthenticated == true)
            {
                return threadPrincipal;
            }

            if (httpPrincipal != null)
            {
                return httpPrincipal;
            }

            return Thread.CurrentPrincipal as ClaimsPrincipal ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    public bool IsAuthenticated() => Principal.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => Principal.IsInRole(role);

    public void EnsureAuthenticated()
    {
        if (!IsAuthenticated())
        {
            throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }

        var userId = Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException("A valid user identifier claim is required for this operation.");
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