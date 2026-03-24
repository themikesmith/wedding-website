using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using WeddingWebsite.Models.Registry;

namespace WeddingWebsite.Data.Stores;

/// <summary>
/// EF Core LINQ implementation of IRegistryStore.
/// This version replaces raw SQL queries with strongly-typed LINQ queries.
/// </summary>
[Authorize]
public class RegistryStoreEf : IRegistryStore
{
    private readonly ApplicationDbContext _context;

    public RegistryStoreEf(ApplicationDbContext context)
    {
        _context = context;
    }

    // IDataStore interface methods - not used with EF Core, but required by interface
    DbConnection IDataStore.CreateConnection()
    {
        throw new NotSupportedException("RegistryStoreEf uses ApplicationDbContext, not raw SQL connections.");
    }

    void IDataStore.AddParameter(DbCommand command, string parameterName, object? value)
    {
        throw new NotSupportedException("RegistryStoreEf uses LINQ, not parameterized SQL queries.");
    }

    [Authorize(Roles = "Admin")]
    public void AddItem(RegistryItem item)
    {
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            _context.RegistryItems.Add(item);
            
            // Add purchase methods with ItemId shadow property
            foreach (var method in item.PurchaseMethods)
            {
                _context.RegistryItemPurchaseMethods.Add(method);
                _context.Entry(method).Property("ItemId").CurrentValue = item.Id;
            }

            _context.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    [Authorize(Roles = "Admin")]
    public void UpdateItem(RegistryItem item)
    {
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            // Remove existing purchase methods
            var existingMethods = _context.RegistryItemPurchaseMethods
                .Where(m => EF.Property<string>(m, "ItemId") == item.Id)
                .ToList();
            
            _context.RegistryItemPurchaseMethods.RemoveRange(existingMethods);

            // Update item
            var existingItem = _context.RegistryItems.FirstOrDefault(i => i.Id == item.Id);
            if (existingItem != null)
            {
                _context.RegistryItems.Entry(existingItem).CurrentValues.SetValues(item);
            }

            // Add new purchase methods with ItemId shadow property
            foreach (var method in item.PurchaseMethods)
            {
                _context.RegistryItemPurchaseMethods.Add(method);
                _context.Entry(method).Property("ItemId").CurrentValue = item.Id;
            }

            _context.SaveChanges();
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    [Authorize(Roles = "Admin")]
    public bool DeleteItem(string itemId)
    {
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var item = _context.RegistryItems.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                return false;

            _context.RegistryItems.Remove(item);
            _context.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public RegistryItem? GetRegistryItemById(string itemId)
    {
        // Query only scalar properties from the database
        var item = _context.RegistryItems
            .AsNoTracking()
            .FirstOrDefault(i => i.Id == itemId);

        if (item == null)
            return null;

        // Load navigation properties separately
        var methods = _context.RegistryItemPurchaseMethods
            .AsNoTracking()
            .Where(m => EF.Property<string>(m, "ItemId") == itemId)
            .ToList();

        var claims = _context.RegistryItemClaims
            .AsNoTracking()
            .Where(c => c.ItemId == itemId)
            .ToList();

        // Create new instance with all properties including navigations
        return new RegistryItem(
            id: item.Id,
            genericName: item.GenericName,
            name: item.Name,
            description: item.Description,
            imageUrl: item.ImageUrl,
            purchaseMethods: methods,
            claims: claims,
            maxQuantity: item.MaxQuantity,
            priority: item.Priority,
            hide: item.Hide,
            allowsPartialContributions: item.AllowsPartialContributions,
            isDonation: item.IsDonation
        );
    }

    public async Task<IEnumerable<RegistryItem>> GetAllRegistryItems(bool includeHidden = false)
    {
        // Query scalar properties from database
        var itemsQuery = _context.RegistryItems.AsNoTracking().AsQueryable();
        if (!includeHidden)
        {
            itemsQuery = itemsQuery.Where(i => !i.Hide);
        }
        var items = await itemsQuery.OrderByDescending(i => i.Priority).ToListAsync();

        if (items.Count == 0)
            return [];

        var itemIds = items.Select(i => i.Id).ToList();

        // Bulk-load all purchase methods and claims in two queries instead of N+1.
        // Shadow property "ItemId" must be projected via EF.Property to allow in-memory grouping.
        var allMethodsWithItemId = await _context.RegistryItemPurchaseMethods
            .AsNoTracking()
            .Where(m => itemIds.Contains(EF.Property<string>(m, "ItemId")))
            .Select(m => new { Method = m, ItemId = EF.Property<string>(m, "ItemId") })
            .ToListAsync();

        var allClaims = await _context.RegistryItemClaims
            .AsNoTracking()
            .Where(c => itemIds.Contains(c.ItemId))
            .ToListAsync();

        var methodsByItemId = allMethodsWithItemId
            .GroupBy(x => x.ItemId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Method).ToList());

        var claimsByItemId = allClaims
            .GroupBy(c => c.ItemId)
            .ToDictionary(g => g.Key, g => g.ToList());

        return items.Select(item => new RegistryItem(
            id: item.Id,
            genericName: item.GenericName,
            name: item.Name,
            description: item.Description,
            imageUrl: item.ImageUrl,
            purchaseMethods: methodsByItemId.GetValueOrDefault(item.Id, []),
            claims: claimsByItemId.GetValueOrDefault(item.Id, []),
            maxQuantity: item.MaxQuantity,
            priority: item.Priority,
            hide: item.Hide,
            allowsPartialContributions: item.AllowsPartialContributions,
            isDonation: item.IsDonation
        ));
    }

    [Authorize(Roles = "Admin")]
    public bool ClaimRegistryItem(string itemId, string userId, decimal contribution, int quantity = 1)
    {
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            // Check if already claimed
            var existingClaim = _context.RegistryItemClaims
                .FirstOrDefault(c => c.ItemId == itemId && c.UserId == userId);
            
            if (existingClaim != null)
                return false;

            var claim = new RegistryItemClaim(
                ItemId: itemId,
                UserId: userId,
                PurchaseMethodId: null,
                DeliveryAddress: null,
                ClaimedAt: DateTime.UtcNow,
                CompletedAt: null,
                Quantity: quantity,
                Notes: null,
                Contribution: contribution
            );

            _context.RegistryItemClaims.Add(claim);
            _context.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    [Authorize(Roles = "Admin")]
    public bool UnclaimRegistryItem(string itemId, string userId)
    {
        using var transaction = _context.Database.BeginTransaction();
        try
        {
            var claim = _context.RegistryItemClaims
                .FirstOrDefault(c => c.ItemId == itemId && c.UserId == userId);

            if (claim == null)
                return false;

            _context.RegistryItemClaims.Remove(claim);
            _context.SaveChanges();
            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    [Authorize(Roles = "Admin")]
    public void ChoosePurchaseMethod(string itemId, string userId, string? purchaseMethodId)
    {
        var claim = _context.RegistryItemClaims
            .FirstOrDefault(c => c.ItemId == itemId && c.UserId == userId);

        if (claim == null)
            return;

        var updatedClaim = claim with { PurchaseMethodId = purchaseMethodId };
        _context.Entry(claim).State = EntityState.Detached;
        _context.Attach(updatedClaim);
        _context.Entry(updatedClaim).State = EntityState.Modified;
        _context.SaveChanges();
    }

    [Authorize(Roles = "Admin")]
    public void ChooseDeliveryAddress(string itemId, string userId, string? address)
    {
        var claim = _context.RegistryItemClaims
            .FirstOrDefault(c => c.ItemId == itemId && c.UserId == userId);

        if (claim == null)
            return;

        var updatedClaim = claim with { DeliveryAddress = address };
        _context.Entry(claim).State = EntityState.Detached;
        _context.Attach(updatedClaim);
        _context.Entry(updatedClaim).State = EntityState.Modified;
        _context.SaveChanges();
    }

    [Authorize(Roles = "Admin")]
    public void MarkClaimAsCompleted(string itemId, string userId)
    {
        var claim = _context.RegistryItemClaims
            .FirstOrDefault(c => c.ItemId == itemId && c.UserId == userId);

        if (claim == null)
            return;

        var updatedClaim = claim with { CompletedAt = DateTime.UtcNow };
        _context.Entry(claim).State = EntityState.Detached;
        _context.Attach(updatedClaim);
        _context.Entry(updatedClaim).State = EntityState.Modified;
        _context.SaveChanges();
    }

    [Authorize(Roles = "Admin")]
    public void MarkClaimAsNotCompleted(string itemId, string userId)
    {
        var claim = _context.RegistryItemClaims
            .FirstOrDefault(c => c.ItemId == itemId && c.UserId == userId);

        if (claim == null)
            return;

        var updatedClaim = claim with { CompletedAt = null };
        _context.Entry(claim).State = EntityState.Detached;
        _context.Attach(updatedClaim);
        _context.Entry(updatedClaim).State = EntityState.Modified;
        _context.SaveChanges();
    }

    [Authorize(Roles = "Admin")]
    public void SetClaimNotes(string itemId, string userId, string? notes)
    {
        var claim = _context.RegistryItemClaims
            .FirstOrDefault(c => c.ItemId == itemId && c.UserId == userId);

        if (claim == null)
            return;

        var updatedClaim = claim with { Notes = notes };
        _context.Entry(claim).State = EntityState.Detached;
        _context.Attach(updatedClaim);
        _context.Entry(updatedClaim).State = EntityState.Modified;
        _context.SaveChanges();
    }
}
