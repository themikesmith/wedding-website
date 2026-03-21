using System;
using System.Data.Common;
using System.Security.Claims;
using System.Collections.Generic;
using System.Threading.Tasks;
using WeddingWebsite.Core;
using WeddingWebsite.Data.Models;
using WeddingWebsite.Data.Stores;
using WeddingWebsite.Models;
using WeddingWebsite.Models.Accounts;
using WeddingWebsite.Models.Registry;
using WeddingWebsite.Services;
using Xunit;

namespace WeddingWebsite.PrivateMedia.Tests.Integration;

public class SecurityAServiceAuthorizationEnforcementTests
{
    [Fact]
    public void NonAdminUser_ShouldBeBlocked_FromRegistryServiceDeleteItem()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "test-user"),
                    new Claim(ClaimTypes.Role, "User")
                ],
                "TestAuth")));

        var store = new SpyRegistryStore();
        var service = new RegistryService(store, userContext);

        Assert.Throws<UnauthorizedAccessException>(() => service.DeleteItem("item-1"));
    }

    [Fact]
    public void UnauthenticatedUser_ShouldBeBlocked_FromRegistryServiceClaimItem()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(new ClaimsIdentity()));

        var store = new SpyRegistryStore();
        var service = new RegistryService(store, userContext);

        Assert.Throws<UnauthorizedAccessException>(() => service.ClaimRegistryItem("item-1", "user-1", 10m, 1));
    }

    [Fact]
    public void NonAdminUser_ShouldBeBlocked_FromAdminServiceDeleteGuest()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "test-user"),
                    new Claim(ClaimTypes.Role, "User")
                ],
                "TestAuth")));

        var store = new SpyStore();
        var service = new AdminService(store, userContext);

        Assert.Throws<UnauthorizedAccessException>(() => service.DeleteGuest("guest-1"));
    }

    private sealed class SpyRegistryStore : IRegistryStore
    {
        public void AddItem(RegistryItem item) { }
        public void UpdateItem(RegistryItem item) { }
        public bool DeleteItem(string itemId) => true;
        public RegistryItem? GetRegistryItemById(string itemId) => null;
        public Task<IEnumerable<RegistryItem>> GetAllRegistryItems(bool includeHidden = false) => Task.FromResult<IEnumerable<RegistryItem>>([]);
        public bool ClaimRegistryItem(string itemId, string userId, decimal contribution, int quantity = 1) => true;
        public bool UnclaimRegistryItem(string itemId, string userId) => true;
        public void ChoosePurchaseMethod(string itemId, string userId, string? purchaseMethodId) { }
        public void ChooseDeliveryAddress(string itemId, string userId, string? address) { }
        public void MarkClaimAsCompleted(string itemId, string userId) { }
        public void MarkClaimAsNotCompleted(string itemId, string userId) { }
        public void SetClaimNotes(string itemId, string userId, string? notes) { }
        public DbConnection CreateConnection() => throw new NotSupportedException();
        public void AddParameter(DbCommand cmd, string name, object? value) => throw new NotSupportedException();
    }

    private sealed class SpyStore : IStore
    {
        public IEnumerable<GuestWithId> GetGuestsForUser(string userId) => [];
        public void AddGuestToAccount(string userId, string firstName, string lastName, RsvpStatus rsvpStatus) { }
        public IEnumerable<AccountWithGuests> GetAllAccounts() => [];
        public IEnumerable<GuestWithId> GetGuestsForAccount(string userId) => [];
        public void RenameGuest(string guestId, string newFirstName, string newLastName) { }
        public void DeleteGuest(string guestId) { }
        public GuestWithId? GetGuestById(string guestId, string userId) => null;
        public string? GetAccountIdFromGuestId(string guestId) => null;
        public void AddAccountLog(string affectedUserId, string actorId, AccountLogType logType, string description) { }
        public string? GetUserIdByUserName(string username) => null;
        public IEnumerable<AccountLog> GetAccountLogs(string userId) => [];
        public DbConnection CreateConnection() => throw new NotSupportedException();
        public void AddParameter(DbCommand cmd, string name, object? value) => throw new NotSupportedException();
    }
}
