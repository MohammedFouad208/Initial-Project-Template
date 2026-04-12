using System.ComponentModel.DataAnnotations;

namespace AdminTemplate.Web.ViewModels.Roles;

public class CreateRoleViewModel
{
    [Required(ErrorMessage = "Role name is required.")]
    [MaxLength(100, ErrorMessage = "Role name cannot exceed 100 characters.")]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }
}
