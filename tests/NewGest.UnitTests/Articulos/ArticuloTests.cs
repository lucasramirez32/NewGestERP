using FluentAssertions;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Neg;

namespace NewGest.UnitTests.Articulos;

public class ArticuloTests
{
    // ─── Fábrica helper ───────────────────────────────────────────────────────
    private static Articulo CrearArticuloValido(
        decimal precioLista = 1000m,
        decimal precioCosto = 500m,
        decimal porcentajeIva = 21m)
        => Articulo.Crear(
            idEmpresa: 1,
            codigo: "ART001",
            descripcion: "Artículo de prueba",
            idGrupo: 1,
            idUnidad: 1,
            precioLista: precioLista,
            precioCosto: precioCosto,
            porcentajeIva: porcentajeIva,
            observaciones: null);

    // ─── Crear — precios ──────────────────────────────────────────────────────
    [Fact]
    public void Crear_con_precio_lista_negativo_lanza_DomainException()
    {
        var act = () => CrearArticuloValido(precioLista: -1m);

        act.Should().Throw<DomainException>()
            .WithMessage("*precio de lista*negativo*");
    }

    [Fact]
    public void Crear_con_precio_costo_negativo_lanza_DomainException()
    {
        var act = () => CrearArticuloValido(precioCosto: -0.01m);

        act.Should().Throw<DomainException>()
            .WithMessage("*precio de costo*negativo*");
    }

    [Fact]
    public void Crear_con_precio_cero_es_valido()
    {
        var articulo = CrearArticuloValido(precioLista: 0m, precioCosto: 0m);

        articulo.PrecioLista.Should().Be(0m);
        articulo.PrecioCosto.Should().Be(0m);
    }

    // ─── Crear — IVA ──────────────────────────────────────────────────────────
    [Fact]
    public void Crear_con_IVA_21_es_valido()
    {
        var articulo = CrearArticuloValido(porcentajeIva: 21m);

        articulo.PorcentajeIva.Should().Be(21m);
        articulo.Activo.Should().BeTrue();
    }

    [Fact]
    public void Crear_con_IVA_10_5_es_valido()
    {
        var articulo = CrearArticuloValido(porcentajeIva: 10.5m);

        articulo.PorcentajeIva.Should().Be(10.5m);
    }

    [Fact]
    public void Crear_con_IVA_0_es_valido()
    {
        var articulo = CrearArticuloValido(porcentajeIva: 0m);

        articulo.PorcentajeIva.Should().Be(0m);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(10)]
    [InlineData(27)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Crear_con_IVA_invalido_lanza_DomainException(decimal iva)
    {
        var act = () => CrearArticuloValido(porcentajeIva: iva);

        act.Should().Throw<DomainException>()
            .WithMessage("*IVA inválido*");
    }

    // ─── Crear — otras propiedades ────────────────────────────────────────────
    [Fact]
    public void Crear_normaliza_codigo_a_mayusculas()
    {
        var articulo = Articulo.Crear(1, "art001", "Descripción",
            1, 1, 100m, 50m, 21m, null);

        articulo.Codigo.Should().Be("ART001");
    }

    [Fact]
    public void Crear_trim_descripcion()
    {
        var articulo = Articulo.Crear(1, "ART001", "  Descripción con espacios  ",
            1, 1, 100m, 50m, 21m, null);

        articulo.Descripcion.Should().Be("Descripción con espacios");
    }

    [Fact]
    public void Crear_activo_por_defecto()
    {
        var articulo = CrearArticuloValido();

        articulo.Activo.Should().BeTrue();
    }

    // ─── Actualizar ───────────────────────────────────────────────────────────
    [Fact]
    public void Actualizar_cambia_campos_editables()
    {
        var articulo = CrearArticuloValido();

        articulo.Actualizar(
            descripcion: "Nueva Descripción",
            idGrupo: 2,
            idUnidad: 3,
            precioLista: 2000m,
            precioCosto: 800m,
            porcentajeIva: 10.5m,
            observaciones: "Nota actualizada");

        articulo.Descripcion.Should().Be("Nueva Descripción");
        articulo.IdGrupo.Should().Be(2);
        articulo.IdUnidad.Should().Be(3);
        articulo.PrecioLista.Should().Be(2000m);
        articulo.PrecioCosto.Should().Be(800m);
        articulo.PorcentajeIva.Should().Be(10.5m);
        articulo.Observaciones.Should().Be("Nota actualizada");
    }

    [Fact]
    public void Actualizar_con_precio_negativo_lanza_DomainException()
    {
        var articulo = CrearArticuloValido();

        var act = () => articulo.Actualizar("Desc", 1, 1, -5m, 0m, 21m, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*precio de lista*negativo*");
    }

    [Fact]
    public void Actualizar_con_IVA_invalido_lanza_DomainException()
    {
        var articulo = CrearArticuloValido();

        var act = () => articulo.Actualizar("Desc", 1, 1, 100m, 50m, 15m, null);

        act.Should().Throw<DomainException>()
            .WithMessage("*IVA inválido*");
    }

    [Fact]
    public void Crear_con_idGrupo_nulo_es_valido()
    {
        var articulo = Articulo.Crear(1, "ART001", "Descripción",
            null, 1, 100m, 50m, 21m, null);

        articulo.IdGrupo.Should().BeNull();
        articulo.Grupo.Should().BeNull();
    }

    [Fact]
    public void Actualizar_con_idGrupo_nulo_es_valido()
    {
        var articulo = CrearArticuloValido();
        articulo.IdGrupo.Should().Be(1);

        articulo.Actualizar("Descripción", null, 1, 100m, 50m, 21m, null);

        articulo.IdGrupo.Should().BeNull();
    }

    // ─── Desactivar ───────────────────────────────────────────────────────────
    [Fact]
    public void Desactivar_pone_Activo_en_false()
    {
        var articulo = CrearArticuloValido();
        articulo.Activo.Should().BeTrue();

        articulo.Desactivar();

        articulo.Activo.Should().BeFalse();
    }
}
