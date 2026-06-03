using Microsoft.AspNetCore.Identity;

namespace NewGest.Domain.Entities.Users;

public class ApplicationUser : IdentityUser<int>
{
    public int IdEmpresa { get; set; }
    public string Nombre { get; set; } = default!;
    public string? Legajo { get; set; }
    public bool DebeResetearPassword { get; set; } = true;
    public DateTime? UltimoLogin { get; set; }
    public int IntentosFallidos { get; set; }
    public bool Bloqueado { get; set; }
}
