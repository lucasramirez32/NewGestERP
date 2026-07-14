import { getAsientos, crearAsiento } from '../../api/contabilidad.js';

let pagina = 1;
let partidas = [];

document.getElementById('fecha').value = new Date().toISOString().slice(0, 10);
document.getElementById('btn-nuevo').addEventListener('click', abrirModal);
document.getElementById('btn-agregar-partida').addEventListener('click', agregarPartida);
document.getElementById('form-asiento').addEventListener('submit', guardarAsiento);

cargar();

async function cargar() {
  const data = await getAsientos({ pagina, tamano: 20 });
  renderizarTabla(data.items);
  renderizarPaginacion(data.total, data.pageSize);
}

function renderizarTabla(items) {
  const tbody = document.getElementById('tabla-asientos');
  const sinAsientos = document.getElementById('sin-asientos');

  if (!items.length) {
    tbody.innerHTML = '';
    sinAsientos.classList.remove('hidden');
    return;
  }
  sinAsientos.classList.add('hidden');

  tbody.innerHTML = items.map(a => `
    <tr class="hover:bg-gray-50 cursor-pointer" title="${a.partidas?.length ?? 0} partidas">
      <td class="px-4 py-3 font-mono text-gray-700 text-xs">${String(a.numero).padStart(8, '0')}</td>
      <td class="px-4 py-3 text-gray-600">${new Date(a.fecha).toLocaleDateString('es-AR')}</td>
      <td class="px-4 py-3 text-gray-900 max-w-xs truncate">${a.descripcion}</td>
      <td class="px-4 py-3 text-center">
        <span class="px-2 py-0.5 rounded-full text-xs ${badgeTipo(a.tipoAsiento)}">${a.tipoAsiento}</span>
      </td>
      <td class="px-4 py-3 text-right font-mono text-gray-700">${fmt(a.totalDebe)}</td>
      <td class="px-4 py-3 text-right font-mono text-gray-700">${fmt(a.totalHaber)}</td>
    </tr>`).join('');
}

function badgeTipo(tipo) {
  return tipo === 'Manual'
    ? 'bg-blue-100 text-blue-700'
    : tipo === 'AutoFactura'
    ? 'bg-green-100 text-green-700'
    : 'bg-gray-100 text-gray-600';
}

function renderizarPaginacion(total, tamano) {
  const totalPaginas = Math.ceil(total / tamano);
  const div = document.getElementById('paginacion');
  if (totalPaginas <= 1) { div.innerHTML = ''; return; }
  div.innerHTML = Array.from({ length: totalPaginas }, (_, i) => `
    <button class="px-3 py-1 rounded text-sm ${i + 1 === pagina
      ? 'bg-blue-600 text-white'
      : 'bg-white border border-gray-200 text-gray-700 hover:bg-gray-50'}"
      data-pag="${i + 1}">${i + 1}</button>`).join('');
  div.querySelectorAll('button').forEach(b =>
    b.addEventListener('click', () => { pagina = parseInt(b.dataset.pag); cargar(); }));
}

// ─── Modal asiento ────────────────────────────────────────────────────────────
function abrirModal() {
  partidas = [];
  document.getElementById('tabla-partidas').innerHTML = '';
  document.getElementById('form-asiento').reset();
  document.getElementById('fecha').value = new Date().toISOString().slice(0, 10);
  recalcular();
  document.getElementById('modal-asiento').showModal();
}

function agregarPartida() {
  const idx = partidas.length;
  partidas.push({ idCuenta: 0, debe: 0, haber: 0, concepto: '' });

  const tr = document.createElement('tr');
  tr.dataset.idx = idx;
  tr.innerHTML = `
    <td class="px-2 py-1"><input type="number" min="1" placeholder="ID" data-field="idCuenta"
      class="w-20 border border-gray-200 rounded px-2 py-1 text-xs focus:ring-1 focus:ring-blue-400" /></td>
    <td class="px-2 py-1"><input type="number" min="0" step="0.01" value="0" data-field="debe"
      class="w-24 border border-gray-200 rounded px-2 py-1 text-xs text-right focus:ring-1 focus:ring-blue-400" /></td>
    <td class="px-2 py-1"><input type="number" min="0" step="0.01" value="0" data-field="haber"
      class="w-24 border border-gray-200 rounded px-2 py-1 text-xs text-right focus:ring-1 focus:ring-blue-400" /></td>
    <td class="px-2 py-1"><input type="text" placeholder="Concepto" data-field="concepto"
      class="w-full border border-gray-200 rounded px-2 py-1 text-xs focus:ring-1 focus:ring-blue-400" /></td>
    <td class="px-2 py-1"><button type="button" class="text-red-400 hover:text-red-600 text-xs"
      data-eliminar>✕</button></td>`;

  tr.querySelectorAll('[data-field]').forEach(el =>
    el.addEventListener('input', () => actualizarPartida(idx, tr)));
  tr.querySelector('[data-eliminar]').addEventListener('click', () => {
    partidas[idx] = null;
    tr.remove();
    recalcular();
  });

  document.getElementById('tabla-partidas').appendChild(tr);
}

function actualizarPartida(idx, tr) {
  partidas[idx] = {
    idCuenta: parseInt(tr.querySelector('[data-field="idCuenta"]').value) || 0,
    debe:     parseFloat(tr.querySelector('[data-field="debe"]').value) || 0,
    haber:    parseFloat(tr.querySelector('[data-field="haber"]').value) || 0,
    concepto: tr.querySelector('[data-field="concepto"]').value,
  };
  recalcular();
}

function recalcular() {
  const activas = partidas.filter(Boolean);
  const debe  = activas.reduce((s, p) => s + p.debe, 0);
  const haber = activas.reduce((s, p) => s + p.haber, 0);

  document.getElementById('total-debe').textContent  = fmt(debe);
  document.getElementById('total-haber').textContent = fmt(haber);

  const estado = document.getElementById('estado-cuadre');
  const btn    = document.getElementById('btn-guardar-asiento');

  if (!activas.length) {
    estado.textContent = 'Sin partidas';
    estado.className = 'text-gray-400 text-sm';
  } else if (Math.abs(debe - haber) < 0.005) {
    estado.textContent = '✓ Cuadra';
    estado.className = 'text-green-600 text-sm font-medium';
    btn.disabled = false;
  } else {
    estado.textContent = `⚠ Diferencia: ${fmt(Math.abs(debe - haber))}`;
    estado.className = 'text-red-600 text-sm font-medium';
    btn.disabled = true;
  }
}

async function guardarAsiento(e) {
  e.preventDefault();
  const msgError = document.getElementById('msg-error-asiento');
  msgError.classList.add('hidden');

  const activas = partidas.filter(Boolean).filter(p => p.idCuenta > 0);
  if (activas.length < 2) {
    msgError.textContent = 'El asiento debe tener al menos 2 partidas.';
    msgError.classList.remove('hidden');
    return;
  }

  try {
    await crearAsiento({
      fecha: document.getElementById('fecha').value,
      descripcion: document.getElementById('descripcion').value.trim(),
      partidas: activas
    });
    document.getElementById('modal-asiento').close();
    await cargar();
  } catch (err) {
    msgError.textContent = err.message ?? 'Error al registrar el asiento.';
    msgError.classList.remove('hidden');
  }
}

const fmt = n => '$ ' + n.toLocaleString('es-AR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
