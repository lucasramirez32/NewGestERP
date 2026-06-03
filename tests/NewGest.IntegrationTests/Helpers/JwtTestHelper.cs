using System.IdentityModel.Tokens.Jwt;

namespace NewGest.IntegrationTests.Helpers;

/// <summary>
/// Utilidad para decodificar tokens JWT en tests de integración
/// sin verificar firma (el token ya fue validado por la API).
/// </summary>
public static class JwtTestHelper
{
    public static IDictionary<string, string> ObtenerClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims.ToDictionary(c => c.Type, c => c.Value);
    }

    public static string? ObtenerClaim(string token, string tipo)
    {
        var claims = ObtenerClaims(token);
        claims.TryGetValue(tipo, out var valor);
        return valor;
    }
}
