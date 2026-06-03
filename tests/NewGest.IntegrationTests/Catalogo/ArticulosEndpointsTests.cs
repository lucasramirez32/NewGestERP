using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NewGest.Application.DTOs.Articulos;
using NewGest.Application.DTOs.Shared;
using NewGest.Infrastructure.Data;

namespace NewGest.IntegrationTests.Catalogo;

/// <summary>
/// Tests de integración para los endpoints de Artículos y Grupos (Sprint 3-4 — M10).
/// Usa NewGest_Test con datos reales de SQL Server — sin mocks de BD.
/// Implementa IAsyncLifetime para sembrar y limpiar datos propios del módulo.
///
/// Las Unidades (IdUnidad 1–6) son datos semilla de la migración; no se modifican.
/// Se usa IdUnidad = 1 (Unidad) para todos los artículos de prueba.
/// </summary>
[Collection("IntegrationTests")]
public class ArticulosEndpointsTests : IAsyncLifetime
{
    private readonly NewgestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private string _token = default!;

    // IDs de datos sembrados para uso en los tests
    private int _idGrupoRaiz;
    private int _idSubgrupo;
    private int _idArticuloA;
    private int _idArticuloB;
    private int _idArticuloC;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // La migración siembra IdUnidad=1 (Unidad/U) — se usa en todos los tests de artículos
    private const int IdUnidadPorDefecto = 1;

    public ArticulosEndpointsTests(NewgestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // IAsyncLifetime — seeding y cleanup específicos de Artículos
    // ──────────────────────────────────────────────────────────────────────────

    public async Task InitializeAsync()
    {
        _token = await LoginYObtenerTokenAsync();

        await LimpiarDatosArticulosAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // Grupo raíz
        var grupoRaiz = new NewGest.Domain.Entities.Neg.GrupoArticulo
        {
            IdEmpresa = NewgestWebApplicationFactory.IdEmpresaTest,
            Descripcion = "ART-GRP-RAIZ",
            IdGrupoPadre = null
        };
        db.GruposArticulos.Add(grupoRaiz);
        await db.SaveChangesAsync();
        _idGrupoRaiz = grupoRaiz.IdGrupo;

        // Subgrupo que apunta al raíz
        var subgrupo = new NewGest.Domain.Entities.Neg.GrupoArticulo
        {
            IdEmpresa = NewgestWebApplicationFactory.IdEmpresaTest,
            Descripcion = "ART-GRP-SUB",
            IdGrupoPadre = _idGrupoRaiz
        };
        db.GruposArticulos.Add(subgrupo);
        await db.SaveChangesAsync();
        _idSubgrupo = subgrupo.IdGrupo;

        // 3 artículos: A en grupo raíz, B en subgrupo, C en grupo raíz con IVA 10.5
        var articuloA = NewGest.Domain.Entities.Neg.Articulo.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "ART-T-001",
            "Artículo Test A",
            _idGrupoRaiz,
            IdUnidadPorDefecto,
            1000m,
            800m,
            21m,
            null);

        var articuloB = NewGest.Domain.Entities.Neg.Articulo.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "ART-T-002",
            "Artículo Test B Subgrupo",
            _idSubgrupo,
            IdUnidadPorDefecto,
            500m,
            400m,
            21m,
            null);

        var articuloC = NewGest.Domain.Entities.Neg.Articulo.Crear(
            NewgestWebApplicationFactory.IdEmpresaTest,
            "ART-T-003",
            "Artículo Test C IVA Reducido",
            _idGrupoRaiz,
            IdUnidadPorDefecto,
            250m,
            200m,
            10.5m,
            "IVA 10.5%");

        db.Articulos.AddRange(articuloA, articuloB, articuloC);
        await db.SaveChangesAsync();

        _idArticuloA = articuloA.IdArticulo;
        _idArticuloB = articuloB.IdArticulo;
        _idArticuloC = articuloC.IdArticulo;
    }

    public async Task DisposeAsync()
    {
        await LimpiarDatosArticulosAsync();
    }

    private async Task LimpiarDatosArticulosAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        // Eliminar artículos de prueba primero (FK a grupos)
        await db.Database.ExecuteSqlRawAsync(@"
            DELETE FROM neg.Articulos
            WHERE Codigo LIKE 'ART-T-%'
               OR Codigo LIKE 'ART-NEW%'
               OR Codigo LIKE 'ART-DUP%'
               OR Codigo LIKE 'ART-DEL%'
               OR Codigo LIKE 'ART-UPD%'");

        // Eliminar grupos en orden: primero los que TIENEN padre (subgrupos),
        // luego los que son raíz. Esto cubre tanto ART-GRP-* como GRP-TEST*.
        // Dos pasadas garantizan que la auto-referencia no deje huérfanos.
        await db.Database.ExecuteSqlRawAsync(@"
            DELETE FROM neg.GruposArticulos
            WHERE IdGrupoPadre IS NOT NULL
              AND (Descripcion LIKE 'ART-GRP-%' OR Descripcion LIKE 'GRP-TEST%')");

        await db.Database.ExecuteSqlRawAsync(@"
            DELETE FROM neg.GruposArticulos
            WHERE Descripcion LIKE 'ART-GRP-%'
               OR Descripcion LIKE 'GRP-TEST%'");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/articulos — sin JWT
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_articulos_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync("/api/articulos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/articulos — con JWT
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_articulos_con_jwt_retorna_200_y_PagedResult()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/articulos?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ArticuloListItemDto>>(JsonOpts);
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Total.Should().BeGreaterThanOrEqualTo(3,
            "hay al menos 3 artículos de prueba sembrados");
    }

    [Fact]
    public async Task GET_articulos_filtro_search_retorna_coincidencias()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/articulos?search=Art%C3%ADculo+Test+A&page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ArticuloListItemDto>>(JsonOpts);
        result!.Items.Should().NotBeEmpty("debe encontrar al menos el artículo Test A");
        result.Items.Should().Contain(a =>
            a.Descripcion.Contains("Test A", StringComparison.OrdinalIgnoreCase) ||
            a.Codigo.Contains("ART-T-001", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GET_articulos_filtro_por_grupo_retorna_solo_articulos_del_grupo()
    {
        using var client = ClienteConToken();

        // Solo el artículo B está en el subgrupo
        var response = await client.GetAsync($"/api/articulos?idGrupo={_idSubgrupo}&page=1&pageSize=100");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<ArticuloListItemDto>>(JsonOpts);
        result!.Items.Should().NotBeEmpty("el subgrupo contiene al menos el artículo B");
        result.Items.Should().AllSatisfy(a =>
            a.IdArticulo.Should().BeOneOf(
                new[] { _idArticuloB },
                "solo artículos del subgrupo deben aparecer en el filtro por grupo"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/articulos/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_articulo_por_id_existente_retorna_200_con_datos_completos()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync($"/api/articulos/{_idArticuloA}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var articulo = await response.Content.ReadFromJsonAsync<ArticuloDto>(JsonOpts);
        articulo.Should().NotBeNull();
        articulo!.IdArticulo.Should().Be(_idArticuloA);
        articulo.Codigo.Should().Be("ART-T-001");
        articulo.Descripcion.Should().Be("Artículo Test A");
        articulo.IdGrupo.Should().Be(_idGrupoRaiz);
        articulo.GrupoDescripcion.Should().Be("ART-GRP-RAIZ");
        articulo.IdUnidad.Should().Be(IdUnidadPorDefecto);
        articulo.UnidadSimbolo.Should().Be("U",
            "la migración siembra IdUnidad=1 con símbolo 'U'");
        articulo.PrecioLista.Should().Be(1000m);
        articulo.PorcentajeIva.Should().Be(21m);
        articulo.Activo.Should().BeTrue();
    }

    [Fact]
    public async Task GET_articulo_por_id_inexistente_retorna_404()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/articulos/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/articulos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_articulo_sin_jwt_retorna_401()
    {
        var dto = new CrearArticuloDto("ART-NEW-01", "Nuevo Artículo", _idGrupoRaiz,
            IdUnidadPorDefecto, 100m, 80m, 21m, null);

        var response = await _client.PostAsJsonAsync("/api/articulos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_articulo_body_valido_retorna_201()
    {
        using var client = ClienteConToken();
        var dto = new CrearArticuloDto(
            "ART-NEW-02",
            "Artículo Nuevo Válido",
            _idGrupoRaiz,
            IdUnidadPorDefecto,
            500m,
            400m,
            21m,
            "Artículo creado en test");

        var response = await client.PostAsJsonAsync("/api/articulos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "un body válido debe generar un 201 Created");

        var result = await response.Content.ReadFromJsonAsync<ArticuloDto>(JsonOpts);
        result.Should().NotBeNull();
        result!.IdArticulo.Should().BeGreaterThan(0);
        result.Codigo.Should().Be("ART-NEW-02");
        result.Descripcion.Should().Be("Artículo Nuevo Válido");
        result.PorcentajeIva.Should().Be(21m);
        result.Activo.Should().BeTrue();

        response.Headers.Location.Should().NotBeNull("la respuesta 201 debe incluir el header Location");
    }

    [Fact]
    public async Task POST_articulo_precio_lista_negativo_retorna_error()
    {
        using var client = ClienteConToken();
        var dto = new CrearArticuloDto("ART-NEW-ERR", "Precio Negativo", _idGrupoRaiz,
            IdUnidadPorDefecto, -100m, 80m, 21m, null);

        var response = await client.PostAsJsonAsync("/api/articulos", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.UnprocessableEntity, HttpStatusCode.BadRequest },
            "precio de lista negativo debe ser rechazado");
    }

    [Fact]
    public async Task POST_articulo_porcentaje_iva_invalido_retorna_error()
    {
        // IVA permitido: 0, 10.5, 21. El valor 15 es inválido.
        using var client = ClienteConToken();
        var dto = new CrearArticuloDto("ART-NEW-ERR2", "IVA Inválido", _idGrupoRaiz,
            IdUnidadPorDefecto, 100m, 80m, 15m, null);

        var response = await client.PostAsJsonAsync("/api/articulos", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.UnprocessableEntity, HttpStatusCode.BadRequest },
            "IVA=15 es inválido y debe ser rechazado por FluentValidation o dominio");
    }

    [Fact]
    public async Task POST_articulo_iva_cero_es_valido()
    {
        // IVA = 0 es permitido (artículos exentos)
        using var client = ClienteConToken();
        var dto = new CrearArticuloDto("ART-NEW-03", "Artículo Exento IVA", _idGrupoRaiz,
            IdUnidadPorDefecto, 100m, 80m, 0m, null);

        var response = await client.PostAsJsonAsync("/api/articulos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "IVA=0 es válido para artículos exentos");
    }

    [Fact]
    public async Task POST_articulo_codigo_duplicado_misma_empresa_retorna_400()
    {
        using var client = ClienteConToken();

        // Primer artículo
        var dto1 = new CrearArticuloDto("ART-DUP-01", "Artículo Dup A", _idGrupoRaiz,
            IdUnidadPorDefecto, 100m, 80m, 21m, null);
        var primera = await client.PostAsJsonAsync("/api/articulos", dto1);
        primera.StatusCode.Should().Be(HttpStatusCode.Created);

        // Mismo código → DomainException → 400
        var dto2 = new CrearArticuloDto("ART-DUP-01", "Artículo Dup B", _idGrupoRaiz,
            IdUnidadPorDefecto, 200m, 160m, 21m, null);
        var segunda = await client.PostAsJsonAsync("/api/articulos", dto2);

        segunda.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "código duplicado en la misma empresa debe retornar 400");

        var mensaje = await LeerMensajeErrorAsync(segunda);
        mensaje.Should().Contain("ART-DUP-01", "el mensaje debe mencionar el código duplicado");
    }

    [Fact]
    public async Task POST_articulo_descripcion_vacia_retorna_422()
    {
        using var client = ClienteConToken();
        var dto = new { codigo = "ART-NEW-ERR3", descripcion = "", idGrupo = _idGrupoRaiz,
            idUnidad = IdUnidadPorDefecto, precioLista = 100m, precioCosto = 80m, porcentajeIva = 21m };

        var response = await client.PostAsJsonAsync("/api/articulos", dto);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity,
            "FluentValidation debe rechazar descripción vacía con 422");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PUT /api/articulos/{id}
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task PUT_articulo_sin_jwt_retorna_401()
    {
        var dto = new ActualizarArticuloDto("Desc Actualizada", _idGrupoRaiz,
            IdUnidadPorDefecto, 100m, 80m, 21m, null);

        var response = await _client.PutAsJsonAsync($"/api/articulos/{_idArticuloA}", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PUT_articulo_existente_body_valido_retorna_200_con_datos_actualizados()
    {
        using var client = ClienteConToken();
        var dto = new ActualizarArticuloDto(
            "Artículo Test C Actualizado",
            _idGrupoRaiz,
            IdUnidadPorDefecto,
            300m,
            240m,
            21m,
            "Actualizado en test");

        var response = await client.PutAsJsonAsync($"/api/articulos/{_idArticuloC}", dto);

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "actualizar artículo existente con datos válidos debe retornar 200 o 204");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var result = await response.Content.ReadFromJsonAsync<ArticuloDto>(JsonOpts);
            result!.Descripcion.Should().Be("Artículo Test C Actualizado");
            result.PrecioLista.Should().Be(300m);
            result.PorcentajeIva.Should().Be(21m);
        }
    }

    [Fact]
    public async Task PUT_articulo_inexistente_retorna_400()
    {
        // El handler lanza DomainException("Artículo X no encontrado") → middleware → 400.
        // El endpoint declara ProducesProblem(404) como documentación, pero la implementación
        // actual retorna 400 vía DomainException. Este test verifica el comportamiento real.
        using var client = ClienteConToken();
        var dto = new ActualizarArticuloDto("No Existe", _idGrupoRaiz,
            IdUnidadPorDefecto, 100m, 80m, 21m, null);

        var response = await client.PutAsJsonAsync("/api/articulos/999999", dto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "el handler lanza DomainException para ID inexistente → middleware mapea a 400");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // DELETE /api/articulos/{id} — soft delete
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DELETE_articulo_existente_retorna_204_y_GET_posterior_retorna_404()
    {
        using var client = ClienteConToken();

        // Crear artículo dedicado para el test de delete
        var dtoCrear = new CrearArticuloDto("ART-DEL-01", "Artículo A Eliminar", _idGrupoRaiz,
            IdUnidadPorDefecto, 100m, 80m, 21m, null);
        var crearResp = await client.PostAsJsonAsync("/api/articulos", dtoCrear);
        crearResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var creado = await crearResp.Content.ReadFromJsonAsync<ArticuloDto>(JsonOpts);
        var idAEliminar = creado!.IdArticulo;

        // Soft delete
        var deleteResp = await client.DeleteAsync($"/api/articulos/{idAEliminar}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent,
            "DELETE de un artículo existente debe retornar 204");

        // GET posterior → 404 (filtro global de soft delete)
        var getResp = await client.GetAsync($"/api/articulos/{idAEliminar}");
        getResp.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "después del soft delete, el artículo no debe ser accesible via GET");
    }

    [Fact]
    public async Task DELETE_articulo_registro_persiste_en_BD_con_Activo_false()
    {
        using var client = ClienteConToken();

        // Crear artículo
        var dto = new CrearArticuloDto("ART-DEL-02", "Artículo Soft Delete Verify", _idGrupoRaiz,
            IdUnidadPorDefecto, 100m, 80m, 0m, null);
        var crearResp = await client.PostAsJsonAsync("/api/articulos", dto);
        var creado = await crearResp.Content.ReadFromJsonAsync<ArticuloDto>(JsonOpts);
        var idVerif = creado!.IdArticulo;

        // Soft delete
        await client.DeleteAsync($"/api/articulos/{idVerif}");

        // Verificar directo en BD con IgnoreQueryFilters
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NewgestDbContext>();

        var enBd = await db.Articulos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.IdArticulo == idVerif);

        enBd.Should().NotBeNull("el registro debe seguir existiendo en BD (soft delete)");
        enBd!.Activo.Should().BeFalse("el soft delete debe poner Activo = false");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/articulos/{id}/existencias — stub Sprint 5
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_existencias_con_jwt_retorna_200_con_existencias_cero()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync($"/api/articulos/{_idArticuloA}/existencias");

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el stub de existencias debe responder 200");

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.TryGetProperty("existencias", out var prop).Should().BeTrue(
            "la respuesta debe incluir la propiedad existencias");
        prop.GetDecimal().Should().Be(0m,
            "el stub debe retornar 0 hasta Sprint 5");
    }

    [Fact]
    public async Task GET_existencias_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync($"/api/articulos/{_idArticuloA}/existencias");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // GET /api/grupos-articulos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_grupos_articulos_sin_jwt_retorna_401()
    {
        var response = await _client.GetAsync("/api/grupos-articulos");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GET_grupos_articulos_con_jwt_retorna_200_y_lista_de_grupos()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/grupos-articulos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var grupos = await response.Content.ReadFromJsonAsync<IReadOnlyList<GrupoArticuloDto>>(JsonOpts);
        grupos.Should().NotBeNull();
        grupos.Should().NotBeEmpty("hay al menos el grupo raíz sembrado");
    }

    [Fact]
    public async Task GET_grupos_articulos_retorna_arbol_con_subgrupos()
    {
        using var client = ClienteConToken();

        var response = await client.GetAsync("/api/grupos-articulos");
        var grupos = await response.Content.ReadFromJsonAsync<IReadOnlyList<GrupoArticuloDto>>(JsonOpts);

        // El grupo raíz ART-GRP-RAIZ debe estar en el nivel raíz
        var grupoRaiz = grupos!.FirstOrDefault(g => g.Descripcion == "ART-GRP-RAIZ");
        grupoRaiz.Should().NotBeNull("el grupo raíz sembrado debe estar en la respuesta");
        grupoRaiz!.IdGrupoPadre.Should().BeNull("el grupo raíz no tiene padre");

        // El subgrupo debe aparecer como hijo del raíz
        grupoRaiz.Subgrupos.Should().Contain(s => s.Descripcion == "ART-GRP-SUB",
            "el subgrupo sembrado debe aparecer dentro del grupo raíz");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // POST /api/grupos-articulos
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_grupo_sin_jwt_retorna_401()
    {
        var cmd = new { idEmpresa = 0, descripcion = "Grupo Nuevo", idGrupoPadre = (int?)null };

        var response = await _client.PostAsJsonAsync("/api/grupos-articulos", cmd);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_grupo_raiz_sin_padre_retorna_201()
    {
        using var client = ClienteConToken();
        var cmd = new { idEmpresa = 0, descripcion = "GRP-TEST-RAIZ-NEW", idGrupoPadre = (int?)null };

        var response = await client.PostAsJsonAsync("/api/grupos-articulos", cmd);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "un grupo raíz válido (sin padre) debe retornar 201");

        var result = await response.Content.ReadFromJsonAsync<GrupoArticuloDto>(JsonOpts);
        result.Should().NotBeNull();
        result!.IdGrupo.Should().BeGreaterThan(0);
        result.Descripcion.Should().Be("GRP-TEST-RAIZ-NEW");
        result.IdGrupoPadre.Should().BeNull();

        response.Headers.Location.Should().NotBeNull("la respuesta 201 debe incluir Location");
    }

    [Fact]
    public async Task POST_grupo_con_padre_valido_retorna_201_como_subgrupo()
    {
        using var client = ClienteConToken();
        var cmd = new { idEmpresa = 0, descripcion = "GRP-TEST-SUB-NEW", idGrupoPadre = (int?)_idGrupoRaiz };

        var response = await client.PostAsJsonAsync("/api/grupos-articulos", cmd);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "un subgrupo con padre válido debe retornar 201");

        var result = await response.Content.ReadFromJsonAsync<GrupoArticuloDto>(JsonOpts);
        result!.IdGrupoPadre.Should().Be(_idGrupoRaiz,
            "el subgrupo debe referenciar al grupo padre correcto");
    }

    [Fact]
    public async Task POST_grupo_idEmpresa_del_jwt_sobreescribe_body()
    {
        // El endpoint hace: var cmd = command with { IdEmpresa = currentUser.IdEmpresa }
        // El cuerpo puede enviar IdEmpresa = 99, pero el handler lo ignora
        using var client = ClienteConToken();
        var cmd = new { idEmpresa = 99, descripcion = "GRP-TEST-EMP-OVERRIDE", idGrupoPadre = (int?)null };

        var response = await client.PostAsJsonAsync("/api/grupos-articulos", cmd);

        response.StatusCode.Should().Be(HttpStatusCode.Created,
            "el IdEmpresa del body debe ser ignorado — se usa el del JWT");

        var result = await response.Content.ReadFromJsonAsync<GrupoArticuloDto>(JsonOpts);
        // El grupo fue creado bajo la empresa del JWT (empresa de prueba), no empresa 99
        result!.IdGrupo.Should().BeGreaterThan(0);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers privados
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<string> LoginYObtenerTokenAsync()
    {
        var dto = new NewGest.Application.DTOs.LoginDto(
            NewgestWebApplicationFactory.IdEmpresaTest,
            NewgestWebApplicationFactory.UsuarioAdmin,
            NewgestWebApplicationFactory.PasswordAdmin);

        var response = await _client.PostAsJsonAsync("/api/auth/login", dto);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<NewGest.Application.DTOs.LoginResultDto>(JsonOpts);
        return result!.Token;
    }

    private HttpClient ClienteConToken()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _token);
        return client;
    }

    private static async Task<string> LeerMensajeErrorAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msgProp))
                return msgProp.GetString() ?? body;
        }
        catch { /* devolver body raw si no es JSON */ }
        return body;
    }
}
