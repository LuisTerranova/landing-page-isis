using MudBlazor;

namespace landing_page_isis;

public class IsisTheme : MudTheme
{
    public IsisTheme()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#284B32",          
            Secondary = "#7E0F20",
            Background = "#F7EAD5",      
            Surface = "#F7EAD5",
            AppbarBackground = "#284B32",
            AppbarText = "#F7EAD5",
            TextPrimary = "#284B32",
            TextSecondary = "#7E0F20",
            DrawerBackground = "#F7EAD5",
            DrawerText = "#284B32",
            ActionDefault = "#560414"
        };

        Typography = new Typography()
        {
            Default = new DefaultTypography()
            {
                FontFamily = new[] { "Glacial Indifference", "sans-serif" },
                FontSize = "1rem",
                LineHeight = "1.6"
            },
            H1 = new H1Typography()
            {
                FontFamily = new[] { "Glacial Indifference", "sans-serif" },
                FontSize = "3.5rem",
                FontWeight = "700"
            },
            H2 = new H2Typography()
            {
                FontFamily = new[] { "Glacial Indifference", "sans-serif" },
                FontSize = "2.5rem",
                FontWeight = "700"
            },
            H3 = new H3Typography()
            {
                FontFamily = new[] { "Glacial Indifference", "sans-serif" },
                FontSize = "2rem",
                FontWeight = "700"
            },
            Button = new ButtonTypography()
            {
                FontFamily = new[] { "Glacial Indifference", "sans-serif" },
                FontWeight = "700",
                TextTransform = "uppercase"
            }
        };

        LayoutProperties = new LayoutProperties()
        {
            DefaultBorderRadius = "24px"
        };
    }
}