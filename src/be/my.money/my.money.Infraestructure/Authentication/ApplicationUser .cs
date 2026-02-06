using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.Infraestructure.Authentication
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
        public DateTime? LastLoginUtc { get; private set; }
        public bool IsActive { get; private set; } = true;

        public void MarkLogin() => LastLoginUtc = DateTime.UtcNow;
        public void Deactivate() => IsActive = false;
    }
}
