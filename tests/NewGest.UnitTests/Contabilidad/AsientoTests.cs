using FluentAssertions;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Cnt;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Contabilidad;

public class AsientoTests
{
    private static Asiento AsientoVacio() =>
        Asiento.Crear(1, 1, new DateOnly(2026, 6, 5), "Test asiento", TipoAsiento.Manual);

    // ─── Validar doble partida ────────────────────────────────────────────────
    [Fact]
    public void Validar_asiento_equilibrado_no_lanza()
    {
        var a = AsientoVacio();
        a.AgregarPartida(10, debe: 1210m, haber: 0, "Cuentas a cobrar");
        a.AgregarPartida(20, debe: 0, haber: 1000m, "Ventas");
        a.AgregarPartida(30, debe: 0, haber: 210m,  "IVA Ventas");

        var act = () => a.Validar();

        act.Should().NotThrow();
        a.TotalDebe.Should().Be(1210m);
        a.TotalHaber.Should().Be(1210m);
    }

    [Fact]
    public void Validar_asiento_desequilibrado_lanza_DomainException()
    {
        var a = AsientoVacio();
        a.AgregarPartida(10, debe: 1210m, haber: 0);
        a.AgregarPartida(20, debe: 0,     haber: 1000m);
        // Falta partida IVA → desbalanceado

        var act = () => a.Validar();

        act.Should().Throw<DomainException>()
            .WithMessage("*desequilibrado*");
    }

    [Fact]
    public void Validar_asiento_con_menos_de_2_partidas_lanza_DomainException()
    {
        var a = AsientoVacio();
        a.AgregarPartida(10, debe: 100m, haber: 0);

        var act = () => a.Validar();

        act.Should().Throw<DomainException>()
            .WithMessage("*al menos 2 partidas*");
    }

    // ─── Asiento FC-B $1210 ───────────────────────────────────────────────────
    [Fact]
    public void Asiento_FacturaB_1210_Debe_CxC_Haber_Ventas_IVA_cuadra()
    {
        var a = AsientoVacio();
        a.AgregarPartida(idCuenta: 11, debe: 1210m, haber: 0,    "Cuentas a cobrar");
        a.AgregarPartida(idCuenta: 41, debe: 0,     haber: 1000m, "Ventas");
        a.AgregarPartida(idCuenta: 24, debe: 0,     haber: 210m,  "IVA Ventas 21%");

        a.Validar();

        a.Partidas.Should().HaveCount(3);
        a.Partidas.First(p => p.IdCuenta == 11).Debe.Should().Be(1210m);
        a.Partidas.First(p => p.IdCuenta == 41).Haber.Should().Be(1000m);
        a.Partidas.First(p => p.IdCuenta == 24).Haber.Should().Be(210m);
    }

    // ─── PartidaAsiento — validaciones ───────────────────────────────────────
    [Fact]
    public void Partida_debe_y_haber_cero_lanza_DomainException()
    {
        var act = () => PartidaAsiento.Crear(10, 0m, 0m);
        act.Should().Throw<DomainException>().WithMessage("*cero*");
    }

    [Fact]
    public void Partida_debe_negativo_lanza_DomainException()
    {
        var act = () => PartidaAsiento.Crear(10, -50m, 0m);
        act.Should().Throw<DomainException>().WithMessage("*negativo*");
    }

    [Fact]
    public void Partida_haber_negativo_lanza_DomainException()
    {
        var act = () => PartidaAsiento.Crear(10, 0m, -50m);
        act.Should().Throw<DomainException>().WithMessage("*negativo*");
    }

    // ─── Anular ───────────────────────────────────────────────────────────────
    [Fact]
    public void Anular_asiento_manual_cambia_flag()
    {
        var a = AsientoVacio();
        a.AgregarPartida(10, 100m, 0);
        a.AgregarPartida(20, 0, 100m);
        a.Anular();

        a.Anulado.Should().BeTrue();
    }

    // Bug #1: verificar que TODOS los tipos no-manual no se pueden anular
    [Theory]
    [InlineData(TipoAsiento.AutoFactura)]
    [InlineData(TipoAsiento.AutoPago)]
    [InlineData(TipoAsiento.AutoAjuste)]
    public void Anular_asiento_no_manual_lanza_DomainException(TipoAsiento tipo)
    {
        var a = Asiento.Crear(1, 1, new DateOnly(2026, 6, 5), "Auto", tipo);
        a.AgregarPartida(10, 100m, 0);
        a.AgregarPartida(20, 0, 100m);

        var act = () => a.Anular();

        act.Should().Throw<DomainException>().WithMessage("*manuales*");
    }

    // ─── CuentaContable ───────────────────────────────────────────────────────
    [Fact]
    public void CuentaContable_codigo_vacio_lanza_DomainException()
    {
        var act = () => CuentaContable.Crear(1, "", "Ventas",
            null, NaturalezaCuenta.Acreedora, TipoCuenta.Resultado, true);

        act.Should().Throw<DomainException>().WithMessage("*código*");
    }

    [Fact]
    public void CuentaContable_descripcion_vacia_lanza_DomainException()
    {
        var act = () => CuentaContable.Crear(1, "4.1", "",
            null, NaturalezaCuenta.Acreedora, TipoCuenta.Resultado, true);

        act.Should().Throw<DomainException>().WithMessage("*descripción*");
    }

    [Fact]
    public void CuentaContable_valida_crea_correctamente()
    {
        var cuenta = CuentaContable.Crear(1, "1.1.01", "Caja Pesos",
            idCuentaPadre: 1, NaturalezaCuenta.Deudora, TipoCuenta.Activo, imputaDirectamente: true);

        cuenta.Codigo.Should().Be("1.1.01");
        cuenta.Activa.Should().BeTrue();
        cuenta.ImputaDirectamente.Should().BeTrue();
    }
}
