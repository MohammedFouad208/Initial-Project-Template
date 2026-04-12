using System.ComponentModel.DataAnnotations;
using AdminTemplate.Application.DTOs;

namespace AdminTemplate.Web.ViewModels.Users;

public class EditUserViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(200, ErrorMessage = "Full name cannot exceed 200 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public List<string> SelectedRoles { get; set; } = [];

    /// <summary>Not bound on POST — loaded from server each time.</summary>
    public List<RoleDto> AvailableRoles { get; set; } = [];
}
