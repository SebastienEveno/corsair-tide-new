using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CorsairTide.Server.Domain.Common;
using CorsairTide.Server.Domain.Players;
using Microsoft.IdentityModel.Tokens;

namespace CorsairTide.Server.Application.Auth;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, Guid PlayerId);

public class AuthService(IPlayerRepository playerRepository, IConfiguration configuration)
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new DomainException("Username and password are required.");

        var player = await playerRepository.GetByUsernameAsync(request.Username, ct)
            ?? throw new DomainException("Invalid username or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, player.PasswordHash))
            throw new DomainException("Invalid username or password.");

        var token = GenerateToken(player);
        return new LoginResponse(token, player.Username, player.Id.Value);
    }

    private string GenerateToken(Player player)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, player.Id.Value.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, player.Username),
            new Claim(JwtRegisteredClaimNames.Email, player.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:   configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims:   claims,
            expires:  DateTime.UtcNow.AddHours(
                          double.Parse(configuration["Jwt:ExpiryHours"] ?? "24")),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
