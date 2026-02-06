using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using my.money.application.Authentication;
using my.money.application.Ports.Authentication;
using my.money.application.Ports.Persistence;
using my.money.application.Ports.Queries;
using my.money.Infraestructure.Authentication;
using my.money.Infraestructure.Persistence;
using my.money.Infraestructure.Repositories;
using System.Text;

namespace my.money
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // CORS configuration to allow frontend calls
            const string FrontendCorsPolicy = "FrontendCors";

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(FrontendCorsPolicy, policy =>
                {
                    policy.WithOrigins("http://localhost:5173")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // Add services to the container.
            builder.Services.AddControllers();

            // Add DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            
            builder.Services
                    .AddOptions<JwtSettings>()
                    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
                    .Validate(s => !string.IsNullOrWhiteSpace(s.Key), "Jwt:Key is missing")
                    .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer), "Jwt:Issuer is missing")
                    .Validate(s => !string.IsNullOrWhiteSpace(s.Audience), "Jwt:Audience is missing")
                    .Validate(s => s.ExpiryMinutes > 0, "Jwt:ExpiryMinutes must be > 0")
                    .ValidateOnStart();
            // Add Identity
            builder.Services
                .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
                {
                    options.Password.RequiredLength = 8;
                    options.Password.RequireDigit = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Bearer";
                    options.DefaultChallengeScheme = "Bearer";
                })
                .AddJwtBearer("Bearer", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                        ),
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                    {
                        OnTokenValidated = ctx =>
                        {
                            var logger = ctx.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtBearer");

                            logger.LogInformation("JWT OK. sub={Sub}, email={Email}",
                                ctx.Principal?.FindFirst("sub")?.Value,
                                ctx.Principal?.FindFirst("email")?.Value);

                            return Task.CompletedTask;
                        },

                        OnAuthenticationFailed = ctx =>
                        {
                            var logger = ctx.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtBearer");

                            logger.LogError(ctx.Exception, "JWT FAIL");
                            return Task.CompletedTask;
                        },

                        OnChallenge = ctx =>
                        {
                            var logger = ctx.HttpContext.RequestServices
                                .GetRequiredService<ILoggerFactory>()
                                .CreateLogger("JwtBearer");

                            logger.LogWarning("JWT CHALLENGE: error={Error}, desc={Desc}",
                                ctx.Error, ctx.ErrorDescription);

                            return Task.CompletedTask;
                        }
                    };
                });

            builder.Services.AddAuthorization();

            // Add Authentication Services
            builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            builder.Services.AddScoped<IUserAuthProvider, IdentityUserAuthProvider>();
            builder.Services.AddScoped<IAuthService, LoginService>();

            // Add Repository Pattern
            builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Add Health Checks
            var healthChecksBuilder = builder.Services.AddHealthChecks();

            // Add Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // fuerza options (opcional pero recomendado)
            _ = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtSettings>>().Value;

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.MapGet("/", () => Results.Redirect("/swagger/index.html")).WithName("Redirect to Swagger").WithOpenApi();
            }

            app.UseRouting();

            app.UseCors(FrontendCorsPolicy);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHealthChecks("/health");

            app.Run();

        }
    }
}
