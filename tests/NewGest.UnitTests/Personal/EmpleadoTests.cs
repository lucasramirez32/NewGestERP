using FluentAssertions;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Neg;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Personal;

public class EmpleadoTests
{
    // ─── Crear ────────────────────────────────────────────────────────────────
    [Fact]
    public void Crear_con_CUIL_invalido_lanza_DomainException()
    {
        var act = () => Empleado.Crear(1, "EMP001", "García Juan", "20-12345678-9", RolEmpleado.Administrativo, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*CUIL inválido*");
    }

    [Fact]
    public void Crear_con_CUIL_nulo_es_valido()
    {
        var empleado = Empleado.Crear(1, "EMP001", "García Juan", null, RolEmpleado.Administrativo, null);

        empleado.Should().NotBeNull();
        empleado.CUIL.Should().BeNull();
        empleado.Activo.Should().BeTrue();
    }

    [Fact]
    public void Crear_con_CUIL_vacio_es_valido()
    {
        var empleado = Empleado.Crear(1, "EMP001", "García Juan", "", RolEmpleado.Administrativo, null);

        empleado.Should().NotBeNull();
        empleado.CUIL.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Crear_con_comision_fuera_de_rango_lanza_DomainException()
    {
        var act = () => Empleado.Crear(1, "V001", "López Ana", null, RolEmpleado.Vendedor, 150m);

        act.Should().Throw<DomainException>()
            .WithMessage("*porcentaje de comisión*");
    }

    [Fact]
    public void Crear_comision_negativa_lanza_DomainException()
    {
        var act = () => Empleado.Crear(1, "V001", "López Ana", null, RolEmpleado.Vendedor, -5m);

        act.Should().Throw<DomainException>()
            .WithMessage("*porcentaje de comisión*");
    }

    [Fact]
    public void Crear_rol_Vendedor_establece_EsVendedor_true()
    {
        var empleado = Empleado.Crear(1, "V001", "López Ana", null, RolEmpleado.Vendedor, 5m);

        empleado.EsVendedor.Should().BeTrue();
        empleado.ComisionPorcentaje.Should().Be(5m);
    }

    [Fact]
    public void Crear_rol_Administrativo_establece_EsVendedor_false()
    {
        var empleado = Empleado.Crear(1, "ADM001", "Pérez Carlos", null, RolEmpleado.Administrativo, null);

        empleado.EsVendedor.Should().BeFalse();
    }

    [Fact]
    public void Crear_normaliza_legajo_a_mayusculas()
    {
        var empleado = Empleado.Crear(1, "emp001", "Torres Mario", null, RolEmpleado.Deposito, null);

        empleado.Legajo.Should().Be("EMP001");
    }

    [Fact]
    public void Crear_quita_guiones_del_CUIL()
    {
        var empleado = Empleado.Crear(1, "EMP001", "Test", "20-12345678-6", RolEmpleado.Administrativo, null);

        empleado.CUIL.Should().Be("20123456786");
    }

    // ─── Actualizar ───────────────────────────────────────────────────────────
    [Fact]
    public void Actualizar_cambia_campos_editables()
    {
        var empleado = Empleado.Crear(1, "V001", "López Ana", null, RolEmpleado.Vendedor, 5m);

        empleado.Actualizar("López Ana María", "20123456786", RolEmpleado.Gerencia, null);

        empleado.ApellidoNombre.Should().Be("López Ana María");
        empleado.CUIL.Should().Be("20123456786");
        empleado.Rol.Should().Be(RolEmpleado.Gerencia);
        empleado.EsVendedor.Should().BeFalse();   // Gerencia no es vendedor
        empleado.ComisionPorcentaje.Should().BeNull();
    }

    [Fact]
    public void Actualizar_con_CUIL_invalido_lanza_DomainException()
    {
        var empleado = Empleado.Crear(1, "EMP001", "Test", null, RolEmpleado.Administrativo, null);

        var act = () => empleado.Actualizar("Test", "99-99999999-9", RolEmpleado.Administrativo, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*CUIL inválido*");
    }

    // ─── Desactivar ───────────────────────────────────────────────────────────
    [Fact]
    public void Desactivar_pone_Activo_en_false()
    {
        var empleado = Empleado.Crear(1, "EMP001", "Test", null, RolEmpleado.Administrativo, null);
        empleado.Activo.Should().BeTrue();

        empleado.Desactivar();

        empleado.Activo.Should().BeFalse();
    }
}
