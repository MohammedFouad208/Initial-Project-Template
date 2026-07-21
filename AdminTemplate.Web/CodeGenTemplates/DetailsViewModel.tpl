namespace AdminTemplate.Web.Models.ViewModels;

/// <summary>
/// Auto-generated details view model for: {{ entity.name }}
/// Generated at: {{ generated_at }}
/// </summary>
public class {{ entity.name }}DetailsViewModel
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }

    {{- for col in entity.columns }}
    public {{ col.cs_type }}{{ if !col.is_required && col.data_type != "string" }}?{{ end }} {{ col.name }} { get; set; }{{ if col.data_type == "string" }} = string.Empty;{{ end }}
    {{- end }}

    {{- for rel in entity.relations }}
    public string {{ rel.navigation_property_name }}Display { get; set; } = string.Empty;
    {{- end }}
}
