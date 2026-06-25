using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace HotelOS.API.Controllers;

/// <summary>
/// Dev/test-only token endpoint — NOT for production.
/// Generates a signed JWT for any role so Swagger/Postman tests can work
/// without a real identity provider.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config) => _config = config;

    /// <summary>
    /// Returns a signed JWT for the given role.
    /// Role must be one of: Guest, Staff, Admin
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    public IActionResult GetToken([FromBody] TokenRequest request)
    {
        var allowed = new[] { "Guest", "Staff", "Admin" };
        if (!allowed.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "Role must be Guest, Staff or Admin" });

        var jwtSection = _config.GetSection("Jwt");
        var key  = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, request.Name ?? request.Role),
            new Claim(ClaimTypes.Role, request.Role)
        };

        var token = new JwtSecurityToken(
            issuer:   jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims:   claims,
            expires:  DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return Ok(new TokenResponse(new JwtSecurityTokenHandler().WriteToken(token)));
    }
}

public sealed record TokenRequest(string Role, string? Name = null);
public sealed record TokenResponse(string Token);
