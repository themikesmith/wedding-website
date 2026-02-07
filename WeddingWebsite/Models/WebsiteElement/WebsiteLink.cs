namespace WeddingWebsite.Models.WebsiteElement;

public class WebsiteLink(string url, string? displayText = null) : IWebsiteElement
{
    public string Url { get; } = url;
    public string? DisplayText { get; } = displayText ?? url;

    public WebsiteLink(string url) : this(url, null) {}
    
    public string GetHtml(string classList = "") {
        if (DisplayText != null) {
            return $"<a href={Url}>{DisplayText}</a>";
        }
        else {
            return $"<a href={Url}>{Url}</a>";
        }
    }
    public override string ToString() {
        return Url;
    }
}
