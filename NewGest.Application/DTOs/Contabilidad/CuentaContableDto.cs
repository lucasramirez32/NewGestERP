namespace NewGest.Application.DTOs.Contabilidad;

public record CuentaContableDto(
    int IdCuenta,
    string Codigo,
    string Descripcion,
    int? IdCuentaPadre,
    string CodigoPadre,
    string Naturaleza,
    string Tipo,
    bool ImputaDirectamente,
    bool Activa
);

public record CrearCuentaContableDto(
    string Codigo,
    string Descripcion,
    int? IdCuentaPadre,
    int Naturaleza,
    int Tipo,
    bool ImputaDirectamente
);
