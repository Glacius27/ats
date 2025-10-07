using System;
using System.Collections.Generic;

namespace AuthorizationService.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string KeycloakUserId { get; set; } = default!;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        // связь многие-ко-многим с ролями
        public ICollection<Role> Roles { get; set; } = new List<Role>();
    }
}