using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.application.Ports.Authentication
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(Guid userId, string email, IEnumerable<string> roles);
    }
}
