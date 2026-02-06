using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using my.money.application.Ports.Authentication;
using my.money.Infraestructure.Authentication;

namespace my.money.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>Registro de usuario</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email y password son obligatorios." });

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return Conflict(new { message = "Ya existe un usuario con ese email." });

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email.Trim().ToLowerInvariant(),
            Email = request.Email.Trim().ToLowerInvariant()
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
            return BadRequest(new { message = "No se pudo crear el usuario.", errors = result.Errors.Select(e => e.Description) });

        _logger.LogInformation("Usuario registrado: {Email} (Id={UserId})", user.Email, user.Id);

        // opcional: auto-login después de register
        var auth = await _authService.LoginAsync(request.Email, request.Password);
        return Ok(new AuthResult(auth.AccessToken));
    }

    /// <summary>Login (devuelve JWT)</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var auth = await _authService.LoginAsync(request.Email, request.Password);
            return Ok(new AuthResult(auth.AccessToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }
    }

    /// <summary>Devuelve info básica del usuario logueado</summary>
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        // claims típicos:
        // sub = userId
        // email = email
        var userId = User.FindFirst("sub")?.Value;
        var email = User.FindFirst("email")?.Value ?? User.Identity?.Name;

        return Ok(new { userId, email });
    }
}
