using FluentAssertions;
using NewGest.Application.DTOs.Comprobantes;
using NewGest.Application.Features.Comprobantes.Commands.EmitirFactura;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Facturacion;

public class EmitirFacturaValidatorTests
{
    private readonly EmitirFacturaCommandValidator _validator = new();

    private static EmitirFacturaCommand CommandValido(DateOnly? fecha = null) => new(
        IdEmpresa: 1,
        Tipo: TipoComprobante.FacturaB,
        PuntoVenta: 1,
        Fecha: fecha ?? DateOnly.FromDateTime(DateTime.Today),
        IdCliente: 10,
        IdPedidoOrigen: null,
        Items: [new ItemFacturaDto(1, "Producto", 2m, 100m, AlicuotaIva.Porcentaje21)]
    );

    [Fact]
    public void Comando_valido_pasa_sin_errores()
    {
        var result = _validator.Validate(CommandValido());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IdEmpresa_cero_falla_validacion()
    {
        var cmd = CommandValido() with { IdEmpresa = 0 };
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdEmpresa");
    }

    [Fact]
    public void PuntoVenta_cero_falla_validacion()
    {
        var cmd = CommandValido() with { PuntoVenta = 0 };
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void PuntoVenta_10000_falla_validacion()
    {
        var cmd = CommandValido() with { PuntoVenta = 10000 };
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Items_vacios_falla_validacion()
    {
        var cmd = CommandValido() with { Items = [] };
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("al menos un ítem"));
    }

    [Fact]
    public void Item_descripcion_vacia_falla_validacion()
    {
        var cmd = CommandValido() with
        {
            Items = [new ItemFacturaDto(1, "", 2m, 100m, AlicuotaIva.Porcentaje21)]
        };
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("descripción"));
    }

    [Fact]
    public void Item_cantidad_cero_falla_validacion()
    {
        var cmd = CommandValido() with
        {
            Items = [new ItemFacturaDto(1, "Prod", 0m, 100m, AlicuotaIva.Porcentaje21)]
        };
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Fecha_mas_de_5_dias_futura_falla_validacion()
    {
        var cmd = CommandValido(DateOnly.FromDateTime(DateTime.Today.AddDays(6)));
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("5 días"));
    }

    [Fact]
    public void Fecha_5_dias_futura_pasa_validacion()
    {
        var cmd = CommandValido(DateOnly.FromDateTime(DateTime.Today.AddDays(5)));
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Fecha_pasada_pasa_validacion()
    {
        var cmd = CommandValido(DateOnly.FromDateTime(DateTime.Today.AddDays(-30)));
        var result = _validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }
}
