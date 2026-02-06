using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.application.Ports.Authentication
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string email, string password);
    }
}
