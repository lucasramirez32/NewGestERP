/**
 * pages/admin/usuarios.js — Lógica de la pantalla de administración de usuarios.
 */
import { requireAuth, logout } from '../../auth.js';
import { NgAlert } from '../../components/ng-alert.js';
import { getUsuarios, crearUsuario, actualizarUsuario, resetearPassword } from '../../api/usuarios.js';
import { formatFechaHora, debounce } from '../../utils.js';

// Verificar autenticación
requireAuth();

// ─── Estado ──────────────────────────────────────────────────────────────────
let paginaActual = 1;
let usuarioEditandoId = null;
const PAGE_SIZE = 20;

// ─── Módulos disponibles para permisos ───────────────────────────────────────
const MODULOS = [
  { id: 'M02', nombre: 'Autenticación' },
  { id: 'M04', nombre: 'Facturación AFIP' },
  { id: 'M05', nombre: 'Facturación Blanca' },
  { id: 'M06', nombre: 'Cobranzas' },
  { id: 'M08', nombre: 'Clientes' },
  { id: 'M09', nombre: 'Stock' },
  { id: 'M10', nombre: 'Artículos' },
  { id: 'M11', nombre: 'Pedidos' },
  { id: 'M12', nombre: 'Contabilidad' },
  { id: 'M13', nombre: 'Personal' },
  { id: 'M15', nombre: 'Reportes' },
  { id: 'M18', nombre: 'Exportaciones' },
  { id: 'ADM', nombre: 'Administración' },
];

// ─── Referencias DOM ─────────────────────────────────────────────────────────
const tabla           = document.getElementById('tabla-usuarios');
const searchInput     = document.getElementById('search-usuarios');
const soloActivosChk  = document.getElementById('solo-activos');
const btnNuevo        = document.getElementById('btn-nuevo-usuario');
const modal           = document.getElementById('modal-usuario');
const form            = document.getElementById('form-usuario');
const listaPermisos   = document.getElementById('lista-permisos');
const btnReset        = document.getElementById('btn-resetear-password');
const btnGuardar      = document.getElementById('btn-guardar-usuario');
const guardarSpinner  = document.getElementById('guardar-spinner');
const seccionPassword = document.getElementById('seccion-password');
const seccionActivo   = document.getElementById('seccion-activo');

// ─── Inicialización ───────────────────────────────────────────────────────────
function inicializarPermisos() {
  listaPermisos.innerHTML = MODULOS.map(m => `
    <label class="flex items-center gap-2 text-sm text-gray-700 cursor-pointer">
      <input type="checkbox" name="permisos" value="${m.id}"
        class="rounded border-gray-300 text-brand-600" />
      ${m.nombre}
    </label>
  `).join('');
}

// ─── Cargar y renderizar ─────────────────────────────────────────────────────
async function cargarUsuarios() {
  tabla.innerHTML = `<tr><td colspan="6" class="px-4 py-8 text-center text-gray-400">Cargando...</td></tr>`;

  try {
    const data = await getUsuarios({
      search: searchInput.value,
      page: paginaActual,
      pageSize: PAGE_SIZE,
      soloActivos: soloActivosChk.checked,
    });

    if (!data?.items?.length) {
      tabla.innerHTML = `<tr><td colspan="6" class="px-4 py-8 text-center text-gray-400 text-sm">Sin resultados</td></tr>`;
      return;
    }

    tabla.innerHTML = data.items.map(u => `
      <tr class="hover:bg-gray-50 transition-colors" data-id="${u.id}">
        <td class="px-4 py-3 font-mono text-gray-700 text-sm">${escapeHtml(u.userName)}</td>
        <td class="px-4 py-3 text-gray-900">${escapeHtml(u.nombre)}</td>
        <td class="px-4 py-3 text-gray-500 text-sm">${escapeHtml(u.legajo ?? '—')}</td>
        <td class="px-4 py-3 text-gray-500 text-sm">${u.ultimoLogin ? formatFechaHora(u.ultimoLogin) : '—'}</td>
        <td class="px-4 py-3">
          ${u.bloqueado
            ? `<span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-red-100 text-red-800">Bloqueado</span>`
            : u.activo
              ? `<span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-800">Activo</span>`
              : `<span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-600">Inactivo</span>`
          }
        </td>
        <td class="px-4 py-3 text-right">
          <button class="text-brand-600 hover:text-brand-700 text-sm font-medium"
            data-edit="${u.id}">Editar</button>
        </td>
      </tr>
    `).join('');

    renderPaginacion(data.total, PAGE_SIZE);

  } catch (err) {
    tabla.innerHTML = `<tr><td colspan="6" class="px-4 py-8 text-center text-red-500 text-sm">${err.message}</td></tr>`;
  }
}

function renderPaginacion(total, pageSize) {
  const totalPages = Math.ceil(total / pageSize);
  const pag = document.getElementById('paginacion');
  if (!pag) return;

  pag.innerHTML = totalPages <= 1 ? '' : `
    <span>${total} usuarios · Página ${paginaActual} de ${totalPages}</span>
    <div class="flex gap-2">
      <button ${paginaActual === 1 ? 'disabled' : ''} id="pag-prev"
        class="btn-secondary px-3 py-1.5 text-xs disabled:opacity-40">Anterior</button>
      <button ${paginaActual >= totalPages ? 'disabled' : ''} id="pag-next"
        class="btn-secondary px-3 py-1.5 text-xs disabled:opacity-40">Siguiente</button>
    </div>
  `;

  document.getElementById('pag-prev')?.addEventListener('click', () => { paginaActual--; cargarUsuarios(); });
  document.getElementById('pag-next')?.addEventListener('click', () => { paginaActual++; cargarUsuarios(); });
}

// ─── Modal nuevo ─────────────────────────────────────────────────────────────
function abrirModalNuevo() {
  usuarioEditandoId = null;
  form.reset();
  seccionPassword.classList.remove('hidden');
  seccionActivo.classList.add('hidden');
  btnReset.classList.add('hidden');
  form.querySelectorAll('.form-error').forEach(el => el.classList.add('hidden'));
  modal.open('Nuevo usuario');
}

function abrirModalEdicion(usuario) {
  usuarioEditandoId = usuario.id;
  form.reset();
  form.querySelector('[name="nombre"]').value = usuario.nombre ?? '';
  form.querySelector('[name="userName"]').value = usuario.userName ?? '';
  form.querySelector('[name="legajo"]').value = usuario.legajo ?? '';
  // Marcar permisos del usuario
  form.querySelectorAll('[name="permisos"]').forEach(chk => {
    chk.checked = usuario.permisos?.includes(chk.value) ?? false;
  });
  // En edición: ocultar campo password, mostrar checkbox activo
  seccionPassword.classList.add('hidden');
  seccionActivo.classList.remove('hidden');
  form.querySelector('[name="activo"]').checked = usuario.activo ?? true;
  btnReset.classList.remove('hidden');
  form.querySelectorAll('.form-error').forEach(el => el.classList.add('hidden'));
  modal.open('Editar usuario');
}

// ─── Submit del formulario ────────────────────────────────────────────────────
form.addEventListener('submit', async (e) => {
  e.preventDefault();
  form.querySelectorAll('.form-error').forEach(el => el.classList.add('hidden'));

  const datos = {
    nombre:   form.querySelector('[name="nombre"]').value.trim(),
    userName: form.querySelector('[name="userName"]').value.trim(),
    legajo:   form.querySelector('[name="legajo"]').value.trim() || null,
    permisos: [...form.querySelectorAll('[name="permisos"]:checked')].map(c => c.value),
  };

  // Validación básica
  let valido = true;
  if (!datos.nombre) { validationError('nombre'); valido = false; }
  if (!datos.userName) { validationError('userName'); valido = false; }
  if (!usuarioEditandoId) {
    const pass = form.querySelector('[name="password"]').value;
    if (!pass || pass.length < 8 || !/\d/.test(pass)) {
      validationError('password'); valido = false;
    } else {
      datos.password = pass;
    }
  } else {
    datos.activo = form.querySelector('[name="activo"]').checked;
  }
  if (!valido) return;

  btnGuardar.disabled = true;
  guardarSpinner.classList.remove('hidden');

  try {
    if (usuarioEditandoId) {
      await actualizarUsuario(usuarioEditandoId, datos);
      NgAlert.mostrar('Usuario actualizado correctamente.', 'success');
    } else {
      await crearUsuario(datos);
      NgAlert.mostrar('Usuario creado correctamente.', 'success');
    }
    modal.close();
    cargarUsuarios();
  } catch (err) {
    NgAlert.mostrar(err.message ?? 'Error al guardar usuario.', 'error');
  } finally {
    btnGuardar.disabled = false;
    guardarSpinner.classList.add('hidden');
  }
});

function validationError(fieldName) {
  const errorEl = form.querySelector(`[data-for="${fieldName}"]`);
  if (errorEl) errorEl.classList.remove('hidden');
  const input = form.querySelector(`[name="${fieldName}"]`);
  if (input) input.classList.add('error');
}

// ─── Resetear contraseña ─────────────────────────────────────────────────────
btnReset.addEventListener('click', async () => {
  if (!usuarioEditandoId) return;
  if (!confirm('¿Confirma el reseteo de contraseña? El usuario deberá cambiarla en su próximo ingreso.')) return;
  try {
    await resetearPassword(usuarioEditandoId);
    NgAlert.mostrar('Contraseña reseteada. El usuario deberá cambiarla al ingresar.', 'success');
  } catch (err) {
    NgAlert.mostrar(err.message ?? 'Error al resetear contraseña.', 'error');
  }
});

// ─── Delegación de eventos en tabla ──────────────────────────────────────────
tabla.addEventListener('click', async (e) => {
  const editBtn = e.target.closest('[data-edit]');
  if (editBtn) {
    const id = parseInt(editBtn.dataset.edit, 10);
    // Por ahora usamos los datos ya cargados — en prod haría GET /api/usuarios/{id}
    NgAlert.mostrar('Cargando datos...', 'info', 1000);
    try {
      const { apiFetch } = await import('../../auth.js');
      const usuario = await apiFetch(`/api/usuarios/${id}`);
      abrirModalEdicion(usuario);
    } catch (err) {
      NgAlert.mostrar(err.message ?? 'Error al cargar el usuario.', 'error');
    }
  }
});

// ─── Eventos globales ────────────────────────────────────────────────────────
btnNuevo.addEventListener('click', abrirModalNuevo);
document.getElementById('btn-logout').addEventListener('click', () => logout());

const buscar = debounce(() => { paginaActual = 1; cargarUsuarios(); }, 300);
searchInput.addEventListener('input', buscar);
soloActivosChk.addEventListener('change', () => { paginaActual = 1; cargarUsuarios(); });

// ─── Escape HTML ─────────────────────────────────────────────────────────────
function escapeHtml(str) {
  if (!str) return '';
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

// ─── Init ─────────────────────────────────────────────────────────────────────
inicializarPermisos();
cargarUsuarios();
