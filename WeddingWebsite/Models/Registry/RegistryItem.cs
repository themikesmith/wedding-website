namespace WeddingWebsite.Models.Registry;

public record RegistryItem
{
    // Primary properties (mapped to database columns)
    public string Id { get; init; } = null!;
    public string GenericName { get; init; } = null!;
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public int MaxQuantity { get; init; } = 1;
    public int Priority { get; init; } = 0;
    public bool Hide { get; init; } = false;
    public bool AllowsPartialContributions { get; init; } = false;
    public bool IsDonation { get; init; } = false;
    
    // Navigation properties (not bound to constructor by EF Core)
    public ICollection<RegistryItemPurchaseMethod> PurchaseMethods { get; init; } = [];
    public ICollection<RegistryItemClaim> Claims { get; init; } = [];

    // Simplified constructor for EF Core LINQ queries (only scalar properties)
    public RegistryItem()
    {
    }

    // Full constructor for manual object creation
    public RegistryItem(
        string id,
        string genericName,
        string name,
        string? description,
        string? imageUrl,
        IEnumerable<RegistryItemPurchaseMethod> purchaseMethods,
        IEnumerable<RegistryItemClaim> claims,
        int maxQuantity = 1,
        int priority = 0,
        bool hide = false,
        bool allowsPartialContributions = false,
        bool isDonation = false
    )
    {
        Id = id;
        GenericName = genericName;
        Name = name;
        Description = description;
        ImageUrl = imageUrl;
        PurchaseMethods = purchaseMethods.ToList();
        Claims = claims.ToList();
        MaxQuantity = maxQuantity;
        Priority = priority;
        Hide = hide;
        AllowsPartialContributions = allowsPartialContributions;
        IsDonation = isDonation;
    }

    public int QuantityClaimed => Claims.Sum(c => c.Quantity);
    public decimal ClaimedAmount => Claims.Sum(c => c.Contribution);
    //public bool IsFullyClaimed => QuantityClaimed >= MaxQuantity;
    public decimal CheapestCost => PurchaseMethods.Min(pm => pm.Cost);
    public decimal MaxCost => PurchaseMethods.Max(pm => pm.Cost);

    public IEnumerable<RegistryItemPurchaseMethod> GetPurchaseMethods()
    {
        // TODO make "buy in full" a set string
        if (AllowsPartialContributions)
        {
            return PurchaseMethods.Where(p => ! p.Name.Contains("(buy in full)"));
        }
        else
        {
            return PurchaseMethods;
        }
    }

    public bool IsFullyClaimed()
    {
        if (AllowsPartialContributions)
        {
            return ClaimedAmount >= CheapestCost;
        }
        else
        {
            return QuantityClaimed >= MaxQuantity;
        }
    }

    public decimal RemainingCost()
    {
        return CheapestCost - ClaimedAmount;
    }
    
    public int NumClaimsByUser(string userId)
    {
        return Claims.Where(c => c.UserId == userId).Sum(c => c.Quantity);
    }
    
    /// <summary>
    /// Gets the unique claim for the specified user. Throws if there is no such claim.
    /// </summary>
    public RegistryItemClaim GetClaimByUser(string userId)
    {
        return Claims.Single(c => c.UserId == userId);
    }

    /// <summary>
    /// Gets the purchase method that the user has selected. Throws if the user hasn't selected a purchase method.
    /// </summary>
    public RegistryItemPurchaseMethod GetPurchaseMethodByUser(string userId)
    {
        var claim = GetClaimByUser(userId);
        return PurchaseMethods.Single(pm => pm.Id == claim.PurchaseMethodId);
    }
}
