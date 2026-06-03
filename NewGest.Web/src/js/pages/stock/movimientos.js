import {
  getDepositos, getExistencias, getHistorialMovimientos,
  registrarMovimiento, getAlertasReposicion, getStockValorizado
} from '../../api/stock.js';

// ─── Tabs ─────────────────────────────────────────────────────────────────────
const tabBtns = document.querySelectorAll('.tab-btn');
const tabPanels = document.querySelectorAll('.tab-panel');

tabBtns.forEach(btn => {
  btn.addEventListener('click', () => {
    const target = btn.dataset.tab;
    tabBtns.forEach(b => {
      b.classList.remove('border-blue-600', 'text-blue-600');
      b.classList.add('border-transparent', 'text-gray-500');
    });
    btn.classList.add('border-blue-600', 'text-blue-600');
    btn.classList.remove('border-transparent', 'text-gray-500');
    tabPanels.forEach(p => p.classList.toggle('hidden', p.id !== `panel-${target}`));
  });
});

// ─── Modal ────────────────────────────────────────────────────────────────────
const modal = document.getElementById('modal-movimiento');
const form = document.getElementById('form-movimiento');
const errorDiv = document.getElementById('form-error');
const campoCosto = document.getElementById('campo-costo');
const selectDeposito = form.querySelector('[name="idDeposito"]');

document.getElementById('btn-registrar').addEventListener('click', () => {
  modal.classList.remove('hidden');
  modal.classList.add('flex');
});

const cerrarModal = () => {
  modal.classList.add('hidden');
  modal.classList.remove('flex');
  form.reset();
  errorDiv.classList.add('hidden');
};
document.getElementById('modal-close').addEventListener('click', cerrarModal);
document.getElementById('btn-cancelar').addEventListener('click', cerrarModal);

// Mostrar campo costo solo en Entrada
form.querySelector('[name="tipo"]').addEventListener('change', e => {
  campoCosto.classList.toggle('hidden', e.target.value !== '1');
});

// Cargar depósitos en el select
async function cargarDepositos() {
  try {
    const depositos = await getDepositos();
    selectDeposito.innerHTML = depositos
      .map(d => `<option value="${d.idDeposito}">${d.descripcion}</option>`)
      .join('');
  } catch {
    selectDeposito.innerHTML = '<option value="">Error al cargar</option>';
  }
}
cargarDepositos();

// Submit movimiento
form.addEventListener('submit', async e => {
  e.preventDefault();
  errorDiv.classList.add('hidden');
  const data = Object.fromEntries(new FormData(form));
  const dto = {
    idArticulo: parseInt(data.idArticulo),
    idDeposito: parseInt(data.idDeposito),
    tipo: parseInt(data.tipo),
    cantidad: parseFloat(data.cantidad),
    costoUnitario: data.tipo === '1' ? parseFloat(data.costoUnitario || 0) : 0,
    observaciones: data.observaciones || null,
    numeroSerie: null,
    idComprobanteOrigen: null
  };
  try {
    await registrarMovimiento(dto);
    cerrarModal();
    // Refrescar existencias si hay artículo buscado
    if (document.getElementById('search-articulo').value) {
      cargarExistencias();
    }
  } catch (err) {
    errorDiv.textContent = err.message;
    errorDiv.classList.remove('hidden');
  }
});

// ─── Existencias ──────────────────────────────────────────────────────────────
async function cargarExistencias() {
  const idArticulo = document.getElementById('search-articulo').value;
  if (!idArticulo) return;
  const tbody = document.getElementById('tabla-existencias');
  tbody.innerHTML = '<tr><td colspan="6" class="px-4 py-4 text-center text-gray-400 text-sm">Cargando...</td></tr>';
  try {
    const existencias = await getExistencias(idArticulo);
    if (!existencias.length) {
      tbody.innerHTML = '<tr><td colspan="6" class="px-4 py-8 text-center text-gray-400 text-sm">Sin existencias</td></tr>';
      return;
    }
    tbody.innerHTML = existencias.map(e => `
      <tr class="hover:bg-gray-50">
        <td class="px-4 py-3 text-gray-900">${e.codigoArticulo} — ${e.descripcionArticulo}</td>
        <td class="px-4 py-3 text-gray-600">${e.nombreDeposito}</td>
        <td class="px-4 py-3 text-right font-mono">${e.cantidad.toFixed(2)}</td>
        <td class="px-4 py-3 text-right font-mono">$${e.costoPromedio.toFixed(4)}</td>
        <td class="px-4 py-3 text-right font-mono">${e.stockMinimo.toFixed(2)}</td>
        <td class="px-4 py-3 text-center">${badgeNivel(e.nivelBadge)}</td>
      </tr>`).join('');
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="6" class="px-4 py-4 text-center text-red-500 text-sm">${err.message}</td></tr>`;
  }
}

document.getElementById('btn-buscar-existencias').addEventListener('click', cargarExistencias);

// ─── Historial ────────────────────────────────────────────────────────────────
let historialPage = 1;

async function cargarHistorial() {
  const idArticulo = document.getElementById('historial-articulo').value;
  if (!idArticulo) return;
  const tbody = document.getElementById('tabla-historial');
  tbody.innerHTML = '<tr><td colspan="7" class="px-4 py-4 text-center text-gray-400 text-sm">Cargando...</td></tr>';
  try {
    const data = await getHistorialMovimientos(idArticulo, { page: historialPage, pageSize: 20 });
    if (!data.items.length) {
      tbody.innerHTML = '<tr><td colspan="7" class="px-4 py-8 text-center text-gray-400 text-sm">Sin movimientos</td></tr>';
      return;
    }
    tbody.innerHTML = data.items.map(m => `
      <tr class="hover:bg-gray-50">
        <td class="px-4 py-3 text-gray-600">${new Date(m.fechaMovimiento).toLocaleString('es-AR')}</td>
        <td class="px-4 py-3">${badgeTipo(m.tipo)}</td>
        <td class="px-4 py-3 text-gray-900">${m.descripcionArticulo}</td>
        <td class="px-4 py-3 text-gray-600">${m.nombreDeposito}</td>
        <td class="px-4 py-3 text-right font-mono">${m.cantidad.toFixed(2)}</td>
        <td class="px-4 py-3 text-right font-mono">$${m.costoUnitario.toFixed(4)}</td>
        <td class="px-4 py-3 text-gray-500 text-xs">${m.observaciones ?? ''}</td>
      </tr>`).join('');
    renderPaginacion(data.total, data.pageSize, historialPage, document.getElementById('historial-paginacion'), n => { historialPage = n; cargarHistorial(); });
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="7" class="px-4 py-4 text-center text-red-500 text-sm">${err.message}</td></tr>`;
  }
}

document.getElementById('btn-buscar-historial').addEventListener('click', () => { historialPage = 1; cargarHistorial(); });

// ─── Alertas ──────────────────────────────────────────────────────────────────
document.getElementById('btn-cargar-alertas').addEventListener('click', async () => {
  const tbody = document.getElementById('tabla-alertas');
  try {
    const alertas = await getAlertasReposicion();
    tbody.innerHTML = alertas.map(e => `
      <tr class="hover:bg-orange-50">
        <td class="px-4 py-3 text-gray-900">${e.codigoArticulo} — ${e.descripcionArticulo}</td>
        <td class="px-4 py-3 text-gray-600">${e.nombreDeposito}</td>
        <td class="px-4 py-3 text-right font-mono text-red-700 font-semibold">${e.cantidad.toFixed(2)}</td>
        <td class="px-4 py-3 text-right font-mono">${e.stockMinimo.toFixed(2)}</td>
        <td class="px-4 py-3 text-center">${badgeNivel(e.nivelBadge)}</td>
      </tr>`).join('') || '<tr><td colspan="5" class="px-4 py-8 text-center text-gray-400 text-sm">Sin alertas</td></tr>';
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-red-500 py-4">${err.message}</td></tr>`;
  }
});

// ─── Valorizado ───────────────────────────────────────────────────────────────
document.getElementById('btn-cargar-valorizado').addEventListener('click', async () => {
  const tbody = document.getElementById('tabla-valorizado');
  try {
    const items = await getStockValorizado();
    tbody.innerHTML = items.map(i => `
      <tr class="hover:bg-gray-50">
        <td class="px-4 py-3 text-gray-900">${i.codigoArticulo} — ${i.descripcionArticulo}</td>
        <td class="px-4 py-3 text-gray-600">${i.nombreDeposito}</td>
        <td class="px-4 py-3 text-right font-mono">${i.cantidad.toFixed(2)}</td>
        <td class="px-4 py-3 text-right font-mono">$${i.costoPromedio.toFixed(4)}</td>
        <td class="px-4 py-3 text-right font-mono font-semibold">$${i.valorTotal.toFixed(2)}</td>
      </tr>`).join('') || '<tr><td colspan="5" class="px-4 py-8 text-center text-gray-400 text-sm">Sin stock valorizado</td></tr>';
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="5" class="text-center text-red-500 py-4">${err.message}</td></tr>`;
  }
});

// ─── Helpers ──────────────────────────────────────────────────────────────────
function badgeNivel(nivel) {
  const map = {
    verde: 'bg-green-100 text-green-800',
    amarillo: 'bg-yellow-100 text-yellow-800',
    rojo: 'bg-red-100 text-red-800'
  };
  return `<span class="inline-flex px-2 py-0.5 rounded text-xs font-medium ${map[nivel] ?? map.verde}">${nivel}</span>`;
}

function badgeTipo(tipo) {
  const map = { 1: ['Entrada', 'bg-green-100 text-green-800'], 2: ['Salida', 'bg-red-100 text-red-800'], 3: ['Transfer.', 'bg-blue-100 text-blue-800'], 4: ['Ajuste', 'bg-yellow-100 text-yellow-800'] };
  const [label, cls] = map[tipo] ?? ['?', 'bg-gray-100 text-gray-600'];
  return `<span class="inline-flex px-2 py-0.5 rounded text-xs font-medium ${cls}">${label}</span>`;
}

function renderPaginacion(total, pageSize, page, container, onPage) {
  const totalPages = Math.ceil(total / pageSize);
  container.innerHTML = `
    <span>${total} registros — Página ${page} de ${totalPages}</span>
    <div class="flex gap-2">
      <button class="px-3 py-1 rounded border text-sm ${page <= 1 ? 'opacity-40 cursor-not-allowed' : 'hover:bg-gray-100'}" ${page <= 1 ? 'disabled' : ''} data-page="${page - 1}">Anterior</button>
      <button class="px-3 py-1 rounded border text-sm ${page >= totalPages ? 'opacity-40 cursor-not-allowed' : 'hover:bg-gray-100'}" ${page >= totalPages ? 'disabled' : ''} data-page="${page + 1}">Siguiente</button>
    </div>`;
  container.querySelectorAll('[data-page]').forEach(btn => {
    btn.addEventListener('click', () => onPage(parseInt(btn.dataset.page)));
  });
}
