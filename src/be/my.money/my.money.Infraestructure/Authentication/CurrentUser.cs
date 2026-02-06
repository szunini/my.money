using Microsoft.AspNetCore.Http;
using my.money.application.Ports.Authentication;
using System.Security.Claims;

namespace my.money.Infraestructure.Authentication;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            // Try getting from "sub" claim first (JWT standard)
            var subClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");
            return subClaim?.Value;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
