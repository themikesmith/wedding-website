namespace WeddingWebsite.Models.Accommodation;

public record Price(
    decimal Amount,
    Discount Discount,
    string? CustomString = null,
    string? CurrencySymbol = "£"
)
{
    public Price(decimal amount) : this(amount, Discount.None(), null, "£") {}
    public Price(decimal amount, string currencySym) : this(amount, Discount.None(), null, currencySym) {}

    public override string ToString() {
        if (CustomString != null) {
            return CustomString;
        } else if (Discount.PercentDiscount == 0) {
            return $"{CurrencySymbol}{Amount}";
        } else {
            return $"Full Price: {CurrencySymbol}{Amount}, With Discount: £{Discount.CalculateDiscountedPrice((float) Amount)} ({Discount.ClaimInstructions})";
        }
    }
    
    public float DiscountedPrice => Discount.CalculateDiscountedPrice((float) Amount);
}
