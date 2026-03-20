using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace WeddingWebsite.PrivateMedia.Tests.Integration;

public class S31ProceduralAuthorizationContractTests
{
    private static readonly string[] AuthorizationMarkers =
    [
        "AuthorizationService.AuthorizeAsync(",
        "AuthorizeAsync(",
        ".IsInRole(",
        "User.IsInRole(",
        "[Authorize"
    ];

    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));

    public static IEnumerable<object[]> MutationCases()
    {
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/ManageAccount.razor", "ApplyEmailChange", ["UserManager.UpdateAsync("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/ManageAccount.razor", "ApplyUserNameChange", ["UserManager.UpdateAsync("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/ManageAccount.razor", "ApplyPasswordChange", ["UserManager.ResetPasswordAsync("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/ManageAccount.razor", "AddNewGuest", ["AdminService.AddGuestToAccount("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/ManageAccount.razor", "DemoteUser", ["UserManager.RemoveFromRoleAsync("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/ManageAccount.razor", "PromoteUser", ["UserManager.AddToRoleAsync("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/ManageAccount.razor", "DeleteAccount", ["UserManager.DeleteAsync("] )];

        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/ManageGuest.razor", "ApplyNameChange", ["AdminService.RenameGuest("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/ManageGuest.razor", "DeleteGuest", ["AdminService.DeleteGuest("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/ManageGuest.razor", "DeleteRsvp", ["RsvpService.DeleteRsvp("] )];

        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/NewRegistryItem.razor", "OnSubmit", ["RegistryService.AddItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/ManageRegistryItem.razor", "OnSubmit", ["RegistryService.UpdateItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/ManageRegistryItem.razor", "DeleteItem", ["RegistryService.DeleteItem("] )];

        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Admin/TodoList.razor", "AddNewItem", ["TodoService.AddNewItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/WeddingComponents/TodoListGroup.razor", "Ungroup", ["TodoService.RemoveGroupFromItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/WeddingComponents/TodoListGroup.razor", "AddNewItem", ["TodoService.AddNewItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/WeddingComponents/TodoListGroup.razor", "SaveEdits", ["TodoService.RenameGroup("] )];

        yield return [new MutationMethodCase("WeddingWebsite/Components/WeddingComponents/TodoListItem.razor", "SaveEdits", ["ApplyToItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/WeddingComponents/TodoListItem.razor", "CancelEdits", ["ApplyToItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/WeddingComponents/TodoListItem.razor", "MarkItemAsCompleted", ["ApplyToItemWithUndoAction("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/WeddingComponents/TodoListItem.razor", "MarkItemAsWaiting", ["ApplyToItemWithUndoAction("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/WeddingComponents/TodoListItem.razor", "MarkItemAsActionRequired", ["ApplyToItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/WeddingComponents/TodoListItem.razor", "Group", ["ApplyToItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/WeddingComponents/TodoListItem.razor", "Delete", ["ApplyToItem("] )];

        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Rsvp/RsvpFormQuestions.razor", "Submit", ["RsvpService.EditRsvp(", "RsvpService.SubmitRsvp("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor", "ClaimItem", ["RegistryService.ClaimRegistryItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor", "RemoveClaim", ["RegistryService.UnclaimRegistryItem("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor", "ChoosePurchaseMethod", ["RegistryService.ChoosePurchaseMethod("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor", "ChooseDeliveryAddress", ["RegistryService.ChooseDeliveryAddress("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor", "MarkCompleted", ["RegistryService.MarkClaimAsCompleted("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor", "SaveNotes", ["RegistryService.SetClaimNotes("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor", "AdminUnmarkClaimAsCompleted", ["RegistryService.MarkClaimAsNotCompleted("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor", "AdminMarkClaimAsCompleted", ["RegistryService.MarkClaimAsCompleted("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor", "AdminRemoveClaim", ["RegistryService.UnclaimRegistryItem("] )];

        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Account.razor", "ChangePassword", ["UserManager.ResetPasswordAsync("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Auth/Register.razor", "RegisterUser", ["UserManager.CreateAsync(", "AdminService.AddGuestToAccount("] )];
        yield return [new MutationMethodCase("WeddingWebsite/Components/Pages/Auth/Setup.razor", "RegisterUser", ["UserManager.CreateAsync(", "UserManager.AddToRoleAsync("] )];
    }

    public static IEnumerable<object[]> InlineMutationLambdaCases()
    {
        yield return [new InlineLambdaCase(
            "WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor",
            "RegistryService.MarkClaimAsNotCompleted(")];
        yield return [new InlineLambdaCase(
            "WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor",
            "RegistryService.MarkClaimAsCompleted(")];
        yield return [new InlineLambdaCase(
            "WeddingWebsite/Components/Pages/Registry/RegistryItemPage.razor",
            "RegistryService.UnclaimRegistryItem(")];
    }

    [Theory]
    [MemberData(nameof(MutationCases))]
    public void S31_Bullet1_MutatingMethod_HasEntryAuthorizationBeforeMutation(MutationMethodCase methodCase)
    {
        var source = File.ReadAllText(ToAbsolutePath(methodCase.FilePath));
        var body = ExtractMethodBody(source, methodCase.MethodName);
        Assert.False(string.IsNullOrWhiteSpace(body), $"Method '{methodCase.MethodName}' not found in {methodCase.FilePath}.");

        var firstMutationIndex = FindFirstIndex(body, methodCase.MutationMarkers);
        Assert.True(firstMutationIndex >= 0, $"No mutation marker found in method '{methodCase.MethodName}' ({methodCase.FilePath}).");

        var firstAuthorizationIndex = FindFirstIndex(body, AuthorizationMarkers);
        Assert.True(
            firstAuthorizationIndex >= 0 && firstAuthorizationIndex < firstMutationIndex,
            $"Method '{methodCase.MethodName}' in {methodCase.FilePath} is missing entry authorization before mutation.");
    }

    [Theory]
    [MemberData(nameof(InlineMutationLambdaCases))]
    public void S31_Bullet1_InlineMutationLambda_HasProceduralAuthorizationGuardNearby(InlineLambdaCase lambdaCase)
    {
        var source = File.ReadAllText(ToAbsolutePath(lambdaCase.FilePath));
        var mutationIndex = source.IndexOf(lambdaCase.MutationMarker, StringComparison.Ordinal);
        Assert.True(mutationIndex >= 0, $"Inline mutation marker not found: {lambdaCase.MutationMarker}");

        var scanStart = Math.Max(0, mutationIndex - 600);
        var scanLength = Math.Min(source.Length - scanStart, 1200);
        var contextWindow = source.Substring(scanStart, scanLength);

        var hasAuthorizationMarker = false;
        foreach (var marker in AuthorizationMarkers)
        {
            if (contextWindow.Contains(marker, StringComparison.Ordinal))
            {
                hasAuthorizationMarker = true;
                break;
            }
        }

        Assert.True(
            hasAuthorizationMarker,
            $"Inline mutation '{lambdaCase.MutationMarker}' is missing nearby procedural authorization marker.");
    }

    private static string ToAbsolutePath(string relativePath) =>
        Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private static int FindFirstIndex(string text, IReadOnlyList<string> markers)
    {
        var best = -1;
        foreach (var marker in markers)
        {
            var index = text.IndexOf(marker, StringComparison.Ordinal);
            if (index >= 0 && (best < 0 || index < best))
            {
                best = index;
            }
        }
        return best;
    }

    private static string ExtractMethodBody(string source, string methodName)
    {
        var pattern = $@"\b(public|private|protected)\s+(async\s+)?(Task|void|ValueTask)\s+{Regex.Escape(methodName)}\s*\(";
        var match = Regex.Match(source, pattern);
        if (!match.Success)
        {
            return string.Empty;
        }

        var braceStart = source.IndexOf('{', match.Index);
        if (braceStart < 0)
        {
            return string.Empty;
        }

        var depth = 0;
        for (var i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            if (source[i] == '}') depth--;
            if (depth == 0)
            {
                return source.Substring(braceStart, i - braceStart + 1);
            }
        }

        return string.Empty;
    }

    public sealed record MutationMethodCase(string FilePath, string MethodName, string[] MutationMarkers);
    public sealed record InlineLambdaCase(string FilePath, string MutationMarker);
}
