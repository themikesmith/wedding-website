using WeddingWebsite.Models.ConfigInterfaces;
using WeddingWebsite.Models.Theme;
using WeddingWebsite.Models.WebsiteConfig;
using WeddingWebsite.Models.WebsiteElement;

namespace WeddingWebsite.Config.ThemeAndLayout;

public class TestConfig : DefaultConfig, IWebsiteConfig
{
    /**public new WeddingColours Colours { get; } = new (
        new Colour("#b9b8ff"),
        new Colour("#d0f0ff"),
        new Colour("#b9b8ff"),
        new Colour("#FAFAE8")
    );*/
    public new WeddingColours Colours { get; } = new (
        new Colour("#D5C9DD"), // lavenderFog
        new Colour("#d1e5f4"), // subtleBlue
        new Colour("#AFA4CE"), // lavender
        new Colour("#fffaee") // islandSpice
    );
    public new DemoMode DemoMode => new DemoMode.Enabled([]);
    public new IEnumerable<string> IgnoredValidationIssues => [
        "You have both a Timeline and SimpleTimeline section. Since both sections display the same information, choose the level of detail you want and remove the other section."
    ];

    public TestConfig() {
        var surfaceVariant = new Colour(254, 252, 231);
        
        // Some pastel colours here that you may want to choose from
        var pink = new Colour("#ffd6d6");
        var darkPink = new Colour("#ffbaba");
        var orange = new Colour("#ffe3c6");
        var darkOrange = new Colour("#ffcc99");
        var yellow = new Colour("#fff2b5");
        var brightYellow = new Colour("#ffda87");
        var lightGreen = new Colour("#f6f4d0");
        var green = new Colour("#e4f7bf");
        var darkGreen = new Colour("#cff3a8");
        var blueGreen = new Colour("#bfe7c6");
        var greenBlue = new Colour("#8edfd1");
        var lightBlue = new Colour("#d0f0ff");
        var darkLightBlue = new Colour("b3e9ee");
        var purple = new Colour("#d6e2ff");
        var darkPurple = new Colour("#b9b8ff");
        
        // And some others
        var darkDarkGreen = new Colour("#73A043");
        var darkDarkPurple = new Colour("#8D8BFA");
        var salmon = new Colour(236, 129, 108, Colour.VeryDarkGrey);
        // our colors
        var peachFuzz = new Colour("#ffbe98");
        var apricotNectar = new Colour("#ecaa79");
        var lavender = new Colour("#AFA4CE");
        var lavenderFog = new Colour("#D5C9DD");
        var pastelLilac = new Colour("#BCAFCF");
        var islandSpice = new Colour("#fffaee");
        var subtleBlue = new Colour("#D1E5F4");

        // A few box styles you might like to try
        var filledBox = new BoxStyle(BoxType.FilledRounded, new SectionTheme(Colours.PrimaryBackground, darkDarkPurple, null));
        var whiteFilledBox = new BoxStyle(BoxType.FilledRounded, new SectionTheme(Colour.White, Colours.Primary, null));
        var outlinedBox = new BoxStyle(BoxType.OutlinedSquare, new SectionTheme(Colour.White, Colours.Primary, null));
        
        // Some background images (note some are AI-generated)
        var bricks = new BackgroundImage("/bg/bricks.jpg", false, "500px", new Colour(255, 255, 255, 150), 0.3, true);
        var flowers = new BackgroundImage("/bg/blue-flowers.png", false, "500px", new Colour(255, 255, 255, 150), 0.3, true);
        var forest = new BackgroundImage("https://www.metroparks.net/wp-content/uploads/2017/06/1080p_HBK_autumn-morning_GI.jpg", false, "100%", null, 1);
        var flowers2 = new BackgroundImage("https://images.pexels.com/photos/355748/pexels-photo-355748.jpeg?cs=srgb&dl=pexels-pixabay-355748.jpg&fm=jpg", true, "min(100%, 1300px)", null, 0);
    
        Sections = [
            new Section.TodoListSummary(new SectionTheme(apricotNectar, Colour.White, new BoxStyle(BoxType.FilledRounded, new SectionTheme(Colours.PrimaryBackground, Colour.White, null)))),
            new Section.HowWeMet(new SectionTheme(peachFuzz, Colours.Primary, filledBox)),
            new Section.Timeline(new SectionTheme(bricks, Colours.Primary, outlinedBox), true),
            new Section.VenueShowcase(new SectionTheme(apricotNectar, Colours.Primary, new BoxStyle(BoxType.FilledRounded, new SectionTheme(pastelLilac, darkDarkPurple, null)))),
            new Section.MeetWeddingParty(new SectionTheme(flowers, Colours.Primary, outlinedBox)),
            new Section.Accommodation(new SectionTheme(peachFuzz, Colours.Primary, filledBox)),
            new Section.TravelDirections(new SectionTheme(Colours.Surface, Colours.Primary, outlinedBox)),
            new Section.SimpleTimeline(new SectionTheme(flowers2, Colours.Primary, filledBox)),
            new Section.Gallery(),
            new Section.DressCode(new SectionTheme(forest, Colours.Primary, filledBox), true, false),
            new Section.Contact(new SectionTheme(peachFuzz, Colours.Secondary, whiteFilledBox))
        ];
        
        TopButtons = new TopButtonsConfig(
            [
                new LinkButton("Accommodation", "#accommodation"),
                new LinkButton("RSVP", "/rsvp")
            ],
            yellow
        );

        Navbar = new NavbarConfig(
            [
                new LinkButton("Home", "/"),
                new LinkButton("Timeline & Transport", "/#timeline"),
                new LinkButton("Accommodation", "/#accommodation"),
                new LinkButton("Registry", "/registry"),
                new LinkButton("Gallery", "/gallery"),
                new LinkButton("Contact", "/#contact")
            ]
        );

        AccountConfig = new PageConfig.Account(new SectionTheme(Colours.PrimaryBackground.WithAlpha(150), pink, whiteFilledBox));
        
        RegistryConfig = new PageConfig.Registry(new SectionTheme(Colours.Surface, Colours.Primary, outlinedBox));
        RegistryItemConfig = new PageConfig.RegistryItem(new SectionTheme(Colours.Surface, Colours.Primary, whiteFilledBox));
        LoginConfig = new PageConfig.Login(new SectionTheme(Colours.PrimaryBackground, Colours.Primary, whiteFilledBox));
    }
}
