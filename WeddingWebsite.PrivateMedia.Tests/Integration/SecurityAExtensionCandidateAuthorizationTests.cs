using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Security.Claims;
using WeddingWebsite.Data.Models;
using WeddingWebsite.Data.Stores;
using WeddingWebsite.Models;
using WeddingWebsite.Models.Accounts;
using WeddingWebsite.Models.Todo;
using WeddingWebsite.Services;
using Xunit;

namespace WeddingWebsite.PrivateMedia.Tests.Integration;

public class SecurityAExtensionCandidateAuthorizationTests
{
    [Fact]
    public void UnauthenticatedUser_ShouldBeBlocked_FromRsvpServiceDeleteRsvp()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(new ClaimsIdentity()));

        var store = new SpyRsvpStore();
        var service = new RsvpService(store, userContext);

        Assert.Throws<UnauthorizedAccessException>(() => service.DeleteRsvp("guest-1"));
    }

    [Fact]
    public void NonAdminUser_ShouldBeBlocked_FromRsvpServiceDeleteRsvp()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "test-user"),
                new Claim(ClaimTypes.Role, "User")
            ],
            "TestAuth")));

        var store = new SpyRsvpStore();
        var service = new RsvpService(store, userContext);

        Assert.Throws<UnauthorizedAccessException>(() => service.DeleteRsvp("guest-1"));
    }

    [Fact]
    public void AdminUser_ShouldBeAllowed_ToDeleteRsvp()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "admin-user"),
                new Claim(ClaimTypes.Role, "Admin")
            ],
            "TestAuth")));

        var store = new SpyRsvpStore();
        var service = new RsvpService(store, userContext);

        service.DeleteRsvp("guest-1");
    }

    [Fact]
    public void NonAdminUser_ShouldBeBlocked_FromTodoServiceAddNewItem()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "test-user"),
                new Claim(ClaimTypes.Role, "User")
            ],
            "TestAuth")));

        var todoStore = new SpyTodoStore();
        var store = new SpyStore();
        var service = new TodoService(todoStore, store, userContext);

        Assert.Throws<UnauthorizedAccessException>(() => service.AddNewItem());
    }

    [Fact]
    public void UnauthenticatedUser_ShouldBeBlocked_FromTodoServiceAddNewItem()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(new ClaimsIdentity()));

        var todoStore = new SpyTodoStore();
        var store = new SpyStore();
        var service = new TodoService(todoStore, store, userContext);

        Assert.Throws<UnauthorizedAccessException>(() => service.AddNewItem());
    }

    [Fact]
    public void AdminUser_ShouldBeAllowed_ToAddNewTodoItem()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "admin-user"),
                new Claim(ClaimTypes.Role, "Admin")
            ],
            "TestAuth")));

        var todoStore = new SpyTodoStore();
        var store = new SpyStore();
        var service = new TodoService(todoStore, store, userContext);

        service.AddNewItem();
    }

    [Fact]
    public void UnauthenticatedCaller_ShouldBeBlocked_FromAccountServiceLogByUserName()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(new ClaimsIdentity()));

        var store = new SpyStore();
        var service = new AccountService(store, userContext);

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.Log("actor@example.com", AccountLogType.LogIn, "test"));
    }

    [Fact]
    public void AuthenticatedUser_ShouldBeAllowed_ToLogByUserName()
    {
        var userContext = new FakeCurrentUserContext(new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "user-1"),
                new Claim(ClaimTypes.Role, "User")
            ],
            "TestAuth")));

        var store = new SpyStore();
        var service = new AccountService(store, userContext);

        service.Log("actor@example.com", AccountLogType.LogIn, "test");
    }

    private sealed class SpyRsvpStore : IRsvpStore
    {
        public bool SubmitRsvp(string guestId, bool isAttending, IReadOnlyList<string?> rsvpData) => true;
        public RsvpResponseData? GetRsvp(string guestId) => null;
        public IEnumerable<RsvpResponseData> GetAllRsvps() => [];
        public void DeleteRsvp(string guestId) { }
        public DbConnection CreateConnection() => throw new NotSupportedException();
        public void AddParameter(DbCommand cmd, string name, object? value) => throw new NotSupportedException();
    }

    private sealed class SpyTodoStore : ITodoStore
    {
        public void AddTodoItem(string id) { }
        public void RenameTodoItem(string id, string newText) { }
        public void SetTodoItemOwner(string id, string? ownerId) { }
        public void SetTodoItemGroup(string id, string? groupId) { }
        public void SetTodoItemWaitingUntil(string id, DateTime? waitingUntil) { }
        public void SetTodoItemCompletedAt(string id, DateTime? completedAt) { }
        public TodoItem? GetTodoItem(string id) => null;
        public IList<TodoItem> GetAllTodoItems() => [];
        public void DeleteTodoItem(string id) { }
        public void AddTodoGroup(string id, string name) { }
        public void RenameTodoGroup(string id, string newName) { }
        public void RemoveTodoGroup(string id) { }
        public TodoGroup? GetTodoGroup(string id) => null;
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
        public string? GetUserIdByUserName(string username) => "actor-id";
        public IEnumerable<AccountLog> GetAccountLogs(string userId) => [];
        public DbConnection CreateConnection() => throw new NotSupportedException();
        public void AddParameter(DbCommand cmd, string name, object? value) => throw new NotSupportedException();
    }
}
