namespace WeddingWebsite.Models.Registry;

public record RegistryItemClaim(
    string ItemId,
    string UserId,
    string? PurchaseMethodId,
    string? DeliveryAddress,
    DateTime ClaimedAt,
    DateTime? CompletedAt,
    int Quantity,
    string? Notes,
    decimal Contribution
)
{
    public bool IsCompleted => CompletedAt != null;
}
