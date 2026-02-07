using WeddingWebsite.Models.WebsiteElement;

namespace WeddingWebsite.Models.Gallery;

public record GalleryItem(WebsiteImage Image, int Size = 1, bool runAtServer = false)
{
    /// <summary>
    /// Create a gallery item with a URL image and no alt text.
    /// </summary>
    public GalleryItem(string url) : this(new WebsiteImage(url, null, false)) {}
    public GalleryItem(string url, bool runatServer) : this(new WebsiteImage(url, null, runatServer)) {}
}
