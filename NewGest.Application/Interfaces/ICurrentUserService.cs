namespace NewGest.Application.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    int IdEmpresa { get; }
    string Nombre { get; }
    bool DebeResetearPassword { get; }
    IEnumerable<string> Permisos { get; }
}
