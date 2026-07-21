namespace AdminTemplate.Application.Interfaces.CodeGen;

public record ThemeConfig(
    string PrimaryColor,
    string SecondaryColor,
    string SuccessColor,
    string DangerColor,
    string FontFamily,
    string SidebarBg,
    string SidebarText);

public interface IThemeService
{
    ThemeConfig LoadTheme(string projectRootPath);
    void SaveTheme(ThemeConfig config, string projectRootPath);
    string GenerateCss(ThemeConfig config);
}
