using Microsoft.AspNetCore.Identity;
using my.money.application.Ports.Authentication;

namespace my.money.Infraestructure.Authentication;

public sealed class IdentityUserAuthProvider : IUserAuthProvider
{
    private readonly UserManager<ApplicationUser> _userManager;

    public IdentityUserAuthProvider(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserAuthInfo?> ValidateCredentialsAsync(string email, string password)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null) return null;

        var ok = await _userManager.CheckPasswordAsync(user, password);
        if (!ok) return null;

        var roles = await _userManager.GetRolesAsync(user);

        user.MarkLogin();
        await _userManager.UpdateAsync(user);

        
        return new UserAuthInfo(user.Id, user.Email!, roles.ToList().AsReadOnly());
    }
}