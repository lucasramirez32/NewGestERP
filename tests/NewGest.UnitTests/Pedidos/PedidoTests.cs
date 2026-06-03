using FluentAssertions;
using NewGest.Domain.Common;
using NewGest.Domain.Entities.Com;
using NewGest.Domain.Enums;

namespace NewGest.UnitTests.Pedidos;

public class PedidoTests
{
    // ─── Helper ───────────────────────────────────────────────────────────────
    private static Pedido CrearPedidoValido(params (int IdArticulo, decimal Cantidad, decimal Precio)[] items)
    {
        var listaItems = items.Length > 0
            ? items
            : new[] { (IdArticulo: 1, Cantidad: 5m, Precio: 100m) };

        return Pedido.Crear(
            idEmpresa: 1,
            idCliente: 10,
            idVendedor: null,
            fechaEntregaEstimada: null,
            observaciones: null,
            items: listaItems.Select(i => (i.IdArticulo, i.Cantidad, i.Precio)));
    }

    // ─── Crear ────────────────────────────────────────────────────────────────
    [Fact]
    public void Crear_sin_items_lanza_DomainException()
    {
        var act = () => Pedido.Crear(1, 10, null, null, null, Enumerable.Empty<(int, decimal, decimal)>());

        act.Should().Throw<DomainException>()
            .WithMessage("*al menos un artículo*");
    }

    [Fact]
    public void Crear_valido_estado_inicial_es_Pendiente()
    {
        var pedido = CrearPedidoValido();

        pedido.Estado.Should().Be(EstadoPedido.Pendiente);
        pedido.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Crear_valido_asigna_correctamente_campos_del_item()
    {
        var pedido = CrearPedidoValido((1, 7.5m, 250m));

        var item = pedido.Items.Single();
        item.IdArticulo.Should().Be(1);
        item.CantidadPedida.Should().Be(7.5m);
        item.PrecioUnitario.Should().Be(250m);
        item.CantidadEntregada.Should().Be(0);
        item.CantidadPendiente.Should().Be(7.5m);
    }

    // ─── Anular ───────────────────────────────────────────────────────────────
    [Fact]
    public void Anular_pedido_Pendiente_cambia_estado_a_Anulado()
    {
        var pedido = CrearPedidoValido();
        pedido.Estado.Should().Be(EstadoPedido.Pendiente);

        pedido.Anular();

        pedido.Estado.Should().Be(EstadoPedido.Anulado);
    }

    [Fact]
    public void Anular_pedido_Entregado_lanza_DomainException()
    {
        var pedido = CrearPedidoValido((1, 2m, 100m));
        // simular entrega total
        pedido.Items.Single().CantidadEntregada = 2m;
        pedido.ActualizarEstado();
        pedido.Estado.Should().Be(EstadoPedido.Entregado);

        var act = () => pedido.Anular();

        act.Should().Throw<DomainException>()
            .WithMessage("*no se puede anular*entregado*");
    }

    // ─── GenerarRemito / ActualizarEstado ────────────────────────────────────
    [Fact]
    public void GenerarRemito_con_cantidad_mayor_a_pendiente_lanza_DomainException()
    {
        var pedido = CrearPedidoValido((1, 3m, 100m));
        var despachos = new[] { (IdArticulo: 1, Cantidad: 10m) };

        var act = () => Remito.Crear(pedido, despachos.Select(d => (d.IdArticulo, d.Cantidad)));

        act.Should().Throw<DomainException>()
            .WithMessage("*supera la pendiente*");
    }

    [Fact]
    public void GenerarRemito_parcial_deja_estado_Parcial()
    {
        var pedido = CrearPedidoValido((1, 10m, 100m));
        var despachos = new[] { (1, 4m) };

        Remito.Crear(pedido, despachos.Select(d => (d.Item1, d.Item2)));

        pedido.Estado.Should().Be(EstadoPedido.Parcial);
        pedido.Items.Single().CantidadPendiente.Should().Be(6m);
    }

    [Fact]
    public void GenerarRemito_total_deja_estado_Entregado()
    {
        var pedido = CrearPedidoValido((1, 5m, 100m));
        var despachos = new[] { (1, 5m) };

        var remito = Remito.Crear(pedido, despachos.Select(d => (d.Item1, d.Item2)));

        pedido.Estado.Should().Be(EstadoPedido.Entregado);
        remito.Items.Should().HaveCount(1);
        remito.Items.Single().Cantidad.Should().Be(5m);
    }

    [Fact]
    public void GenerarRemito_sin_items_lanza_DomainException()
    {
        var pedido = CrearPedidoValido();

        var act = () => Remito.Crear(pedido, Enumerable.Empty<(int, decimal)>());

        act.Should().Throw<DomainException>()
            .WithMessage("*al menos un artículo*");
    }

    [Fact]
    public void GenerarRemito_articulo_no_pertenece_al_pedido_lanza_DomainException()
    {
        var pedido = CrearPedidoValido((1, 5m, 100m));
        var despachos = new[] { (999, 2m) }; // artículo que no existe en el pedido

        var act = () => Remito.Crear(pedido, despachos.Select(d => (d.Item1, d.Item2)));

        act.Should().Throw<DomainException>()
            .WithMessage("*no pertenece al pedido*");
    }
}
