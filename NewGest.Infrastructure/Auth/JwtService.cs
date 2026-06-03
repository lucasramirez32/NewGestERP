using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NewGest.Application.Common;
using NewGest.Application.Interfaces;
using NewGest.Domain.Entities.Users;

namespace NewGest.Infrastructure.Auth;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerarToken(ApplicationUser user, IList<Claim> claims)
    {
        var tokenClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(NewgestClaims.Empresa, user.IdEmpresa.ToString()),
            new(NewgestClaims.Nombre, user.Nombre),
            new(NewgestClaims.DebeResetear, user.DebeResetearPassword.ToString().ToLower()),
        };

        // Agregar claims de permisos del usuario (tipo NewgestClaims.Permiso)
        tokenClaims.AddRange(claims);

        var secretKey = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret no configurado.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: tokenClaims,
            expires: DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpiryMinutes", 480)),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
