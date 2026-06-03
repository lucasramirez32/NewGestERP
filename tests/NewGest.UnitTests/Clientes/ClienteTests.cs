using FluentAssertions;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Neg;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Clientes;

public class ClienteTests
{
    // ─── Fábrica helper ───────────────────────────────────────────────────────
    private static Cliente CrearClienteValido(string? cuit = null)
        => Cliente.Crear(
            idEmpresa: 1,
            codigo: "CLI001",
            razonSocial: "Empresa de Prueba SA",
            cuit: cuit,
            condicionIva: CondicionIva.Inscripto,
            domicilio: "Av. Corrientes 1234",
            localidad: "CABA",
            telefono: "011-4000-0000",
            email: "contacto@empresa.com",
            idZona: null,
            observaciones: null);

    // ─── Crear ────────────────────────────────────────────────────────────────
    [Fact]
    public void Crear_con_CUIT_valido_retorna_cliente()
    {
        var cliente = CrearClienteValido(cuit: "20-12345678-6");

        cliente.Should().NotBeNull();
        cliente.CUIT.Should().Be("20123456786"); // guardado sin guiones
        cliente.Activo.Should().BeTrue();
    }

    [Fact]
    public void Crear_con_CUIT_invalido_lanza_DomainException()
    {
        var act = () => CrearClienteValido(cuit: "20-12345678-9"); // DV incorrecto

        act.Should().Throw<DomainException>()
            .WithMessage("*CUIT inválido*");
    }

    [Fact]
    public void Crear_con_CUIT_vacio_es_valido()
    {
        var cliente = CrearClienteValido(cuit: "");

        cliente.Should().NotBeNull();
        cliente.CUIT.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Crear_con_CUIT_nulo_es_valido()
    {
        var cliente = CrearClienteValido(cuit: null);

        cliente.Should().NotBeNull();
        cliente.CUIT.Should().BeNull();
    }

    [Fact]
    public void Crear_normaliza_codigo_a_mayusculas()
    {
        var cliente = Cliente.Crear(
            1, "cli001", "Empresa Test", null,
            CondicionIva.Inscripto, null, null, null, null, null, null);

        cliente.Codigo.Should().Be("CLI001");
    }

    [Fact]
    public void Crear_trim_razon_social()
    {
        var cliente = Cliente.Crear(
            1, "CLI001", "  Empresa Test  ", null,
            CondicionIva.Inscripto, null, null, null, null, null, null);

        cliente.RazonSocial.Should().Be("Empresa Test");
    }

    [Fact]
    public void Crear_quita_guiones_del_CUIT()
    {
        var cliente = CrearClienteValido(cuit: "30-55667788-9");
        cliente.CUIT.Should().Be("30556677889");
    }

    // ─── Actualizar ───────────────────────────────────────────────────────────
    [Fact]
    public void Actualizar_cambia_los_campos_editables()
    {
        var cliente = CrearClienteValido();

        cliente.Actualizar(
            razonSocial: "Nueva Razón Social SRL",
            cuit: null,
            condicionIva: CondicionIva.Monotributo,
            domicilio: "Belgrano 500",
            localidad: "Córdoba",
            telefono: "0351-0000000",
            email: "nuevo@email.com",
            idZona: 3,
            observaciones: "Observación actualizada");

        cliente.RazonSocial.Should().Be("Nueva Razón Social SRL");
        cliente.CondicionIva.Should().Be(CondicionIva.Monotributo);
        cliente.Domicilio.Should().Be("Belgrano 500");
        cliente.Localidad.Should().Be("Córdoba");
        cliente.Telefono.Should().Be("0351-0000000");
        cliente.Email.Should().Be("nuevo@email.com");
        cliente.IdZona.Should().Be(3);
        cliente.Observaciones.Should().Be("Observación actualizada");
    }

    [Fact]
    public void Actualizar_con_CUIT_invalido_lanza_DomainException()
    {
        var cliente = CrearClienteValido();

        var act = () => cliente.Actualizar(
            "Empresa", "99-99999999-9", CondicionIva.Inscripto,
            null, null, null, null, null, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*CUIT inválido*");
    }

    // ─── Desactivar ───────────────────────────────────────────────────────────
    [Fact]
    public void Desactivar_pone_Activo_en_false()
    {
        var cliente = CrearClienteValido();
        cliente.Activo.Should().BeTrue();

        cliente.Desactivar();

        cliente.Activo.Should().BeFalse();
    }

    [Fact]
    public void Activar_pone_Activo_en_true()
    {
        var cliente = CrearClienteValido();
        cliente.Desactivar();
        cliente.Activo.Should().BeFalse();

        cliente.Activar();

        cliente.Activo.Should().BeTrue();
    }
}
