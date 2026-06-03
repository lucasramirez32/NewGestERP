using FluentAssertions;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Inv;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Stock;

/// <summary>
/// Tests de lógica de cálculo de stock que no dependen de EF Core.
/// La verificación de persistencia va en los tests de integración.
/// </summary>
public class StockServiceLogicTests
{
    // ─── Costo promedio ponderado ─────────────────────────────────────────────
    [Fact]
    public void CostoPromedio_primera_entrada_queda_igual_al_costo_unitario()
    {
        // Sin existencia previa: costo promedio = costo de la primera entrada
        const decimal costoEntrada = 500m;
        const decimal cantidadEntrada = 10m;

        // Simulación del cálculo del service (cantidadAnterior <= 0)
        var cantidadAnterior = 0m;
        var costoPromedio = 0m;
        var costoResultante = cantidadAnterior <= 0
            ? costoEntrada
            : ((cantidadAnterior * costoPromedio) + (cantidadEntrada * costoEntrada))
              / (cantidadAnterior + cantidadEntrada);

        costoResultante.Should().Be(costoEntrada);
    }

    [Fact]
    public void CostoPromedio_segunda_entrada_es_ponderado()
    {
        // Stock previo: 10 unidades a $500 = $5000
        // Nueva entrada: 5 unidades a $800 = $4000
        // Total: 15 unidades, valor $9000 → costo promedio $600
        const decimal cantidadAnterior = 10m;
        const decimal costoAnterior = 500m;
        const decimal cantidadNueva = 5m;
        const decimal costoNueva = 800m;

        var nuevaCantidad = cantidadAnterior + cantidadNueva;
        var costoResultante = ((cantidadAnterior * costoAnterior) + (cantidadNueva * costoNueva)) / nuevaCantidad;

        costoResultante.Should().Be(600m);
    }

    // ─── Delta por tipo de movimiento ─────────────────────────────────────────
    [Theory]
    [InlineData(TipoMovimiento.Entrada, 10, 10, 0)]       // existencia era 0, entra 10 → queda 10
    [InlineData(TipoMovimiento.Salida, 5, -5, 0)]          // sale 5 → delta -5
    [InlineData(TipoMovimiento.Transferencia, 3, -3, 0)]   // transferencia origen → delta -3
    public void Delta_por_tipo_es_correcto(TipoMovimiento tipo, decimal cantidad, decimal deltaEsperado, decimal cantidadPrevia)
    {
        var existencia = new ExistenciaDeposito { Cantidad = cantidadPrevia };

        var delta = tipo switch
        {
            TipoMovimiento.Entrada => cantidad,
            TipoMovimiento.Salida => -cantidad,
            TipoMovimiento.Ajuste => cantidad - existencia.Cantidad,
            TipoMovimiento.Transferencia => -cantidad,
            _ => throw new DomainException("Tipo desconocido")
        };

        delta.Should().Be(deltaEsperado);
    }

    [Fact]
    public void Ajuste_establece_cantidad_absoluta()
    {
        var existencia = new ExistenciaDeposito { Cantidad = 30m };
        const decimal cantidadAjuste = 12m;

        var delta = cantidadAjuste - existencia.Cantidad; // = -18
        existencia.Cantidad += delta;

        existencia.Cantidad.Should().Be(cantidadAjuste);
    }
}
