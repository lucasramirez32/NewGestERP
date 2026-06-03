using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using NewGest.Application.Common;
using NewGest.Application.Interfaces;

namespace NewGest.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public int UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            return int.TryParse(value, out var id) ? id : 0;
        }
    }

    public int IdEmpresa
    {
        get
        {
            var value = User?.FindFirstValue(NewgestClaims.Empresa);
            return int.TryParse(value, out var id) ? id : 0;
        }
    }

    public string Nombre => User?.FindFirstValue(NewgestClaims.Nombre) ?? string.Empty;

    public bool DebeResetearPassword
    {
        get
        {
            var value = User?.FindFirstValue(NewgestClaims.DebeResetear);
            return bool.TryParse(value, out var resultado) && resultado;
        }
    }

    public IEnumerable<string> Permisos
        => User?.FindAll(NewgestClaims.Permiso).Select(c => c.Value)
           ?? Enumerable.Empty<string>();

    // Declarar la constante localmente para evitar dep de System.IdentityModel.Tokens.Jwt en esta clase
    private static class JwtRegisteredClaimNames
    {
        public const string Sub = "sub";
    }
}
