using Microsoft.AspNetCore.Authorization;
using WeddingWebsite.Core;
using WeddingWebsite.Data.Models;
using WeddingWebsite.Data.Stores;
using WeddingWebsite.Models.Accounts;

namespace WeddingWebsite.Services;

[Authorize (Roles = "Admin")]
public class AdminService(IStore store, ICurrentUserContext currentUserContext) : IAdminService
{
    public void AddGuestToAccount(string userId, string firstName, string lastName, RsvpStatus rsvpStatus = RsvpStatus.NotResponded)
    {
        currentUserContext.EnsureInRole("Admin");
        store.AddGuestToAccount(userId, firstName, lastName, rsvpStatus);
    }

    public IEnumerable<AccountWithGuests> GetAllAccounts()
    {
        currentUserContext.EnsureInRole("Admin");
        return store.GetAllAccounts();
    }
    
    public IEnumerable<GuestWithId> GetGuestsForAccount(string userId)
    {
        currentUserContext.EnsureInRole("Admin");
        return store.GetGuestsForAccount(userId);
    }
    
    public Guest? GetGuest(string userId, string guestId)
    {
        currentUserContext.EnsureInRole("Admin");
        return store.GetGuestById(guestId, userId);
    }
    
    public void RenameGuest(string guestId, string newFirstName, string newLastName)
    {
        currentUserContext.EnsureInRole("Admin");
        store.RenameGuest(guestId, newFirstName, newLastName);
    }
    
    public void DeleteGuest(string guestId)
    {
        currentUserContext.EnsureInRole("Admin");
        store.DeleteGuest(guestId);
    }
    
    public string? GetAccountIdFromGuestId(string guestId)
    {
        currentUserContext.EnsureInRole("Admin");
        return store.GetAccountIdFromGuestId(guestId);
    }
    
    public IEnumerable<AccountLog> GetAccountLogs(string userId)
    {
        currentUserContext.EnsureInRole("Admin");
        return store.GetAccountLogs(userId);
    }
}
