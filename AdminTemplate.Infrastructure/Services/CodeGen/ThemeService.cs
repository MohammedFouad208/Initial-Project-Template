using AdminTemplate.Application.Interfaces.CodeGen;
using System.Text;
using System.Text.Json;

namespace AdminTemplate.Infrastructure.Services.CodeGen;

public class ThemeService : IThemeService
{
    private static readonly ThemeConfig DefaultTheme = new(
        PrimaryColor:   "#0d6efd",
        SecondaryColor: "#6c757d",
        SuccessColor:   "#198754",
        DangerColor:    "#dc3545",
        FontFamily:     "'Inter', sans-serif",
        SidebarBg:      "#1e2a3a",
        SidebarText:    "#c8d3e0");

    public ThemeConfig LoadTheme(string projectRootPath)
    {
        var path = GetThemePath(projectRootPath);
        if (!File.Exists(path)) return DefaultTheme;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ThemeConfig>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? DefaultTheme;
        }
        catch
        {
            return DefaultTheme;
        }
    }

    public void SaveTheme(ThemeConfig config, string projectRootPath)
    {
        var path = GetThemePath(projectRootPath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);

        var css = GenerateCss(config);
        var cssPath = Path.Combine(projectRootPath, "AdminTemplate.Web", "wwwroot", "css", "theme.css");
        File.WriteAllText(cssPath, css);
    }

    public string GenerateCss(ThemeConfig config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("/* Auto-generated theme — do not edit manually */");
        sb.AppendLine(":root {");
        sb.AppendLine($"  --primary:   {config.PrimaryColor};");
        sb.AppendLine($"  --secondary: {config.SecondaryColor};");
        sb.AppendLine($"  --success:   {config.SuccessColor};");
        sb.AppendLine($"  --danger:    {config.DangerColor};");
        sb.AppendLine($"  --font-family: {config.FontFamily};");
        sb.AppendLine($"  --sidebar-bg:   {config.SidebarBg};");
        sb.AppendLine($"  --sidebar-text: {config.SidebarText};");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("body { font-family: var(--font-family); }");
        sb.AppendLine(".sidebar { background-color: var(--sidebar-bg); color: var(--sidebar-text); }");
        sb.AppendLine(".btn-primary { background-color: var(--primary); border-color: var(--primary); }");
        sb.AppendLine(".text-primary, a.nav-link.active { color: var(--primary) !important; }");
        return sb.ToString();
    }

    private static string GetThemePath(string projectRootPath) =>
        Path.Combine(projectRootPath, "AdminTemplate.Web", "Config", "theme.json");
}
