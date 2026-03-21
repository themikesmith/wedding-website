using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using WeddingWebsite.Core;
using WeddingWebsite.Data.Models;
using WeddingWebsite.Data.Stores;
using WeddingWebsite.Models;
using WeddingWebsite.Models.Accounts;
using WeddingWebsite.Models.Registry;
using WeddingWebsite.Models.Rsvp;
using WeddingWebsite.Models.Todo;
using WeddingWebsite.Services;
using Xunit;

namespace WeddingWebsite.PrivateMedia.Tests.Integration;

public class SecurityAAuthorizationMatrixHarnessTests
{
    private enum AuthorizationPolicy
    {
        Authenticated,
        Admin
    }

    [Fact]
    public void AllPublicServiceEntryPoints_ShouldBeCoveredByAuthorizationMatrix()
    {
        var expected = AuthorizationCases().Select(c => c.MethodKey).ToHashSet();

        var discovered = TargetServiceTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(method => !method.IsSpecialName)
            .Select(GetMethodKey)
            .ToHashSet();

        var missing = discovered.Except(expected).OrderBy(x => x).ToList();
        Assert.True(missing.Count == 0,
            $"Authorization matrix is missing service methods:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", missing)}");
    }

    [Fact]
    public void ServiceAuthorizationMatrix_ShouldEnforceExpectedPoliciesAcrossRoles()
    {
        foreach (var authorizationCase in AuthorizationCases())
        {
            AssertAccess(authorizationCase, BuildAnonymousPrincipal(), shouldAllow: false);
            AssertAccess(
                authorizationCase,
                BuildAuthenticatedPrincipal("user-1", "User"),
                shouldAllow: authorizationCase.Policy == AuthorizationPolicy.Authenticated);
            AssertAccess(
                authorizationCase,
                BuildAuthenticatedPrincipal("admin-1", "Admin"),
                shouldAllow: true);
            AssertAccess(
                authorizationCase,
                BuildAuthenticatedPrincipal("owner-1", "Owner"),
                shouldAllow: authorizationCase.Policy == AuthorizationPolicy.Authenticated);
        }
    }

    [Fact]
    public void AccountServiceMethodsUsingClaimsPrincipal_ShouldThrow_WhenNameIdentifierMissing()
    {
        var principalMissingNameIdentifier = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Role, "User")], "TestAuth"));

        var service = new AccountService(new NoOpStore(), new FakeCurrentUserContext(principalMissingNameIdentifier));

        Assert.Throws<InvalidOperationException>(() => service.GetOwnGuests(principalMissingNameIdentifier));
        Assert.Throws<InvalidOperationException>(() => service.Log(
            principalMissingNameIdentifier,
            AccountLogType.LogIn,
            "missing name id"));
    }

    [Fact]
    public void AuthenticatedPrincipalWithoutNameIdentifier_ShouldBeBlocked_ByCurrentUserContextChecks()
    {
        var principalWithoutNameId = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "TestAuth"));

        var registryService = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principalWithoutNameId));
        var todoService = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principalWithoutNameId));
        var accountService = new AccountService(new NoOpStore(), new FakeCurrentUserContext(principalWithoutNameId));

        Assert.Throws<UnauthorizedAccessException>(() => registryService.GetRegistryItemById("item-1"));
        Assert.Throws<UnauthorizedAccessException>(() => todoService.GetGroupedTodoItems());
        Assert.Throws<UnauthorizedAccessException>(() => accountService.Log("actor@example.com", AccountLogType.LogIn, "test", null));
    }

    [Fact]
    public void AdminPrincipalWithoutNameIdentifier_ShouldBeBlocked_ByAdminRoleChecks()
    {
        var adminPrincipalWithoutNameId = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Role, "Admin")], "TestAuth"));

        var adminService = new AdminService(new NoOpStore(), new FakeCurrentUserContext(adminPrincipalWithoutNameId));
        var registryService = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(adminPrincipalWithoutNameId));
        var rsvpService = new RsvpService(new NoOpRsvpStore(), new FakeCurrentUserContext(adminPrincipalWithoutNameId));
        var todoService = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(adminPrincipalWithoutNameId));

        Assert.Throws<UnauthorizedAccessException>(() => adminService.GetAllAccounts());
        Assert.Throws<UnauthorizedAccessException>(() => registryService.DeleteItem("item-1"));
        Assert.Throws<UnauthorizedAccessException>(() => rsvpService.GetAllRsvps(true, new RsvpQuestions([])).ToList());
        Assert.Throws<UnauthorizedAccessException>(() => todoService.MarkItemAsCompleted("todo-1"));
    }

    [Fact]
    public void AuthenticatedPrincipalWithWhitespaceNameIdentifier_ShouldBeBlocked_ByCurrentUserContextChecks()
    {
        var whitespaceNameIdPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "   "),
                new Claim(ClaimTypes.Role, "User")
            ],
            "TestAuth"));

        var registryService = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(whitespaceNameIdPrincipal));
        var todoService = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(whitespaceNameIdPrincipal));

        Assert.Throws<UnauthorizedAccessException>(() => registryService.GetRegistryItemById("item-1"));
        Assert.Throws<UnauthorizedAccessException>(() => todoService.GetGroupedTodoItems());
    }

    private static void AssertAccess(AuthorizationCase authorizationCase, ClaimsPrincipal principal, bool shouldAllow)
    {
        if (shouldAllow)
        {
            authorizationCase.Invoke(principal);
            return;
        }

        Assert.Throws<UnauthorizedAccessException>(() => authorizationCase.Invoke(principal));
    }

    private static ClaimsPrincipal BuildAnonymousPrincipal() => new(new ClaimsIdentity());

    private static ClaimsPrincipal BuildAuthenticatedPrincipal(string userId, string role) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            ],
            "TestAuth"));

    private static IEnumerable<Type> TargetServiceTypes() =>
    [
        typeof(AdminService),
        typeof(RegistryService),
        typeof(RsvpService),
        typeof(TodoService),
        typeof(AccountService)
    ];

    private static List<AuthorizationCase> AuthorizationCases() =>
    [
        Case<AdminService>(nameof(AdminService.AddGuestToAccount), AuthorizationPolicy.Admin, [typeof(string), typeof(string), typeof(string), typeof(RsvpStatus)], principal =>
        {
            var service = new AdminService(new NoOpStore(), new FakeCurrentUserContext(principal));
            service.AddGuestToAccount("user-1", "First", "Last", RsvpStatus.NotResponded);
        }),
        Case<AdminService>(nameof(AdminService.GetAllAccounts), AuthorizationPolicy.Admin, [], principal =>
        {
            var service = new AdminService(new NoOpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetAllAccounts();
        }),
        Case<AdminService>(nameof(AdminService.GetGuestsForAccount), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new AdminService(new NoOpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetGuestsForAccount("user-1");
        }),
        Case<AdminService>(nameof(AdminService.GetGuest), AuthorizationPolicy.Admin, [typeof(string), typeof(string)], principal =>
        {
            var service = new AdminService(new NoOpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetGuest("user-1", "guest-1");
        }),
        Case<AdminService>(nameof(AdminService.RenameGuest), AuthorizationPolicy.Admin, [typeof(string), typeof(string), typeof(string)], principal =>
        {
            var service = new AdminService(new NoOpStore(), new FakeCurrentUserContext(principal));
            service.RenameGuest("guest-1", "New", "Name");
        }),
        Case<AdminService>(nameof(AdminService.DeleteGuest), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new AdminService(new NoOpStore(), new FakeCurrentUserContext(principal));
            service.DeleteGuest("guest-1");
        }),
        Case<AdminService>(nameof(AdminService.GetAccountIdFromGuestId), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new AdminService(new NoOpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetAccountIdFromGuestId("guest-1");
        }),
        Case<AdminService>(nameof(AdminService.GetAccountLogs), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new AdminService(new NoOpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetAccountLogs("user-1");
        }),

        Case<RegistryService>(nameof(RegistryService.AddItem), AuthorizationPolicy.Admin, [typeof(RegistryItem)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            service.AddItem(NewRegistryItem("item-1"));
        }),
        Case<RegistryService>(nameof(RegistryService.UpdateItem), AuthorizationPolicy.Admin, [typeof(RegistryItem)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            service.UpdateItem(NewRegistryItem("item-1"));
        }),
        Case<RegistryService>(nameof(RegistryService.DeleteItem), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            _ = service.DeleteItem("item-1");
        }),
        Case<RegistryService>(nameof(RegistryService.GetRegistryItemById), AuthorizationPolicy.Authenticated, [typeof(string)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            _ = service.GetRegistryItemById("item-1");
        }),
        Case<RegistryService>(nameof(RegistryService.GetAllRegistryItems), AuthorizationPolicy.Authenticated, [typeof(bool)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            _ = service.GetAllRegistryItems(includeHidden: false).GetAwaiter().GetResult();
        }),
        Case<RegistryService>(nameof(RegistryService.ClaimRegistryItem), AuthorizationPolicy.Authenticated, [typeof(string), typeof(string), typeof(decimal), typeof(int)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            _ = service.ClaimRegistryItem("item-1", "user-1", 10m, 1);
        }),
        Case<RegistryService>(nameof(RegistryService.UnclaimRegistryItem), AuthorizationPolicy.Authenticated, [typeof(string), typeof(string)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            _ = service.UnclaimRegistryItem("item-1", "user-1");
        }),
        Case<RegistryService>(nameof(RegistryService.ChoosePurchaseMethod), AuthorizationPolicy.Authenticated, [typeof(string), typeof(string), typeof(string)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            service.ChoosePurchaseMethod("item-1", "user-1", "pm-1");
        }),
        Case<RegistryService>(nameof(RegistryService.ChooseDeliveryAddress), AuthorizationPolicy.Authenticated, [typeof(string), typeof(string), typeof(string)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            service.ChooseDeliveryAddress("item-1", "user-1", "Address");
        }),
        Case<RegistryService>(nameof(RegistryService.MarkClaimAsCompleted), AuthorizationPolicy.Authenticated, [typeof(string), typeof(string)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            service.MarkClaimAsCompleted("item-1", "user-1");
        }),
        Case<RegistryService>(nameof(RegistryService.MarkClaimAsNotCompleted), AuthorizationPolicy.Admin, [typeof(string), typeof(string)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            service.MarkClaimAsNotCompleted("item-1", "user-1");
        }),
        Case<RegistryService>(nameof(RegistryService.SetClaimNotes), AuthorizationPolicy.Authenticated, [typeof(string), typeof(string), typeof(string)], principal =>
        {
            var service = new RegistryService(new NoOpRegistryStore(), new FakeCurrentUserContext(principal));
            service.SetClaimNotes("item-1", "user-1", "notes");
        }),

        Case<RsvpService>(nameof(RsvpService.SubmitRsvp), AuthorizationPolicy.Authenticated, [typeof(string), typeof(bool), typeof(IReadOnlyList<string?>)], principal =>
        {
            var service = new RsvpService(new NoOpRsvpStore(), new FakeCurrentUserContext(principal));
            _ = service.SubmitRsvp("guest-1", true, ["yes"]);
        }),
        Case<RsvpService>(nameof(RsvpService.GetAllRsvps), AuthorizationPolicy.Admin, [typeof(bool), typeof(RsvpQuestions)], principal =>
        {
            var service = new RsvpService(new NoOpRsvpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetAllRsvps(true, new RsvpQuestions([]));
        }),
        Case<RsvpService>(nameof(RsvpService.GetRsvp), AuthorizationPolicy.Admin, [typeof(string), typeof(RsvpQuestions), typeof(RsvpQuestions)], principal =>
        {
            var service = new RsvpService(new NoOpRsvpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetRsvp("guest-1", new RsvpQuestions([]), new RsvpQuestions([]));
        }),
        Case<RsvpService>(nameof(RsvpService.GetRsvpBasic), AuthorizationPolicy.Authenticated, [typeof(string)], principal =>
        {
            var service = new RsvpService(new NoOpRsvpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetRsvpBasic("guest-1");
        }),
        Case<RsvpService>(nameof(RsvpService.DeleteRsvp), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new RsvpService(new NoOpRsvpStore(), new FakeCurrentUserContext(principal));
            service.DeleteRsvp("guest-1");
        }),
        Case<RsvpService>(nameof(RsvpService.EditRsvp), AuthorizationPolicy.Authenticated, [typeof(string), typeof(bool), typeof(IReadOnlyList<string?>)], principal =>
        {
            var service = new RsvpService(new NoOpRsvpStore(), new FakeCurrentUserContext(principal));
            _ = service.EditRsvp("guest-1", false, ["no"]);
        }),

        Case<TodoService>(nameof(TodoService.GetGroupedTodoItems), AuthorizationPolicy.Authenticated, [], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetGroupedTodoItems();
        }),
        Case<TodoService>(nameof(TodoService.MarkItemAsCompleted), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            service.MarkItemAsCompleted("todo-1");
        }),
        Case<TodoService>(nameof(TodoService.MarkItemAsWaiting), AuthorizationPolicy.Admin, [typeof(string), typeof(TimeSpan)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            service.MarkItemAsWaiting("todo-1", TimeSpan.FromHours(1));
        }),
        Case<TodoService>(nameof(TodoService.MarkItemAsActionRequired), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            service.MarkItemAsActionRequired("todo-1");
        }),
        Case<TodoService>(nameof(TodoService.GetTodoItem), AuthorizationPolicy.Authenticated, [typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetTodoItem("todo-1");
        }),
        Case<TodoService>(nameof(TodoService.AddNewItem), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            service.AddNewItem("group-1");
        }),
        Case<TodoService>(nameof(TodoService.RenameItem), AuthorizationPolicy.Admin, [typeof(string), typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            service.RenameItem("todo-1", "new text");
        }),
        Case<TodoService>(nameof(TodoService.GroupItem), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            service.GroupItem("todo-1");
        }),
        Case<TodoService>(nameof(TodoService.RemoveGroupFromItem), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            service.RemoveGroupFromItem("todo-1");
        }),
        Case<TodoService>(nameof(TodoService.RenameGroup), AuthorizationPolicy.Admin, [typeof(string), typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            service.RenameGroup("group-1", "new name");
        }),
        Case<TodoService>(nameof(TodoService.DeleteItem), AuthorizationPolicy.Admin, [typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            service.DeleteItem("todo-1");
        }),
        Case<TodoService>(nameof(TodoService.SetItemOwnerByUserName), AuthorizationPolicy.Admin, [typeof(string), typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            service.SetItemOwnerByUserName("todo-1", "user@example.com");
        }),
        Case<TodoService>(nameof(TodoService.GetTodoItemsRequiringActionForGivenUserNameOrNoUserName), AuthorizationPolicy.Authenticated, [typeof(string)], principal =>
        {
            var service = new TodoService(new NoOpTodoStore(), new NoOpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetTodoItemsRequiringActionForGivenUserNameOrNoUserName("user@example.com");
        }),

        Case<AccountService>(nameof(AccountService.GetOwnGuests), AuthorizationPolicy.Authenticated, [typeof(ClaimsPrincipal)], principal =>
        {
            var service = new AccountService(new NoOpStore(), new FakeCurrentUserContext(principal));
            _ = service.GetOwnGuests(principal);
        }),
        Case<AccountService>(nameof(AccountService.Log), AuthorizationPolicy.Authenticated, [typeof(ClaimsPrincipal), typeof(AccountLogType), typeof(string), typeof(string)], principal =>
        {
            var service = new AccountService(new NoOpStore(), new FakeCurrentUserContext(principal));
            service.Log(principal, AccountLogType.LogIn, "test", null);
        }),
        Case<AccountService>(nameof(AccountService.Log), AuthorizationPolicy.Authenticated, [typeof(string), typeof(AccountLogType), typeof(string), typeof(string)], principal =>
        {
            var service = new AccountService(new NoOpStore(), new FakeCurrentUserContext(principal));
            service.Log("actor@example.com", AccountLogType.LogIn, "test", null);
        })
    ];

    private static AuthorizationCase Case<TService>(
        string methodName,
        AuthorizationPolicy policy,
        Type[] parameters,
        Action<ClaimsPrincipal> invoke)
    {
        var method = typeof(TService).GetMethod(methodName, parameters);
        if (method == null)
        {
            throw new InvalidOperationException($"Could not find method {typeof(TService).Name}.{methodName}({string.Join(",", parameters.Select(p => p.Name))})");
        }

        return new AuthorizationCase(GetMethodKey(method), policy, invoke);
    }

    private static string GetMethodKey(MethodInfo method)
    {
        var paramTypes = string.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName));
        return $"{method.DeclaringType?.FullName}.{method.Name}({paramTypes})";
    }

    private static RegistryItem NewRegistryItem(string id)
    {
        return new RegistryItem(
            id,
            "gift",
            "Gift",
            null,
            null,
            [],
            [],
            maxQuantity: 1,
            priority: 0,
            hide: false,
            allowsPartialContributions: false,
            isDonation: false);
    }

    private sealed record AuthorizationCase(string MethodKey, AuthorizationPolicy Policy, Action<ClaimsPrincipal> Invoke);

    private sealed class NoOpRegistryStore : IRegistryStore
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

    private sealed class NoOpRsvpStore : IRsvpStore
    {
        public bool SubmitRsvp(string guestId, bool isAttending, IReadOnlyList<string?> rsvpData) => true;
        public RsvpResponseData? GetRsvp(string guestId) => null;
        public IEnumerable<RsvpResponseData> GetAllRsvps() => [];
        public void DeleteRsvp(string guestId) { }
        public DbConnection CreateConnection() => throw new NotSupportedException();
        public void AddParameter(DbCommand cmd, string name, object? value) => throw new NotSupportedException();
    }

    private sealed class NoOpTodoStore : ITodoStore
    {
        public void AddTodoItem(string id) { }
        public void RenameTodoItem(string id, string newText) { }
        public void SetTodoItemOwner(string id, string? ownerId) { }
        public void SetTodoItemGroup(string id, string? groupId) { }
        public void SetTodoItemWaitingUntil(string id, DateTime? waitingUntil) { }
        public void SetTodoItemCompletedAt(string id, DateTime? completedAt) { }
        public TodoItem? GetTodoItem(string id) => new(id, "user@example.com", "todo", new TodoGroup("group-1", "Group"));
        public IList<TodoItem> GetAllTodoItems() =>
        [
            new("todo-1", "user@example.com", "todo", new TodoGroup("group-1", "Group")),
            new("todo-2", null, "todo", null)
        ];
        public void DeleteTodoItem(string id) { }
        public void AddTodoGroup(string id, string name) { }
        public void RenameTodoGroup(string id, string newName) { }
        public void RemoveTodoGroup(string id) { }
        public TodoGroup? GetTodoGroup(string id) => new(id, "Group");
        public DbConnection CreateConnection() => throw new NotSupportedException();
        public void AddParameter(DbCommand cmd, string name, object? value) => throw new NotSupportedException();
    }

    private sealed class NoOpStore : IStore
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
