using System;
using System.Collections.Generic;

namespace AuthorizationService.Models
{
    public class Role
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;

        // обратная связь с пользователями
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}