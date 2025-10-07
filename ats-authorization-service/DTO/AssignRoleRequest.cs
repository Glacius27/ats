using System.ComponentModel.DataAnnotations;

namespace AuthorizationService.DTO;

public class AssignRoleRequest
{
    [Required, MaxLength(100)]
    public string RoleName { get; set; } = null!;
}