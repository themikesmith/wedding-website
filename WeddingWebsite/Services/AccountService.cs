using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WeddingWebsite.Core;
using WeddingWebsite.Data.Models;
using WeddingWebsite.Data.Stores;
using WeddingWebsite.Models.Accounts;

namespace WeddingWebsite.Services;

[Authorize]
public class AccountService(IStore store, ICurrentUserContext currentUserContext) : IAccountService
{
    public IEnumerable<GuestWithId> GetOwnGuests(ClaimsPrincipal user)
    {
        EnsureAuthenticated(user);
        return store.GetGuestsForUser(GetUserId(user));
    }
    
    public void Log(ClaimsPrincipal user, AccountLogType logType, string description, string? affectedUserId = null)
    {
        EnsureAuthenticated(user);
        var affectedUser = affectedUserId ?? GetUserId(user);
        store.AddAccountLog(affectedUser, GetUserId(user), logType, description);
    }
    
    public void Log(string actorUserName, AccountLogType logType, string description, string? affectedUserId = null)
    {
        currentUserContext.EnsureAuthenticated();
        var actorId = store.GetUserIdByUserName(actorUserName);
        if (actorId == null)
        {
            throw new InvalidOperationException("Could not find user ID for the provided user name.");
        }
        var affectedUser = affectedUserId ?? actorId;
        store.AddAccountLog(affectedUser, actorId, logType, description);
    }

    private string? GetUserIdOrNull(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier);
        return claim?.Value;
    }
    
    private string GetUserId(ClaimsPrincipal user)
    {
        var userId = GetUserIdOrNull(user);
        if (userId == null)
        {
            throw new InvalidOperationException("User ID claim is missing.");
        }

        return userId;
    }

    private static void EnsureAuthenticated(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Authentication is required for this operation.");
        }
    }
}