import { emitirFactura } from '../../api/comprobantes.js';
import { getClientes } from '../../api/clientes.js';

// Mapa alícuota valor-enum → porcentaje decimal
const ALICUOTA_PCT = { 0: 0, 3: 0, 9: 0.025, 8: 0.05, 4: 0.105, 5: 0.21, 6: 0.27 };

let idClienteSeleccionado = null;
let items = [];

// ─── Init ─────────────────────────────────────────────────────────────────────
document.getElementById('fecha').value = new Date().toISOString().slice(0, 10);

document.getElementById('btn-agregar-item').addEventListener('click', agregarFila);
document.getElementById('form-factura').addEventListener('submit', emitir);
document.getElementById('cliente-search').addEventListener('input', buscarCliente);
document.getElementById('dialog-nueva').addEventListener('click', () => {
  document.getElementById('dialog-cae').close();
  location.reload();
});

// ─── Búsqueda de clientes ─────────────────────────────────────────────────────
let timerCliente;
async function buscarCliente(e) {
  clearTimeout(timerCliente);
  const q = e.target.value.trim();
  if (q.length < 2) { cerrarDropdown(); return; }

  timerCliente = setTimeout(async () => {
    const { items: lista } = await getClientes({ search: q, pageSize: 8 });
    mostrarDropdown(lista);
  }, 250);
}

function mostrarDropdown(lista) {
  const ul = document.getElementById('cliente-dropdown');
  if (!lista.length) { ul.classList.add('hidden'); return; }

  ul.innerHTML = lista.map(c => `
    <li class="px-3 py-2 hover:bg-blue-50 cursor-pointer text-gray-800"
        data-id="${c.idCliente}"
        data-nombre="${c.razonSocial}"
        data-cuit="${c.cuit ?? ''}"
        data-condicion="${c.condicionIvaLabel ?? ''}">
      <span class="font-medium">${c.razonSocial}</span>
      ${c.cuit ? `<span class="text-xs text-gray-400 ml-1">${formatCuit(c.cuit)}</span>` : ''}
    </li>
  `).join('');

  ul.querySelectorAll('li').forEach(li => li.addEventListener('click', seleccionarCliente));
  ul.classList.remove('hidden');
}

function cerrarDropdown() {
  document.getElementById('cliente-dropdown').classList.add('hidden');
}

function seleccionarCliente(e) {
  const li = e.currentTarget;
  idClienteSeleccionado = parseInt(li.dataset.id);
  document.getElementById('id-cliente').value = idClienteSeleccionado;
  document.getElementById('cliente-search').value = li.dataset.nombre;
  document.getElementById('cliente-nombre').textContent = li.dataset.nombre;
  document.getElementById('cliente-cuit').textContent = li.dataset.cuit ? `CUIT: ${formatCuit(li.dataset.cuit)}` : '';
  document.getElementById('cliente-condicion').textContent = li.dataset.condicion;
  document.getElementById('cliente-seleccionado').classList.remove('hidden');
  cerrarDropdown();
}

document.addEventListener('click', e => {
  if (!e.target.closest('#cliente-search') && !e.target.closest('#cliente-dropdown')) cerrarDropdown();
});

// ─── Items ────────────────────────────────────────────────────────────────────
function agregarFila() {
  const idx = items.length;
  items.push({ idArticulo: 0, descripcion: '', cantidad: 1, precioUnitario: 0, alicuota: 5 });

  const tbody = document.getElementById('tabla-items');
  const tr = document.createElement('tr');
  tr.className = 'border-b border-gray-100';
  tr.dataset.idx = idx;
  tr.innerHTML = `
    <td class="py-2 pr-2">
      <input type="text" placeholder="Descripción del artículo" data-field="descripcion"
        class="w-full border border-gray-200 rounded px-2 py-1 text-sm focus:ring-1 focus:ring-blue-400" />
      <input type="hidden" data-field="idArticulo" value="0" />
    </td>
    <td class="py-2 pr-2">
      <input type="number" min="0.001" step="0.001" value="1" data-field="cantidad"
        class="w-full border border-gray-200 rounded px-2 py-1 text-sm text-right focus:ring-1 focus:ring-blue-400" />
    </td>
    <td class="py-2 pr-2">
      <input type="number" min="0" step="0.01" value="0" data-field="precioUnitario"
        class="w-full border border-gray-200 rounded px-2 py-1 text-sm text-right focus:ring-1 focus:ring-blue-400" />
    </td>
    <td class="py-2 pr-2">
      <select data-field="alicuota"
        class="w-full border border-gray-200 rounded px-2 py-1 text-sm focus:ring-1 focus:ring-blue-400">
        <option value="5" selected>21%</option>
        <option value="4">10,5%</option>
        <option value="3">0%</option>
        <option value="0">Exento</option>
        <option value="9">2,5%</option>
        <option value="8">5%</option>
        <option value="6">27%</option>
      </select>
    </td>
    <td class="py-2 pr-2 text-right font-mono text-gray-700 subtotal">$ 0,00</td>
    <td class="py-2">
      <button type="button" class="text-red-400 hover:text-red-600 text-xs" data-eliminar>✕</button>
    </td>
  `;

  tr.querySelectorAll('[data-field]').forEach(el =>
    el.addEventListener('input', () => actualizarItem(idx, tr)));
  tr.querySelector('[data-eliminar]').addEventListener('click', () => eliminarFila(idx, tr));

  tbody.appendChild(tr);
  document.getElementById('sin-items').classList.add('hidden');
  recalcularTotales();
}

function actualizarItem(idx, tr) {
  items[idx] = {
    idArticulo: parseInt(tr.querySelector('[data-field="idArticulo"]').value) || 0,
    descripcion: tr.querySelector('[data-field="descripcion"]').value,
    cantidad: parseFloat(tr.querySelector('[data-field="cantidad"]').value) || 0,
    precioUnitario: parseFloat(tr.querySelector('[data-field="precioUnitario"]').value) || 0,
    alicuota: parseInt(tr.querySelector('[data-field="alicuota"]').value),
  };

  const { cantidad, precioUnitario, alicuota } = items[idx];
  const neto = cantidad * precioUnitario;
  const iva = neto * (ALICUOTA_PCT[alicuota] ?? 0);
  tr.querySelector('.subtotal').textContent = fmt(neto + iva);

  recalcularTotales();
}

function eliminarFila(idx, tr) {
  items[idx] = null;
  tr.remove();
  const activos = items.filter(Boolean);
  if (!activos.length) document.getElementById('sin-items').classList.remove('hidden');
  recalcularTotales();
}

// ─── Totales ──────────────────────────────────────────────────────────────────
function recalcularTotales() {
  const activos = items.filter(Boolean);

  const neto21 = sumarNeto(activos, 5);
  const iva21  = neto21 * 0.21;
  const neto105 = sumarNeto(activos, 4);
  const iva105  = neto105 * 0.105;
  const exento  = sumarNeto(activos, 0) + sumarNeto(activos, 3);
  const total   = neto21 + iva21 + neto105 + iva105 + exento;

  document.getElementById('neto-21').textContent   = fmt(neto21);
  document.getElementById('iva-21').textContent    = fmt(iva21);
  document.getElementById('neto-105').textContent  = fmt(neto105);
  document.getElementById('iva-105').textContent   = fmt(iva105);
  document.getElementById('total-exento').textContent = fmt(exento);
  document.getElementById('total-general').textContent = fmt(total);
}

function sumarNeto(activos, alicuota) {
  return activos
    .filter(i => i.alicuota === alicuota)
    .reduce((s, i) => s + i.cantidad * i.precioUnitario, 0);
}

// ─── Emitir ───────────────────────────────────────────────────────────────────
async function emitir(e) {
  e.preventDefault();
  const btnEmitir = document.getElementById('btn-emitir');
  const msgError  = document.getElementById('msg-error');

  const activos = items.filter(Boolean).filter(i => i.descripcion && i.cantidad > 0);
  if (!idClienteSeleccionado) { mostrarError('Seleccioná un cliente.'); return; }
  if (!activos.length)        { mostrarError('Agregá al menos un ítem.'); return; }

  btnEmitir.disabled = true;
  btnEmitir.textContent = 'Emitiendo…';
  msgError.classList.add('hidden');

  try {
    const dto = {
      tipo: parseInt(document.getElementById('tipo').value),
      puntoVenta: parseInt(document.getElementById('punto-venta').value),
      fecha: document.getElementById('fecha').value,
      idCliente: idClienteSeleccionado,
      items: activos.map(i => ({
        idArticulo: i.idArticulo || 0,
        descripcion: i.descripcion,
        cantidad: i.cantidad,
        precioUnitario: i.precioUnitario,
        alicuota: i.alicuota,
      })),
    };

    const result = await emitirFactura(dto);
    mostrarDialogCae(result);
  } catch (err) {
    mostrarError(err.message ?? 'Error al emitir el comprobante.');
    btnEmitir.disabled = false;
    btnEmitir.textContent = 'Emitir comprobante';
  }
}

function mostrarDialogCae(result) {
  const tipoLabel = {1:'FC-A',6:'FC-B',11:'FC-C',3:'NC-A',8:'NC-B',13:'NC-C',2:'ND-A',7:'ND-B',12:'ND-C'};
  const tipo = tipoLabel[parseInt(document.getElementById('tipo').value)] ?? 'Comprobante';
  const pv   = document.getElementById('punto-venta').value.padStart(4, '0');
  const num  = String(result.numero).padStart(8, '0');

  document.getElementById('dialog-tipo-numero').textContent = `${tipo} ${pv}-${num}`;

  if (result.codigoCae) {
    document.getElementById('dialog-cae-codigo').textContent       = result.codigoCae;
    document.getElementById('dialog-cae-vencimiento').textContent  = result.fechaVencimientoCae ?? '';
    document.getElementById('dialog-cae-info').classList.remove('hidden');
  }

  document.getElementById('dialog-cae').showModal();
}

function mostrarError(msg) {
  const el = document.getElementById('msg-error');
  el.textContent = msg;
  el.classList.remove('hidden');
}

// ─── Helpers ──────────────────────────────────────────────────────────────────
function fmt(n) {
  return '$ ' + n.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

function formatCuit(cuit) {
  const s = String(cuit).replace(/\D/g, '');
  return s.length === 11 ? `${s.slice(0,2)}-${s.slice(2,10)}-${s.slice(10)}` : cuit;
}
