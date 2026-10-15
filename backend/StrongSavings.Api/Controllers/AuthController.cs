
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StrongSavings.Api.Auth;

namespace StrongSavings.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string Issuer = "StrongSavings.Api";
    private const string Audience = "StrongSavings.Web";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public sealed record RegisterRequest(
        string Email,
        string Password);

    public sealed record LoginRequest(
        string Email,
        string Password);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) ||
            !new System.ComponentModel.DataAnnotations.EmailAddressAttribute()
                .IsValid(email))
        {
            return BadRequest(new { message = "Enter a valid email address." });
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Password is required." });
        }

        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser is not null)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            PersonId = null
        };

        var createResult = await _userManager.CreateAsync(
            user, request.Password);

        if (!createResult.Succeeded)
        {
            return BadRequest(new
            {
                message = "Account registration failed.",
                errors = createResult.Errors.Select(error => error.Description)
            });
        }

        var roleResult = await _userManager.AddToRoleAsync(user, "Member");

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Could not assign the member role." });
        }

        // New registrations are deliberately NOT linked to a club Person.
        // A trusted administrator must link the account after verifying identity.

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Account created. An administrator must link it to your club member record before savings can be viewed.",
            email = user.Email
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var user = await _userManager.FindByEmailAsync(email);

        if (user is null ||
            await _userManager.IsLockedOutAsync(user) ||
            !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            if (user is not null)
            {
                await _userManager.AccessFailedAsync(user);
            }

            return Unauthorized(new { message = "Invalid email or password." });
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role =>
            new Claim(ClaimTypes.Role, role)));

        // Include PersonId only when an administrator has linked the account.
        if (user.PersonId.HasValue)
        {
            claims.Add(new Claim("personId", user.PersonId.Value.ToString()));
        }

        var key = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT signing key is missing.");

        var signingKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(key));

        var credentials = new SigningCredentials(
            signingKey, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(60);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return Ok(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            tokenType = "Bearer",
            expiresAtUtc = expires,
            email = user.Email,
            roles,
            isLinkedToMember = user.PersonId.HasValue
        });
    }
}
