using AdminTemplate.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Domain.Entities;

/// <summary>
/// Auto-generated entity: {{ entity.name }}
/// Generated at: {{ generated_at }}
/// </summary>
public partial class {{ entity.name }} : BaseEntity
{
    {{- for col in entity.columns }}
    {{- if col.is_required }}
    [Required]
    {{- end }}
    {{- if col.max_length }}
    [MaxLength({{ col.max_length }})]
    {{- end }}
    public {{ col.cs_type }}{{ if !col.is_required && col.data_type != "string" }}?{{ end }} {{ col.name }} { get; set; }{{ if col.data_type == "string" }} = string.Empty;{{ else if col.is_required }} = default!;{{ end }}
    {{ end }}
    {{- for rel in entity.relations }}
    public Guid {{ rel.foreign_key_name }} { get; set; }
    public {{ rel.related_entity_name }}? {{ rel.navigation_property_name }} { get; set; }
    {{ end }}
}
