import { getEmpleados, crearEmpleado, actualizarEmpleado, desactivarEmpleado } from '../../api/empleados.js';

// ─── Estado ───────────────────────────────────────────────────────────────────
let paginaActual = 1;
const MULTIPLICADORES = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

function validarCuil(cuil) {
  cuil = cuil.replace(/[-\s]/g, '');
  if (cuil.length !== 11 || !/^\d+$/.test(cuil)) return false;
  const suma = MULTIPLICADORES.reduce((acc, m, i) => acc + m * parseInt(cuil[i]), 0);
  const resto = suma % 11;
  const digito = resto === 0 ? 0 : resto === 1 ? 9 : 11 - resto;
  return digito === parseInt(cuil[10]);
}

// ─── Carga de tabla ───────────────────────────────────────────────────────────
async function cargarEmpleados() {
  const tbody = document.getElementById('tabla-empleados');
  const search = document.getElementById('search').value;
  const soloVendedores = document.getElementById('solo-vendedores').checked ? true : null;
  try {
    const data = await getEmpleados({ search, soloVendedores, page: paginaActual });
    if (!data.items.length) {
      tbody.innerHTML = '<tr><td colspan="7" class="px-4 py-8 text-center text-gray-400 text-sm">Sin empleados</td></tr>';
      return;
    }
    tbody.innerHTML = data.items.map(e => `
      <tr class="hover:bg-gray-50">
        <td class="px-4 py-3 font-mono text-gray-700">${e.legajo.trim()}</td>
        <td class="px-4 py-3 text-gray-900">${e.apellidoNombre}</td>
        <td class="px-4 py-3 text-gray-600 font-mono">${formatCuil(e.cuil)}</td>
        <td class="px-4 py-3 text-gray-600">${e.rolDescripcion}</td>
        <td class="px-4 py-3 text-center">
          ${e.esVendedor ? '<span class="inline-flex px-2 py-0.5 rounded text-xs font-medium bg-blue-100 text-blue-800">SI</span>' : ''}
        </td>
        <td class="px-4 py-3 text-right">${e.comisionPorcentaje != null ? e.comisionPorcentaje + '%' : '—'}</td>
        <td class="px-4 py-3 text-right flex gap-2 justify-end">
          <button class="btn-editar text-blue-600 hover:text-blue-800 text-sm" data-id="${e.idEmpleado}">Editar</button>
          <button class="btn-desactivar text-red-600 hover:text-red-800 text-sm" data-id="${e.idEmpleado}">Dar de baja</button>
        </td>
      </tr>`).join('');

    renderPaginacion(data.total, data.pageSize, paginaActual, document.getElementById('paginacion'));
  } catch (err) {
    tbody.innerHTML = `<tr><td colspan="7" class="text-center text-red-500 py-4">${err.message}</td></tr>`;
  }
}

cargarEmpleados();

// ─── Event delegation tabla ───────────────────────────────────────────────────
document.getElementById('tabla-empleados').addEventListener('click', async e => {
  const btnEditar = e.target.closest('.btn-editar');
  const btnBaja = e.target.closest('.btn-desactivar');

  if (btnEditar) {
    const id = parseInt(btnEditar.dataset.id);
    // Buscar el empleado en el DOM para pre-llenar
    const row = btnEditar.closest('tr');
    const cells = row.querySelectorAll('td');
    abrirModal({
      idEmpleado: id,
      legajo: cells[0].textContent.trim(),
      apellidoNombre: cells[1].textContent.trim(),
      cuil: cells[2].textContent.trim().replace(/-/g, ''),
      rol: null // sin FK en DOM — se deja vacío para que el usuario reseleccione
    });
  }

  if (btnBaja) {
    const id = parseInt(btnBaja.dataset.id);
    if (!confirm('¿Dar de baja al empleado?')) return;
    try {
      await desactivarEmpleado(id);
      cargarEmpleados();
    } catch (err) {
      alert(err.message);
    }
  }
});

// ─── Búsqueda con debounce ────────────────────────────────────────────────────
let searchTimer;
document.getElementById('search').addEventListener('input', () => {
  clearTimeout(searchTimer);
  searchTimer = setTimeout(() => { paginaActual = 1; cargarEmpleados(); }, 300);
});
document.getElementById('solo-vendedores').addEventListener('change', () => { paginaActual = 1; cargarEmpleados(); });

// ─── Modal ABM ────────────────────────────────────────────────────────────────
const modal = document.getElementById('modal-empleado');
const form = document.getElementById('form-empleado');
const errorDiv = document.getElementById('form-error');

document.getElementById('btn-nuevo').addEventListener('click', () => abrirModal(null));
document.getElementById('modal-close').addEventListener('click', cerrarModal);
document.getElementById('btn-cancelar').addEventListener('click', cerrarModal);

// Mostrar/ocultar comisión según rol
form.querySelector('[name="rol"]').addEventListener('change', e => {
  document.getElementById('campo-comision').classList.toggle('hidden', e.target.value !== '1');
});

// Validar CUIL en tiempo real
form.querySelector('[name="cuil"]').addEventListener('input', e => {
  const cuil = e.target.value.replace(/[-\s]/g, '');
  const errEl = document.getElementById('cuil-error');
  errEl.classList.toggle('hidden', !cuil || validarCuil(cuil));
});

function abrirModal(empleado) {
  form.reset();
  errorDiv.classList.add('hidden');
  document.getElementById('campo-comision').classList.add('hidden');

  if (empleado) {
    document.getElementById('modal-titulo').textContent = 'Editar Empleado';
    form.querySelector('[name="idEmpleado"]').value = empleado.idEmpleado;
    form.querySelector('[name="legajo"]').value = empleado.legajo;
    form.querySelector('[name="legajo"]').disabled = true;  // no editable
    form.querySelector('[name="apellidoNombre"]').value = empleado.apellidoNombre;
    form.querySelector('[name="cuil"]').value = empleado.cuil ?? '';
  } else {
    document.getElementById('modal-titulo').textContent = 'Nuevo Empleado';
    form.querySelector('[name="legajo"]').disabled = false;
  }

  modal.classList.remove('hidden');
  modal.classList.add('flex');
}

function cerrarModal() {
  modal.classList.add('hidden');
  modal.classList.remove('flex');
  form.reset();
  form.querySelector('[name="legajo"]').disabled = false;
}

form.addEventListener('submit', async e => {
  e.preventDefault();
  errorDiv.classList.add('hidden');

  const data = Object.fromEntries(new FormData(form));
  const idEmpleado = data.idEmpleado ? parseInt(data.idEmpleado) : null;
  const esNuevo = !idEmpleado;

  const cuil = data.cuil?.replace(/[-\s]/g, '') || null;
  if (cuil && !validarCuil(cuil)) {
    errorDiv.textContent = 'CUIL inválido';
    errorDiv.classList.remove('hidden');
    return;
  }

  try {
    if (esNuevo) {
      await crearEmpleado({
        legajo: data.legajo,
        apellidoNombre: data.apellidoNombre,
        cuil,
        rol: parseInt(data.rol),
        comisionPorcentaje: data.comisionPorcentaje ? parseFloat(data.comisionPorcentaje) : null
      });
    } else {
      await actualizarEmpleado(idEmpleado, {
        apellidoNombre: data.apellidoNombre,
        cuil,
        rol: parseInt(data.rol),
        comisionPorcentaje: data.comisionPorcentaje ? parseFloat(data.comisionPorcentaje) : null
      });
    }
    cerrarModal();
    cargarEmpleados();
  } catch (err) {
    errorDiv.textContent = err.message;
    errorDiv.classList.remove('hidden');
  }
});

// ─── Helpers ──────────────────────────────────────────────────────────────────
function formatCuil(cuil) {
  if (!cuil || cuil.length !== 11) return cuil ?? '';
  return `${cuil.slice(0, 2)}-${cuil.slice(2, 10)}-${cuil.slice(10)}`;
}

function renderPaginacion(total, pageSize, page, container) {
  const totalPages = Math.ceil(total / pageSize);
  container.innerHTML = `
    <span>${total} empleados — Página ${page} de ${totalPages}</span>
    <div class="flex gap-2">
      <button class="px-3 py-1 rounded border text-sm ${page <= 1 ? 'opacity-40 cursor-not-allowed' : 'hover:bg-gray-100'}" ${page <= 1 ? 'disabled' : ''} id="btn-prev">Anterior</button>
      <button class="px-3 py-1 rounded border text-sm ${page >= totalPages ? 'opacity-40 cursor-not-allowed' : 'hover:bg-gray-100'}" ${page >= totalPages ? 'disabled' : ''} id="btn-next">Siguiente</button>
    </div>`;
  document.getElementById('btn-prev')?.addEventListener('click', () => { paginaActual--; cargarEmpleados(); });
  document.getElementById('btn-next')?.addEventListener('click', () => { paginaActual++; cargarEmpleados(); });
}
