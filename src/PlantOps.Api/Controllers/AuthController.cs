using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlantOps.Api.Contracts;
using PlantOps.Api.Data;
using PlantOps.Api.Models;
using PlantOps.Api.Options;

namespace PlantOps.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly PlantOpsDbContext _db;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly AuthOptions _authOptions;

    public AuthController(PlantOpsDbContext db, IPasswordHasher<AppUser> passwordHasher, IOptions<AuthOptions> authOptions)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _authOptions = authOptions.Value;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await _db.Users.SingleOrDefaultAsync(candidate => candidate.Username == request.Username, cancellationToken);
        if (user is null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = "Invalid username or password." });
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authOptions.JwtSigningKey));
        var token = new JwtSecurityToken(
            issuer: _authOptions.JwtIssuer,
            audience: _authOptions.JwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_authOptions.TokenHours),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), user.Username, user.Role));
    }
}
