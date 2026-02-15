using WeddingWebsite.Models.WebsiteElement;

namespace WeddingWebsite.Models.WebsiteConfig;

public record DressCodeButtonsConfig(
    IEnumerable<LinkButton> Buttons,
    Colour? Colour = null
);
