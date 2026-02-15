namespace WeddingWebsite.Models.People;

public record ContactWebsite(string url) : IContactMethod
{
    public string Type => "Website";
    public string Text => url;
    public string? Link => url;
}
