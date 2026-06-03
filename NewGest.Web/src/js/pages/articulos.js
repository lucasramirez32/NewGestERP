/**
 * articulos.js — Lógica de la página Artículos (M10).
 * Reemplaza funcionalidad de CONSULA.SCX del sistema VFP.
 */

import { requireAuth } from '../auth.js';
import {
  getArticulos,
  getArticuloById,
  crearArticulo,
  actualizarArticulo,
  desactivarArticulo,
  getGruposArticulos,
  crearGrupoArticulo,
} from '../api/articulos.js';
import { formatMoneda, debounce, escapeHtml } from '../utils.js';

requireAuth();

// ─── Estado ──────────────────────────────────────────────────────────────────
let paginaActual = 1;
const PAGE_SIZE = 20;
let articuloEditandoId = null;
let gruposCache = [];

// ─── Referencias DOM ──────────────────────────────────────────────────────────
const tablaBody       = document.getElementById('tabla-articulos');
const searchInput     = document.getElementById('search-articulos');
const filtroGrupo     = document.getElementById('filtro-grupo');
const paginacion      = document.getElementById('paginacion-articulos');
const modalArticulo   = document.getElementById('modal-articulo');
const formArticulo    = document.getElementById('form-articulo');
const spinnerArticulo = document.getElementById('articulo-spinner');
const btnNuevoArt     = document.getElementById('btn-nuevo-articulo');
const modalGrupo      = document.getElementById('modal-grupo');
const formGrupo       = document.getElementById('form-grupo');
const btnNuevoGrupo   = document.getElementById('btn-nuevo-grupo');

// ─── Carga de grupos ──────────────────────────────────────────────────────────
async function cargarGrupos() {
  try {
    gruposCache = await getGruposArticulos();
    poblarSelectsGrupos();
  } catch (err) {
    console.error('Error al cargar grupos:', err);
  }
}

function poblarSelectsGrupos() {
  // Aplanar árbol para los selects
  const opciones = aplanarGrupos(gruposCache, '');

  // Select de filtro
  filtroGrupo.innerHTML = `<option value="">Todos los grupos</option>` +
    opciones.map(g => `<option value="${g.idGrupo}">${escapeHtml(g.label)}</option>`).join('');

  // Select en modal artículo
  const selectGrupoModal = formArticulo.querySelector('[name="idGrupo"]');
  selectGrupoModal.innerHTML = `<option value="">Seleccionar grupo...</option>` +
    opciones.map(g => `<option value="${g.idGrupo}">${escapeHtml(g.label)}</option>`).join('');

  // Select padre en modal grupo
  const selectPadre = formGrupo.querySelector('[name="idGrupoPadre"]');
  selectPadre.innerHTML = `<option value="">Sin padre (grupo raíz)</option>` +
    opciones.map(g => `<option value="${g.idGrupo}">${escapeHtml(g.label)}</option>`).join('');
}

function aplanarGrupos(grupos, prefijo) {
  const result = [];
  for (const g of grupos) {
    result.push({ idGrupo: g.idGrupo, label: prefijo + g.descripcion });
    if (g.subgrupos?.length) {
      result.push(...aplanarGrupos(g.subgrupos, prefijo + g.descripcion + ' › '));
    }
  }
  return result;
}

// ─── Carga de artículos ───────────────────────────────────────────────────────
async function cargarArticulos() {
  tablaBody.innerHTML = `
    <tr><td colspan="6" class="px-4 py-8 text-center text-gray-400 text-sm">Cargando...</td></tr>`;

  try {
    const data = await getArticulos({
      search: searchInput.value.trim(),
      idGrupo: filtroGrupo.value,
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

function renderTabla(articulos) {
  if (!articulos.length) {
    tablaBody.innerHTML = `
      <tr><td colspan="6" class="px-4 py-8 text-center text-gray-400 text-sm">No se encontraron artículos.</td></tr>`;
    return;
  }

  tablaBody.innerHTML = articulos.map(a => {
    const ivaBadge = a.porcentajeIva === 0
      ? 'bg-gray-100 text-gray-600'
      : a.porcentajeIva === 21
        ? 'bg-blue-100 text-blue-700'
        : 'bg-indigo-100 text-indigo-700';

    return `
      <tr class="hover:bg-gray-50 transition-colors">
        <td class="px-4 py-3 font-mono text-xs text-gray-600 whitespace-nowrap">${escapeHtml(a.codigo.trim())}</td>
        <td class="px-4 py-3 font-medium text-gray-900">${escapeHtml(a.descripcion)}</td>
        <td class="px-4 py-3 text-sm text-gray-500">${escapeHtml(a.grupoDescripcion)}</td>
        <td class="px-4 py-3 text-right font-mono text-sm text-gray-800 whitespace-nowrap">
          ${formatMoneda(a.precioLista)}
        </td>
        <td class="px-4 py-3 text-center">
          <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${ivaBadge}">
            ${a.porcentajeIva}%
          </span>
        </td>
        <td class="px-4 py-3 text-right whitespace-nowrap">
          <button
            class="text-blue-600 hover:text-blue-800 text-sm font-medium transition-colors"
            data-edit="${a.idArticulo}">
            Editar
          </button>
          <button
            class="ml-3 text-red-500 hover:text-red-700 text-sm transition-colors"
            data-delete="${a.idArticulo}"
            data-desc="${escapeHtml(a.descripcion)}">
            Dar de baja
          </button>
        </td>
      </tr>`;
  }).join('');
}

function renderPaginacion(total, pageSize) {
  const totalPaginas = Math.ceil(total / pageSize);

  if (totalPaginas <= 1) {
    paginacion.innerHTML = `<span class="text-gray-500">${total} artículo${total !== 1 ? 's' : ''}</span>`;
    return;
  }

  paginacion.innerHTML = `
    <span class="text-gray-500">${total} artículo${total !== 1 ? 's' : ''} — página ${paginaActual} de ${totalPaginas}</span>
    <div class="flex gap-1">
      <button id="pag-art-prev"
        class="px-3 py-1 rounded border border-gray-300 text-sm ${paginaActual === 1 ? 'opacity-50 cursor-not-allowed' : 'hover:bg-gray-100'}"
        ${paginaActual === 1 ? 'disabled' : ''}>
        Anterior
      </button>
      <button id="pag-art-next"
        class="px-3 py-1 rounded border border-gray-300 text-sm ${paginaActual === totalPaginas ? 'opacity-50 cursor-not-allowed' : 'hover:bg-gray-100'}"
        ${paginaActual === totalPaginas ? 'disabled' : ''}>
        Siguiente
      </button>
    </div>`;

  document.getElementById('pag-art-prev')?.addEventListener('click', () => {
    if (paginaActual > 1) { paginaActual--; cargarArticulos(); }
  });
  document.getElementById('pag-art-next')?.addEventListener('click', () => {
    if (paginaActual < totalPaginas) { paginaActual++; cargarArticulos(); }
  });
}

// ─── Modal Artículo ───────────────────────────────────────────────────────────
function abrirModalNuevo() {
  articuloEditandoId = null;
  formArticulo.reset();
  limpiarErroresArticulo();
  formArticulo.querySelector('[name="codigo"]').readOnly = false;
  formArticulo.querySelector('[name="codigo"]').classList.remove('bg-gray-50');
  modalArticulo.open('Nuevo artículo');
}

async function abrirModalEditar(idArticulo) {
  articuloEditandoId = idArticulo;
  formArticulo.reset();
  limpiarErroresArticulo();
  modalArticulo.open('Editar artículo');

  try {
    const a = await getArticuloById(idArticulo);
    formArticulo.querySelector('[name="codigo"]').value      = a.codigo?.trim() ?? '';
    formArticulo.querySelector('[name="descripcion"]').value = a.descripcion ?? '';
    formArticulo.querySelector('[name="idGrupo"]').value     = a.idGrupo;
    formArticulo.querySelector('[name="idUnidad"]').value    = a.idUnidad;
    formArticulo.querySelector('[name="precioLista"]').value = a.precioLista;
    formArticulo.querySelector('[name="precioCosto"]').value = a.precioCosto;
    formArticulo.querySelector('[name="porcentajeIva"]').value = a.porcentajeIva;
    formArticulo.querySelector('[name="observaciones"]').value = a.observaciones ?? '';

    // Código no editable en actualización
    formArticulo.querySelector('[name="codigo"]').readOnly = true;
    formArticulo.querySelector('[name="codigo"]').classList.add('bg-gray-50');
  } catch (err) {
    modalArticulo.close();
    alert('Error al cargar el artículo: ' + err.message);
  }
}

async function guardarArticulo(e) {
  e.preventDefault();
  if (!validarFormularioArticulo()) return;

  const dto = {
    codigo:       formArticulo.querySelector('[name="codigo"]').value.trim().toUpperCase(),
    descripcion:  formArticulo.querySelector('[name="descripcion"]').value.trim(),
    idGrupo:      parseInt(formArticulo.querySelector('[name="idGrupo"]').value, 10),
    idUnidad:     parseInt(formArticulo.querySelector('[name="idUnidad"]').value, 10),
    precioLista:  parseFloat(formArticulo.querySelector('[name="precioLista"]').value) || 0,
    precioCosto:  parseFloat(formArticulo.querySelector('[name="precioCosto"]').value) || 0,
    porcentajeIva: parseFloat(formArticulo.querySelector('[name="porcentajeIva"]').value),
    observaciones: formArticulo.querySelector('[name="observaciones"]').value.trim() || null,
  };

  spinnerArticulo.classList.remove('hidden');
  try {
    if (articuloEditandoId) {
      await actualizarArticulo(articuloEditandoId, dto);
    } else {
      await crearArticulo(dto);
    }
    modalArticulo.close();
    paginaActual = 1;
    cargarArticulos();
  } catch (err) {
    alert(err.message);
  } finally {
    spinnerArticulo.classList.add('hidden');
  }
}

function validarFormularioArticulo() {
  let valido = true;

  if (!articuloEditandoId) {
    const codigoEl = formArticulo.querySelector('[name="codigo"]');
    if (!codigoEl.value.trim()) {
      mostrarErrorArticulo('codigo', 'El código es requerido.'); valido = false;
    }
  }

  const descEl = formArticulo.querySelector('[name="descripcion"]');
  if (!descEl.value.trim()) {
    mostrarErrorArticulo('descripcion', 'La descripción es requerida.'); valido = false;
  }

  const grupoEl = formArticulo.querySelector('[name="idGrupo"]');
  if (!grupoEl.value) {
    mostrarErrorArticulo('idGrupo', 'Debe seleccionar un grupo.'); valido = false;
  }

  const precioEl = formArticulo.querySelector('[name="precioLista"]');
  if (precioEl.value === '' || parseFloat(precioEl.value) < 0) {
    mostrarErrorArticulo('precioLista', 'Ingrese un precio válido.'); valido = false;
  }

  return valido;
}

function mostrarErrorArticulo(campo, mensaje) {
  const el = formArticulo.querySelector(`[data-for="${campo}"]`);
  if (el) { el.textContent = mensaje; el.classList.remove('hidden'); }
}

function limpiarErroresArticulo() {
  formArticulo.querySelectorAll('.form-error').forEach(el => el.classList.add('hidden'));
}

// ─── Modal Grupo ──────────────────────────────────────────────────────────────
async function guardarGrupo(e) {
  e.preventDefault();
  const descripcion = formGrupo.querySelector('[name="descripcion"]').value.trim();
  if (!descripcion) {
    const errEl = formGrupo.querySelector('[data-for="descripcion-grupo"]');
    if (errEl) errEl.classList.remove('hidden');
    return;
  }

  const idGrupoPadreVal = formGrupo.querySelector('[name="idGrupoPadre"]').value;
  const dto = {
    idEmpresa:    0, // Se reemplaza en el backend con el claim JWT
    descripcion,
    idGrupoPadre: idGrupoPadreVal ? parseInt(idGrupoPadreVal, 10) : null,
  };

  try {
    await crearGrupoArticulo(dto);
    modalGrupo.close();
    await cargarGrupos();
  } catch (err) {
    alert(err.message);
  }
}

// ─── Event listeners ──────────────────────────────────────────────────────────
btnNuevoArt.addEventListener('click', abrirModalNuevo);
btnNuevoGrupo.addEventListener('click', () => { formGrupo.reset(); modalGrupo.open('Nuevo grupo'); });
formArticulo.addEventListener('submit', guardarArticulo);
formGrupo.addEventListener('submit', guardarGrupo);

tablaBody.addEventListener('click', (e) => {
  const editBtn   = e.target.closest('[data-edit]');
  const deleteBtn = e.target.closest('[data-delete]');

  if (editBtn) abrirModalEditar(parseInt(editBtn.dataset.edit, 10));

  if (deleteBtn) {
    const desc = deleteBtn.dataset.desc;
    const id   = parseInt(deleteBtn.dataset.delete, 10);
    if (confirm(`¿Dar de baja el artículo "${desc}"?\nEsta acción es reversible.`)) {
      desactivarArticulo(id)
        .then(() => cargarArticulos())
        .catch(err => alert(err.message));
    }
  }
});

filtroGrupo.addEventListener('change', () => { paginaActual = 1; cargarArticulos(); });
const buscarDebounced = debounce(() => { paginaActual = 1; cargarArticulos(); }, 300);
searchInput.addEventListener('input', buscarDebounced);

// ─── Inicialización ───────────────────────────────────────────────────────────
Promise.all([cargarGrupos(), cargarArticulos()]);
