/**
 * clientes.js — Lógica de la página Clientes (M08).
 * Reemplaza funcionalidad de CONSULTE.SCX del sistema VFP.
 */

import { requireAuth } from '../auth.js';
import { getClientes, getClienteById, crearCliente, actualizarCliente, desactivarCliente, getAnalitico, getHistoricoArticulos } from '../api/clientes.js';
import { validarCuit, formatCuit, debounce, escapeHtml } from '../utils.js';

// Verificar autenticación antes de todo
requireAuth();

// ─── Estado ──────────────────────────────────────────────────────────────────
let paginaActual = 1;
const PAGE_SIZE = 20;
let clienteEditandoId = null;
let historicoArticulos = []; // Guardar en memoria para filtrado rápido

// ─── Referencias DOM ──────────────────────────────────────────────────────────
const tablaBody      = document.getElementById('tabla-clientes');
const searchInput    = document.getElementById('search-clientes');
const filtroIva      = document.getElementById('filtro-condicion-iva');
const paginacion     = document.getElementById('paginacion-clientes');
const modal          = document.getElementById('modal-cliente');
const form           = document.getElementById('form-cliente');
const spinner        = document.getElementById('cliente-spinner');
const btnNuevo       = document.getElementById('btn-nuevo-cliente');
const cuitInput      = form.querySelector('[name="cuit"]');
const cuitError      = document.getElementById('cuit-error');

// ─── Badges de condición IVA ──────────────────────────────────────────────────
const IVA_BADGE = {
  Inscripto:       'bg-blue-100 text-blue-800',
  Exento:          'bg-gray-100 text-gray-700',
  Monotributo:     'bg-green-100 text-green-800',
  ConsumidorFinal: 'bg-yellow-100 text-yellow-800',
  NoInscripto:     'bg-red-100 text-red-800',
};

// ─── Lógica de Pestañas (Tabs) ─────────────────────────────────────────────────
const tabContainer = document.getElementById('cliente-tabs');
const tabButtons = tabContainer.querySelectorAll('button[data-tab]');
const tabPanes = document.querySelectorAll('.tab-pane');

function seleccionarTab(tabName) {
  tabButtons.forEach(btn => {
    const active = btn.dataset.tab === tabName;
    btn.classList.toggle('border-blue-500', active);
    btn.classList.toggle('text-blue-600', active);
    btn.classList.toggle('font-semibold', active);
    btn.classList.toggle('border-transparent', !active);
    btn.classList.toggle('text-gray-500', !active);
    btn.classList.toggle('font-medium', !active);
  });

  tabPanes.forEach(pane => {
    pane.classList.toggle('hidden', pane.id !== `tab-content-${tabName}`);
  });
}

tabButtons.forEach(btn => {
  btn.addEventListener('click', () => seleccionarTab(btn.dataset.tab));
});

// ─── Carga de datos ───────────────────────────────────────────────────────────
async function cargarClientes() {
  tablaBody.innerHTML = `
    <tr><td colspan="6" class="px-4 py-8 text-center text-gray-400 text-sm">Cargando...</td></tr>`;

  try {
    const data = await getClientes({
      search: searchInput.value.trim(),
      condicionIva: filtroIva.value,
      page: paginaActual,
      pageSize: PAGE_SIZE,
    });

    renderTabla(data.items);
    renderPaginacion(data.total, data.pageSize);
  } catch (err) {
    tablaBody.innerHTML = `
      <tr><td colspan="6" class="px-4 py-8 text-center text-red-500 text-sm">${escapeHtml(err.message)}</td></tr>`;
  }
}

function renderTabla(clientes) {
  if (!clientes.length) {
    tablaBody.innerHTML = `
      <tr><td colspan="6" class="px-4 py-8 text-center text-gray-400 text-sm">No se encontraron clientes.</td></tr>`;
    return;
  }

  tablaBody.innerHTML = clientes.map(c => {
    const badgeClass = IVA_BADGE[c.condicionIvaDescripcion] ?? 'bg-gray-100 text-gray-700';
    const cuitFormateado = formatCuit(c.cuit ?? '');

    return `
      <tr class="hover:bg-gray-50 transition-colors">
        <td class="px-4 py-3 font-mono text-xs text-gray-600 whitespace-nowrap">${escapeHtml(c.codigo.trim())}</td>
        <td class="px-4 py-3 font-medium text-gray-900">
          ${escapeHtml(c.razonSocial)}
          ${c.nombreFantasia ? `<span class="block text-xs font-normal text-gray-400 mt-0.5">${escapeHtml(c.nombreFantasia)}</span>` : ''}
        </td>
        <td class="px-4 py-3 font-mono text-sm text-gray-600">${escapeHtml(cuitFormateado)}</td>
        <td class="px-4 py-3">
          <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${badgeClass}">
            ${escapeHtml(c.condicionIvaDescripcion)}
          </span>
        </td>
        <td class="px-4 py-3 text-sm text-gray-500">${escapeHtml(c.localidad ?? '')}</td>
        <td class="px-4 py-3 text-right whitespace-nowrap">
          <button
            class="text-blue-600 hover:text-blue-800 text-sm font-medium transition-colors"
            data-edit="${c.idCliente}">
            Editar
          </button>
          <button
            class="ml-3 text-red-500 hover:text-red-700 text-sm transition-colors"
            data-delete="${c.idCliente}"
            data-nombre="${escapeHtml(c.razonSocial)}">
            Dar de baja
          </button>
        </td>
      </tr>`;
  }).join('');
}

function renderPaginacion(total, pageSize) {
  const totalPaginas = Math.ceil(total / pageSize);

  if (totalPaginas <= 1) {
    paginacion.innerHTML = `<span class="text-gray-500">${total} cliente${total !== 1 ? 's' : ''}</span>`;
    return;
  }

  paginacion.innerHTML = `
    <span class="text-gray-500">${total} cliente${total !== 1 ? 's' : ''} — página ${paginaActual} de ${totalPaginas}</span>
    <div class="flex gap-1">
      <button id="pag-prev"
        class="px-3 py-1 rounded border border-gray-300 text-sm ${paginaActual === 1 ? 'opacity-50 cursor-not-allowed' : 'hover:bg-gray-100'}"
        ${paginaActual === 1 ? 'disabled' : ''}>
        Anterior
      </button>
      <button id="pag-next"
        class="px-3 py-1 rounded border border-gray-300 text-sm ${paginaActual === totalPaginas ? 'opacity-50 cursor-not-allowed' : 'hover:bg-gray-100'}"
        ${paginaActual === totalPaginas ? 'disabled' : ''}>
        Siguiente
      </button>
    </div>`;

  document.getElementById('pag-prev')?.addEventListener('click', () => {
    if (paginaActual > 1) { paginaActual--; cargarClientes(); }
  });
  document.getElementById('pag-next')?.addEventListener('click', () => {
    if (paginaActual < totalPaginas) { paginaActual++; cargarClientes(); }
  });
}

// ─── Modal ABM ────────────────────────────────────────────────────────────────
function abrirModalNuevo() {
  clienteEditandoId = null;
  form.reset();
  limpiarErrores();
  
  // Ocultar pestañas de sólo lectura
  document.getElementById('tab-btn-analitico').classList.add('hidden');
  document.getElementById('tab-btn-historico').classList.add('hidden');

  seleccionarTab('catalogo');
  modal.open('Nuevo cliente');
}

async function abrirModalEditar(idCliente) {
  clienteEditandoId = idCliente;
  form.reset();
  limpiarErrores();

  // Mostrar pestañas de sólo lectura
  document.getElementById('tab-btn-analitico').classList.remove('hidden');
  document.getElementById('tab-btn-historico').classList.remove('hidden');

  seleccionarTab('catalogo');
  modal.open('Editar cliente');

  try {
    const c = await getClienteById(idCliente);
    form.querySelector('[name="codigo"]').value      = c.codigo?.trim() ?? '';
    form.querySelector('[name="razonSocial"]').value = c.razonSocial ?? '';
    form.querySelector('[name="cuit"]').value        = formatCuit(c.cuit ?? '');
    form.querySelector('[name="condicionIva"]').value = c.condicionIva;
    form.querySelector('[name="domicilio"]').value   = c.domicilio ?? '';
    form.querySelector('[name="localidad"]').value   = c.localidad ?? '';
    form.querySelector('[name="telefono"]').value    = c.telefono ?? '';
    form.querySelector('[name="email"]').value       = c.email ?? '';
    form.querySelector('[name="observaciones"]').value = c.observaciones ?? '';

    // Nuevos campos
    form.querySelector('[name="nombreFantasia"]').value = c.nombreFantasia ?? '';
    form.querySelector('[name="limiteCredito"]').value = c.limiteCredito ?? 0;
    form.querySelector('[name="diasMora"]').value = c.diasMora ?? 0;
    form.querySelector('[name="descuento"]').value = c.descuento ?? 0;
    form.querySelector('[name="provincia"]').value = c.provincia ?? '';
    form.querySelector('[name="codigoPostal"]').value = c.codigoPostal ?? '';

    // Ficha Médica
    form.querySelector('[name="obraSocial"]').value = c.obraSocial ?? '';
    form.querySelector('[name="nroAfiliado"]').value = c.nroAfiliado ?? '';
    form.querySelector('[name="medicoCabecera"]').value = c.medicoCabecera ?? '';
    form.querySelector('[name="matriculaMedico"]').value = c.matriculaMedico ?? '';
    form.querySelector('[name="alergia"]').checked = c.alergia || false;
    form.querySelector('[name="alergias"]').value = c.alergias ?? '';
    form.querySelector('[name="tratamiento"]').checked = c.tratamiento || false;
    form.querySelector('[name="convulsiones"]').checked = c.convulsiones || false;
    form.querySelector('[name="medicacion"]').value = c.medicacion ?? '';
    form.querySelector('[name="patologia"]').value = c.patologia ?? '';

    // En edición el código no se puede cambiar
    form.querySelector('[name="codigo"]').readOnly = true;
    form.querySelector('[name="codigo"]').classList.add('bg-gray-50');

    // Cargar información relacionada
    cargarCuentaCorriente(idCliente);
    cargarHistoricoArticulos(idCliente);

  } catch (err) {
    modal.close();
    alert('Error al cargar el cliente: ' + err.message);
  }
}

async function cargarCuentaCorriente(idCliente) {
  const tableBody = document.getElementById('analitico-tabla-body');
  tableBody.innerHTML = `<tr><td colspan="6" class="px-4 py-8 text-center text-gray-400 text-sm">Cargando movimientos...</td></tr>`;

  try {
    const data = await getAnalitico(idCliente);

    if (!data.movimientos || !data.movimientos.length) {
      tableBody.innerHTML = `<tr><td colspan="6" class="px-4 py-8 text-center text-gray-400 text-sm">Sin movimientos registrados.</td></tr>`;
    } else {
      tableBody.innerHTML = data.movimientos.map(m => {
        const fecha = new Date(m.fecha).toLocaleDateString('es-AR');
        const debe = m.debe > 0 ? `$${m.debe.toLocaleString('es-AR', { minimumFractionDigits: 2 })}` : '-';
        const haber = m.haber > 0 ? `$${m.haber.toLocaleString('es-AR', { minimumFractionDigits: 2 })}` : '-';
        const saldo = `$${m.saldo.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`;
        return `
          <tr class="hover:bg-gray-50 transition-colors">
            <td class="px-4 py-2">${escapeHtml(fecha)}</td>
            <td class="px-4 py-2"><span class="font-semibold text-gray-700">${escapeHtml(m.tipo)}</span></td>
            <td class="px-4 py-2 font-mono text-xs text-gray-600">${escapeHtml(m.numero)}</td>
            <td class="px-4 py-2 text-right text-gray-900">${debe}</td>
            <td class="px-4 py-2 text-right text-gray-900">${haber}</td>
            <td class="px-4 py-2 text-right font-semibold ${m.saldo >= 0 ? 'text-gray-900' : 'text-red-600'}">${saldo}</td>
          </tr>`;
      }).join('');
    }

    document.getElementById('analitico-saldo-cta').textContent = `$${data.saldoCtaCte.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`;
    document.getElementById('analitico-saldo-favor').textContent = `$${data.saldoAFavor.toLocaleString('es-AR', { minimumFractionDigits: 2 })}`;

  } catch (err) {
    tableBody.innerHTML = `<tr><td colspan="6" class="px-4 py-8 text-center text-red-500 text-sm">Error: ${escapeHtml(err.message)}</td></tr>`;
  }
}

async function cargarHistoricoArticulos(idCliente) {
  const tableBody = document.getElementById('historico-tabla-body');
  tableBody.innerHTML = `<tr><td colspan="7" class="px-4 py-8 text-center text-gray-400 text-sm">Cargando histórico...</td></tr>`;

  try {
    historicoArticulos = await getHistoricoArticulos(idCliente);
    renderHistoricoArticulos(historicoArticulos);
  } catch (err) {
    tableBody.innerHTML = `<tr><td colspan="7" class="px-4 py-8 text-center text-red-500 text-sm">Error: ${escapeHtml(err.message)}</td></tr>`;
  }
}

function renderHistoricoArticulos(items) {
  const tableBody = document.getElementById('historico-tabla-body');
  if (!items || !items.length) {
    tableBody.innerHTML = `<tr><td colspan="7" class="px-4 py-8 text-center text-gray-400 text-sm">Sin artículos comprados históricamente.</td></tr>`;
    return;
  }

  tableBody.innerHTML = items.map(i => {
    const fecha = new Date(i.fecha).toLocaleDateString('es-AR');
    return `
      <tr class="hover:bg-gray-50 transition-colors">
        <td class="px-4 py-2">${escapeHtml(fecha)}</td>
        <td class="px-4 py-2 font-mono text-xs text-gray-600 whitespace-nowrap">${escapeHtml(i.codigoArticulo)}</td>
        <td class="px-4 py-2 font-medium text-gray-900">${escapeHtml(i.descripcionArticulo)}</td>
        <td class="px-4 py-2 text-right">${i.cantidad.toLocaleString('es-AR')}</td>
        <td class="px-4 py-2 text-right">$${i.precioUnitario.toLocaleString('es-AR', { minimumFractionDigits: 2 })}</td>
        <td class="px-4 py-2 text-right font-medium text-gray-950">$${i.subtotal.toLocaleString('es-AR', { minimumFractionDigits: 2 })}</td>
        <td class="px-4 py-2 text-gray-500 whitespace-nowrap">${escapeHtml(i.comprobanteInfo)}</td>
      </tr>`;
  }).join('');
}

async function guardarCliente(e) {
  e.preventDefault();
  if (!validarFormulario()) return;

  const cuitRaw = form.querySelector('[name="cuit"]').value.trim();
  const dto = {
    codigo:       form.querySelector('[name="codigo"]').value.trim().toUpperCase(),
    razonSocial:  form.querySelector('[name="razonSocial"]').value.trim(),
    cuit:         cuitRaw || null,
    condicionIva: parseInt(form.querySelector('[name="condicionIva"]').value, 10),
    domicilio:    form.querySelector('[name="domicilio"]').value.trim() || null,
    localidad:    form.querySelector('[name="localidad"]').value.trim() || null,
    telefono:     form.querySelector('[name="telefono"]').value.trim() || null,
    email:        form.querySelector('[name="email"]').value.trim() || null,
    idZona:       null,
    observaciones: form.querySelector('[name="observaciones"]').value.trim() || null,

    // Nuevos campos comerciales
    nombreFantasia: form.querySelector('[name="nombreFantasia"]').value.trim() || null,
    limiteCredito:  parseFloat(form.querySelector('[name="limiteCredito"]').value) || 0,
    diasMora:       parseInt(form.querySelector('[name="diasMora"]').value, 10) || 0,
    descuento:      parseFloat(form.querySelector('[name="descuento"]').value) || 0,
    provincia:      form.querySelector('[name="provincia"]').value.trim() || null,
    codigoPostal:   form.querySelector('[name="codigoPostal"]').value.trim() || null,

    // Ficha médica
    obraSocial:     form.querySelector('[name="obraSocial"]').value.trim() || null,
    nroAfiliado:    form.querySelector('[name="nroAfiliado"]').value.trim() || null,
    medicoCabecera: form.querySelector('[name="medicoCabecera"]').value.trim() || null,
    matriculaMedico: form.querySelector('[name="matriculaMedico"]').value.trim() || null,
    alergia:        form.querySelector('[name="alergia"]').checked,
    alergias:       form.querySelector('[name="alergias"]').value.trim() || null,
    tratamiento:    form.querySelector('[name="tratamiento"]').checked,
    convulsiones:   form.querySelector('[name="convulsiones"]').checked,
    medicacion:     form.querySelector('[name="medicacion"]').value.trim() || null,
    patologia:      form.querySelector('[name="patologia"]').value.trim() || null
  };

  spinner.classList.remove('hidden');
  try {
    if (clienteEditandoId) {
      await actualizarCliente(clienteEditandoId, dto);
    } else {
      await crearCliente(dto);
    }
    modal.close();
    paginaActual = 1;
    cargarClientes();
  } catch (err) {
    alert(err.message);
  } finally {
    spinner.classList.add('hidden');
  }
}

function validarFormulario() {
  let valido = true;

  // Código (solo en creación)
  if (!clienteEditandoId) {
    const codigoEl = form.querySelector('[name="codigo"]');
    if (!codigoEl.value.trim()) {
      mostrarError('codigo', 'El código es requerido.');
      valido = false;
    }
  }

  // Razón Social
  const rsEl = form.querySelector('[name="razonSocial"]');
  if (!rsEl.value.trim()) {
    mostrarError('razonSocial', 'La razón social es requerida.');
    valido = false;
  }

  // Condición IVA
  const ivaEl = form.querySelector('[name="condicionIva"]');
  if (!ivaEl.value) {
    mostrarError('condicionIva', 'Seleccione una condición de IVA.');
    valido = false;
  }

  // CUIT (si tiene valor, debe ser válido)
  const cuitVal = form.querySelector('[name="cuit"]').value.trim();
  if (cuitVal && !validarCuit(cuitVal)) {
    cuitError.classList.remove('hidden');
    valido = false;
  }

  // Si hay algún error, ir automáticamente a la pestaña Catálogo para mostrarlo
  if (!valido) {
    seleccionarTab('catalogo');
  }

  return valido;
}

function mostrarError(campo, mensaje) {
  const el = form.querySelector(`[data-for="${campo}"]`);
  if (el) { el.textContent = mensaje; el.classList.remove('hidden'); }
}

function limpiarErrores() {
  form.querySelectorAll('.form-error').forEach(el => el.classList.add('hidden'));
  cuitError.classList.add('hidden');
  const codigoInput = form.querySelector('[name="codigo"]');
  codigoInput.readOnly = false;
  codigoInput.classList.remove('bg-gray-50');
}

// ─── Event listeners ──────────────────────────────────────────────────────────
btnNuevo.addEventListener('click', abrirModalNuevo);
form.addEventListener('submit', guardarCliente);

// Filtrado del historial de artículos
document.getElementById('search-historico-articulos')?.addEventListener('input', (e) => {
  const query = e.target.value.toLowerCase().trim();
  if (!query) {
    renderHistoricoArticulos(historicoArticulos);
    return;
  }
  const filtrados = historicoArticulos.filter(item => 
    item.codigoArticulo.toLowerCase().includes(query) || 
    item.descripcionArticulo.toLowerCase().includes(query)
  );
  renderHistoricoArticulos(filtrados);
});

// Validación CUIT en tiempo real
cuitInput.addEventListener('input', () => {
  const val = cuitInput.value.trim();
  if (!val) { cuitError.classList.add('hidden'); return; }
  cuitError.classList.toggle('hidden', validarCuit(val));
});

// Delegación de eventos en la tabla
tablaBody.addEventListener('click', (e) => {
  const editBtn   = e.target.closest('[data-edit]');
  const deleteBtn = e.target.closest('[data-delete]');

  if (editBtn) {
    abrirModalEditar(parseInt(editBtn.dataset.edit, 10));
  }

  if (deleteBtn) {
    const nombre = deleteBtn.dataset.nombre;
    const id     = parseInt(deleteBtn.dataset.delete, 10);
    if (confirm(`¿Dar de baja al cliente "${nombre}"?\nEsta acción es reversible desde la administración.`)) {
      desactivarCliente(id)
        .then(() => cargarClientes())
        .catch(err => alert(err.message));
    }
  }
});

// Búsqueda con debounce
const buscarDebounced = debounce(() => { paginaActual = 1; cargarClientes(); }, 300);
searchInput.addEventListener('input', buscarDebounced);
filtroIva.addEventListener('change', () => { paginaActual = 1; cargarClientes(); });

// ─── Inicialización ───────────────────────────────────────────────────────────
cargarClientes();
