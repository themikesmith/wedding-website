namespace WeddingWebsite.Models.WebsiteElement;

public class WebsiteImage(string url, string? altText, IEnumerable<ImageSizeVariation> customSizes, bool runatServer) : IWebsiteElement
{
    public string Url { get; } = url;
    public string? AltText { get; } = altText;
    public IEnumerable<ImageSizeVariation> CustomSizes { get; } = customSizes;
    // <asp:Image ID="Image1" runat="server" ImageUrl="~/HandlerCS.ashx?FileName=Penguins.jpg" />
    public bool RunAtServer { get; } = runatServer;
    
    public WebsiteImage(string url, string? altText) : this(url, altText, [], false) {}
    public WebsiteImage(string url, string? altText, bool runAtServer) : this(url, altText, [], runAtServer) {}

    public string GetHtml(string classList = "") {
        var optional_server_param = "";
        var tag_class = "img";
        if(RunAtServer) {
            optional_server_param = "runat=\"server\" ";
            //tag_class = "asp:Image";
            tag_class = "img";
        }
        if (CustomSizes.Any())
        {
            return $"<picture class=\"{classList}\">" +
                   string.Join("", CustomSizes.Select(size => $"<source srcset=\"{size.Src}\" media=\"(width >= {size.MinWidth}px)\">")) +
                   $"<{tag_class} class=\"{classList}\" {optional_server_param}src=\"{Url}\" alt=\"{AltText ?? ""}\" />" +
                   $"</picture>";
        }
        return $"<{tag_class} class=\"{classList}\" {optional_server_param}src=\"{Url}\" alt=\"{AltText ?? ""}\" />";
    }
}
