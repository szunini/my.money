using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.application.Ports.Authentication
{
    public interface IUserAuthProvider
    {
        Task<UserAuthInfo?> ValidateCredentialsAsync(string email, string password);
    }
}
