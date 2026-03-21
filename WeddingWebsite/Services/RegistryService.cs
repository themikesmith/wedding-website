using Microsoft.AspNetCore.Authorization;
using WeddingWebsite.Core;
using WeddingWebsite.Data.Stores;
using WeddingWebsite.Models.Registry;

namespace WeddingWebsite.Services;

[Authorize]
public class RegistryService(IRegistryStore registryStore, ICurrentUserContext currentUserContext) : IRegistryService
{
    [Authorize(Roles = "Admin")]
    public void AddItem(RegistryItem item)
    {
        currentUserContext.EnsureInRole("Admin");
        registryStore.AddItem(item);
    }
    
    [Authorize(Roles = "Admin")]
    public void UpdateItem(RegistryItem item)
    {
        currentUserContext.EnsureInRole("Admin");
        registryStore.UpdateItem(item);
    }
    
    [Authorize(Roles = "Admin")]
    public bool DeleteItem(string itemId)
    {
        currentUserContext.EnsureInRole("Admin");
        return registryStore.DeleteItem(itemId);
    }
    
    public RegistryItem? GetRegistryItemById(string itemId)
    {
        currentUserContext.EnsureAuthenticated();
        return registryStore.GetRegistryItemById(itemId);
    }
    
    public Task<IEnumerable<RegistryItem>> GetAllRegistryItems(bool includeHidden = false)
    {
        currentUserContext.EnsureAuthenticated();
        return registryStore.GetAllRegistryItems(includeHidden);
    }
    
    public bool ClaimRegistryItem(string itemId, string userId, decimal contribution, int quantity = 1)
    {
        currentUserContext.EnsureAuthenticated();
        return registryStore.ClaimRegistryItem(itemId, userId, contribution, quantity);
    }
    
    public bool UnclaimRegistryItem(string itemId, string userId)
    {
        currentUserContext.EnsureAuthenticated();
        return registryStore.UnclaimRegistryItem(itemId, userId);
    }
    
    public void ChoosePurchaseMethod(string itemId, string userId, string? purchaseMethodId)
    {
        currentUserContext.EnsureAuthenticated();
        registryStore.ChoosePurchaseMethod(itemId, userId, purchaseMethodId);
    }
    
    public void ChooseDeliveryAddress(string itemId, string userId, string? address)
    {
        currentUserContext.EnsureAuthenticated();
        registryStore.ChooseDeliveryAddress(itemId, userId, address);
    }
    
    public void MarkClaimAsCompleted(string itemId, string userId)
    {
        currentUserContext.EnsureAuthenticated();
        registryStore.MarkClaimAsCompleted(itemId, userId);
    }
    
    [Authorize(Roles = "Admin")]
    public void MarkClaimAsNotCompleted(string itemId, string userId)
    {
        currentUserContext.EnsureInRole("Admin");
        registryStore.MarkClaimAsNotCompleted(itemId, userId);
    }
    
    public void SetClaimNotes(string itemId, string userId, string? notes)
    {
        currentUserContext.EnsureAuthenticated();
        registryStore.SetClaimNotes(itemId, userId, notes);
    }
}
