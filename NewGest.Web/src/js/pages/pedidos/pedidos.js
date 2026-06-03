import { getPedidos, getPedidoById, crearPedido, anularPedido, generarRemito } from '../../api/pedidos.js';
import { getVendedores } from '../../api/empleados.js';
import { getDepositos } from '../../api/stock.js';

let paginaActual = 1;
let estadoFiltro = null;
let pedidoActivo = null;

// ─── Carga inicial ────────────────────────────────────────────────────────────
cargarPedidos();
cargarVendedoresEnModal();
cargarDepositosEnModal();

async function cargarPedidos() {
  const tbody = document.getElementById('tabla-pedidos');
  try {
    const data = await getPedidos({ estado: estadoFiltro, page: paginaActual });
    if (!data.items.length) {
      tbody.innerHTML = '<tr><td colspan="7" class="px-4 py-8 text-center text-gray-400 text-sm">Sin pedidos</td></tr>';
      return;
    }
    tbody.innerHTML = data.items.map(p => `
      <tr class="hover:bg-gray-50 cursor-pointer">
        <td class="px-4 py-3 font-mono text-gray-700">#${p.idPedido}</td>
        <td class="px-4 py-3 text-gray-900">${p.razonSocialCliente || `Cliente #${p.idCliente}`}</td>
        <td class="px-4 py-3 text-gray-600">${new Date(p.fechaPedido).toLocaleDateString('es-AR')}</td>
        <td class="px-4 py-3 text-gray-600">${p.fechaEntregaEstimada ? new Date(p.fechaEntregaEstimada).toLocaleDateString('es-AR') : '—'}</td>
        <td class="px-4 py-3 text-center">${badgeEstado(p.estado)}</td>
        <td class="px-4 py-3 text-center text-gray-600">${p.cantidadItems}</td>
        <td class="px-4 py-3 text-right flex gap-2 justify-end">
          ${p.estado !== 3 && p.estado !== 4
            ? `<button class="btn-remito text-green-600 hover:text-green-800 text-xs font-medium px-2 py-1 rounded border border-green-300" data-id="${p.idPedido}">Remito</button>`
            : ''}
          ${p.estado !== 3 && p.estado !== 4
            ? `<button class="btn-anular text-red-600 hover:text-red-800 text-xs font-medium px-2 py-1 rounded border border-red-300" data-id="${p.idPedido}">Anular</button>`
            : ''}
        </td>
      </tr>`).join('');

    renderPaginacion(data.total, data.pageSize, paginaActual, document.getElementById('paginacion'));
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="7" class="text-center text-red-500 py-4">${err.message}</td></tr>`;
  }
}

// ─── Filtros de estado ────────────────────────────────────────────────────────
document.querySelectorAll('.filtro-estado').forEach(btn => {
  btn.addEventListener('click', () => {
    estadoFiltro = btn.dataset.estado ? parseInt(btn.dataset.estado) : null;
    paginaActual = 1;
    cargarPedidos();
  });
});

// ─── Acciones en tabla (event delegation) ────────────────────────────────────
document.getElementById('tabla-pedidos').addEventListener('click', async e => {
  const btnAnular = e.target.closest('.btn-anular');
  const btnRemito = e.target.closest('.btn-remito');

  if (btnAnular) {
    const id = parseInt(btnAnular.dataset.id);
    if (!confirm(`¿Anular pedido #${id}?`)) return;
    try {
      await anularPedido(id);
      cargarPedidos();
    } catch (err) {
      alert(err.message);
    }
  }

  if (btnRemito) {
    const id = parseInt(btnRemito.dataset.id);
    await abrirModalRemito(id);
  }
});

// ─── Modal: Nuevo Pedido ──────────────────────────────────────────────────────
const modalPedido = document.getElementById('modal-pedido');
const formPedido = document.getElementById('form-pedido');

document.getElementById('btn-nuevo').addEventListener('click', () => {
  modalPedido.classList.remove('hidden');
  modalPedido.classList.add('flex');
  renderItemVacio();
});

document.querySelectorAll('.modal-close').forEach(b => b.addEventListener('click', () => {
  modalPedido.classList.add('hidden');
  modalPedido.classList.remove('flex');
  formPedido.reset();
  document.getElementById('items-pedido').innerHTML = '';
  document.getElementById('pedido-error').classList.add('hidden');
}));

document.getElementById('btn-agregar-item').addEventListener('click', renderItemVacio);

function renderItemVacio() {
  const container = document.getElementById('items-pedido');
  const idx = container.children.length;
  const div = document.createElement('div');
  div.className = 'grid grid-cols-4 gap-2 items-center';
  div.innerHTML = `
    <input placeholder="ID Artículo" type="number" name="items[${idx}][idArticulo]" required min="1"
      class="border border-gray-300 rounded-lg px-2 py-1.5 text-sm focus:ring-2 focus:ring-blue-500" />
    <input placeholder="Cantidad" type="number" step="0.01" name="items[${idx}][cantidad]" required min="0.01"
      class="border border-gray-300 rounded-lg px-2 py-1.5 text-sm focus:ring-2 focus:ring-blue-500" />
    <input placeholder="Precio unit." type="number" step="0.01" name="items[${idx}][precioUnitario]" required min="0"
      class="border border-gray-300 rounded-lg px-2 py-1.5 text-sm focus:ring-2 focus:ring-blue-500" />
    <button type="button" class="btn-quitar-item text-red-400 hover:text-red-600 text-lg leading-none">&times;</button>`;
  div.querySelector('.btn-quitar-item').addEventListener('click', () => div.remove());
  container.appendChild(div);
}

formPedido.addEventListener('submit', async e => {
  e.preventDefault();
  const errDiv = document.getElementById('pedido-error');
  errDiv.classList.add('hidden');

  const fd = new FormData(formPedido);
  const rawData = Object.fromEntries(fd);
  const itemsMap = {};
  for (const [key, val] of Object.entries(rawData)) {
    const match = key.match(/^items\[(\d+)\]\[(\w+)\]$/);
    if (match) {
      const [, idx, field] = match;
      itemsMap[idx] ??= {};
      itemsMap[idx][field] = parseFloat(val);
    }
  }

  const dto = {
    idCliente: parseInt(rawData.idCliente),
    idVendedor: rawData.idVendedor ? parseInt(rawData.idVendedor) : null,
    fechaEntregaEstimada: rawData.fechaEntregaEstimada || null,
    observaciones: rawData.observaciones || null,
    items: Object.values(itemsMap)
  };

  try {
    await crearPedido(dto);
    document.querySelectorAll('.modal-close')[0].click();
    cargarPedidos();
  } catch (err) {
    errDiv.textContent = err.message;
    errDiv.classList.remove('hidden');
  }
});

// ─── Modal: Generar Remito ────────────────────────────────────────────────────
const modalRemito = document.getElementById('modal-remito');

async function abrirModalRemito(idPedido) {
  pedidoActivo = await getPedidoById(idPedido);
  if (!pedidoActivo) return;

  // Renderizar items con input de cantidad a despachar
  const container = document.getElementById('remito-items');
  container.innerHTML = pedidoActivo.items.map(item => `
    <div class="grid grid-cols-3 gap-2 items-center border-b border-gray-100 pb-2">
      <span class="text-sm text-gray-700">Artículo #${item.idArticulo}</span>
      <span class="text-sm text-gray-500">Pendiente: ${item.cantidadPendiente}</span>
      <input type="number" step="0.01" min="0.01" max="${item.cantidadPendiente}"
        placeholder="Cant. a despachar"
        data-articulo="${item.idArticulo}"
        class="remito-qty border border-gray-300 rounded-lg px-2 py-1.5 text-sm focus:ring-2 focus:ring-green-500" />
    </div>`).join('');

  document.getElementById('remito-error').classList.add('hidden');
  modalRemito.classList.remove('hidden');
  modalRemito.classList.add('flex');
}

document.querySelectorAll('.remito-close').forEach(b => b.addEventListener('click', () => {
  modalRemito.classList.add('hidden');
  modalRemito.classList.remove('flex');
  pedidoActivo = null;
}));

document.getElementById('btn-confirmar-remito').addEventListener('click', async () => {
  const errDiv = document.getElementById('remito-error');
  errDiv.classList.add('hidden');
  const idDeposito = parseInt(document.getElementById('remito-deposito').value);
  if (!idDeposito) { errDiv.textContent = 'Seleccione un depósito.'; errDiv.classList.remove('hidden'); return; }

  const items = [];
  document.querySelectorAll('.remito-qty').forEach(input => {
    const cantidad = parseFloat(input.value);
    if (cantidad > 0) items.push({ idArticulo: parseInt(input.dataset.articulo), cantidad });
  });

  if (!items.length) { errDiv.textContent = 'Ingrese al menos una cantidad a despachar.'; errDiv.classList.remove('hidden'); return; }

  try {
    await generarRemito(pedidoActivo.idPedido, { idDeposito, items });
    document.querySelectorAll('.remito-close')[0].click();
    cargarPedidos();
  } catch (err) {
    errDiv.textContent = err.message;
    errDiv.classList.remove('hidden');
  }
});

// ─── Loaders auxiliares ───────────────────────────────────────────────────────
async function cargarVendedoresEnModal() {
  const sel = formPedido.querySelector('[name="idVendedor"]');
  try {
    const vendedores = await getVendedores();
    if (vendedores.length) {
      sel.innerHTML = '<option value="">Sin vendedor</option>' +
        vendedores.map(v => `<option value="${v.idEmpleado}">${v.apellidoNombre}</option>`).join('');
    }
  } catch { /* silencioso */ }
}

async function cargarDepositosEnModal() {
  const sel = document.getElementById('remito-deposito');
  try {
    const depositos = await getDepositos();
    sel.innerHTML = depositos.map(d => `<option value="${d.idDeposito}">${d.descripcion}</option>`).join('');
  } catch {
    sel.innerHTML = '<option value="">Error al cargar</option>';
  }
}

// ─── Helpers ──────────────────────────────────────────────────────────────────
function badgeEstado(estado) {
  const map = {
    1: ['Pendiente', 'bg-yellow-100 text-yellow-800'],
    2: ['Parcial', 'bg-blue-100 text-blue-800'],
    3: ['Entregado', 'bg-green-100 text-green-800'],
    4: ['Anulado', 'bg-gray-100 text-gray-500']
  };
  const [label, cls] = map[estado] ?? ['?', 'bg-gray-100 text-gray-600'];
  return `<span class="inline-flex px-2 py-0.5 rounded text-xs font-medium ${cls}">${label}</span>`;
}

function renderPaginacion(total, pageSize, page, container) {
  const totalPages = Math.ceil(total / pageSize);
  container.innerHTML = `
    <span>${total} pedidos — Página ${page} de ${totalPages}</span>
    <div class="flex gap-2">
      <button class="px-3 py-1 rounded border text-sm ${page <= 1 ? 'opacity-40 cursor-not-allowed' : 'hover:bg-gray-100'}" ${page <= 1 ? 'disabled' : ''} id="btn-prev">Anterior</button>
      <button class="px-3 py-1 rounded border text-sm ${page >= totalPages ? 'opacity-40 cursor-not-allowed' : 'hover:bg-gray-100'}" ${page >= totalPages ? 'disabled' : ''} id="btn-next">Siguiente</button>
    </div>`;
  document.getElementById('btn-prev')?.addEventListener('click', () => { paginaActual--; cargarPedidos(); });
  document.getElementById('btn-next')?.addEventListener('click', () => { paginaActual++; cargarPedidos(); });
}
