using WeddingWebsite.Models.WebsiteElement;

namespace WeddingWebsite.Models.WebsiteConfig;

public record GalleryButtonsConfig(
    IEnumerable<LinkButton> Buttons,
    Colour? Colour = null
);
