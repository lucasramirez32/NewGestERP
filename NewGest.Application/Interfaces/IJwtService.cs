using System.Security.Claims;
using NewGest.Domain.Entities.Users;

namespace NewGest.Application.Interfaces;

public interface IJwtService
{
    string GenerarToken(ApplicationUser user, IList<Claim> claims);
}
