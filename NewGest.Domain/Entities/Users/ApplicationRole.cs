using Microsoft.AspNetCore.Identity;

namespace NewGest.Domain.Entities.Users;

public class ApplicationRole : IdentityRole<int>
{
    public int IdEmpresa { get; set; }
}
