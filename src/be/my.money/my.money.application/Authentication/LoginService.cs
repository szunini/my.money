using my.money.application.Ports.Authentication;

namespace my.money.application.Authentication;

public sealed class LoginService : IAuthService
{
    private readonly IUserAuthProvider _userAuthProvider;
    private readonly IJwtTokenGenerator _jwt;

    public LoginService(IUserAuthProvider userAuthProvider, IJwtTokenGenerator jwt)
    {
        _userAuthProvider = userAuthProvider;
        _jwt = jwt;
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var info = await _userAuthProvider.ValidateCredentialsAsync(email, password);

        if (info is null)
            throw new UnauthorizedAccessException();

        var token = _jwt.GenerateToken(info.UserId, info.Email, info.Roles);
        return new AuthResult(token);
    }
}
