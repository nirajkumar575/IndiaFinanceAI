using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace IndiaFinanceAI.Api.Controllers;

public sealed class AdminLoginRequest
{
    [Required, StringLength(100)]
    public string Username { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Password { get; init; } = string.Empty;
}

[ApiController]
[AllowAnonymous]
[Route("api/auth/admin")]
public sealed class AdminAuthController(IConfiguration configuration) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("admin-login")]
    public IActionResult Login([FromBody] AdminLoginRequest request)
    {
        var configuredUsername = configuration["Admin:Username"]!;
        var configuredPassword = configuration["Admin:Password"]!;
        if (!FixedTimeEquals(request.Username, configuredUsername)
            || !FixedTimeEquals(request.Password, configuredPassword))
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var issuer = configuration["Jwt:Issuer"] ?? "IndiaFinanceAI";
        var audience = configuration["Jwt:Audience"] ?? "IndiaFinanceAI.Admin";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));
        var expiresAt = DateTime.UtcNow.AddMinutes(20);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [
                new Claim(ClaimTypes.NameIdentifier, configuredUsername),
                new Claim(ClaimTypes.Name, configuredUsername),
                new Claim(ClaimTypes.Role, "Admin"),
            ],
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), expiresAt });
    }

    private static bool FixedTimeEquals(string submitted, string configured)
    {
        var submittedBytes = Encoding.UTF8.GetBytes(submitted);
        var configuredBytes = Encoding.UTF8.GetBytes(configured);
        return submittedBytes.Length == configuredBytes.Length
            && CryptographicOperations.FixedTimeEquals(submittedBytes, configuredBytes);
    }
}
