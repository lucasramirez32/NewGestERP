using FluentAssertions;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Inv;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Stock;

public class MovimientoStockTests
{
    [Fact]
    public void Crear_con_cantidad_cero_lanza_DomainException()
    {
        var act = () => MovimientoStock.Crear(1, 1, 1, TipoMovimiento.Entrada, 0, 100);

        act.Should().Throw<DomainException>()
            .WithMessage("*cantidad*mayor a cero*");
    }

    [Fact]
    public void Crear_con_cantidad_negativa_lanza_DomainException()
    {
        var act = () => MovimientoStock.Crear(1, 1, 1, TipoMovimiento.Entrada, -5, 100);

        act.Should().Throw<DomainException>()
            .WithMessage("*cantidad*mayor a cero*");
    }

    [Fact]
    public void Crear_con_costo_negativo_lanza_DomainException()
    {
        var act = () => MovimientoStock.Crear(1, 1, 1, TipoMovimiento.Entrada, 10, -1);

        act.Should().Throw<DomainException>()
            .WithMessage("*costo unitario*negativo*");
    }

    [Fact]
    public void Crear_valido_retorna_movimiento_con_campos_correctos()
    {
        var mov = MovimientoStock.Crear(1, 5, 2, TipoMovimiento.Entrada, 15.5m, 200m, "SN-001", null, "Obs test");

        mov.Should().NotBeNull();
        mov.IdEmpresa.Should().Be(1);
        mov.IdArticulo.Should().Be(5);
        mov.IdDeposito.Should().Be(2);
        mov.Tipo.Should().Be(TipoMovimiento.Entrada);
        mov.Cantidad.Should().Be(15.5m);
        mov.CostoUnitario.Should().Be(200m);
        mov.NumeroSerie.Should().Be("SN-001");
        mov.Observaciones.Should().Be("Obs test");
        mov.FechaMovimiento.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Crear_salida_con_costo_cero_es_valido()
    {
        var mov = MovimientoStock.Crear(1, 1, 1, TipoMovimiento.Salida, 3m, 0);

        mov.Should().NotBeNull();
        mov.CostoUnitario.Should().Be(0);
    }
}
