using System;
using System.Collections.Generic;
using System.Text;

namespace my.money.application.Ports.Authentication
{
    public sealed record AuthResult(string AccessToken);
}
