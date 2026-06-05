import { getPlanCuentas, crearCuentaContable } from '../../api/contabilidad.js';

let cuentas = [];

document.getElementById('btn-nueva').addEventListener('click', () =>
  document.getElementById('modal-cuenta').showModal());
document.getElementById('form-cuenta').addEventListener('submit', guardarCuenta);
document.getElementById('buscar').addEventListener('input', filtrar);

cargar();

async function cargar() {
  cuentas = await getPlanCuentas();
  renderizar(cuentas);
}

function filtrar(e) {
  const q = e.target.value.toLowerCase();
  renderizar(cuentas.filter(c =>
    c.codigo.toLowerCase().includes(q) || c.descripcion.toLowerCase().includes(q)));
}

function renderizar(lista) {
  const tbody = document.getElementById('tabla-cuentas');
  const sinCuentas = document.getElementById('sin-cuentas');

  if (!lista.length) {
    tbody.innerHTML = '';
    sinCuentas.classList.remove('hidden');
    return;
  }
  sinCuentas.classList.add('hidden');

  tbody.innerHTML = lista.map(c => `
    <tr class="hover:bg-gray-50">
      <td class="px-4 py-3 font-mono text-gray-800 text-xs">${c.codigo}</td>
      <td class="px-4 py-3 text-gray-900" style="padding-left:${nivelPadding(c.codigo)}px">${c.descripcion}</td>
      <td class="px-4 py-3 text-gray-600 text-xs">${c.tipo}</td>
      <td class="px-4 py-3 text-gray-600 text-xs">${c.naturaleza}</td>
      <td class="px-4 py-3 text-center">${c.imputaDirectamente ? '✓' : ''}</td>
      <td class="px-4 py-3 text-center">
        <span class="px-2 py-0.5 rounded-full text-xs ${c.activa ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-400'}">
          ${c.activa ? 'Activa' : 'Inactiva'}
        </span>
      </td>
    </tr>`).join('');
}

function nivelPadding(codigo) {
  const nivel = (codigo.match(/\./g) || []).length;
  return 16 + nivel * 16;
}

async function guardarCuenta(e) {
  e.preventDefault();
  const msgError = document.getElementById('msg-error-cuenta');
  msgError.classList.add('hidden');

  try {
    const dto = {
      codigo: document.getElementById('codigo').value.trim(),
      descripcion: document.getElementById('descripcion').value.trim(),
      idCuentaPadre: parseInt(document.getElementById('id-padre').value) || null,
      naturaleza: parseInt(document.getElementById('naturaleza').value),
      tipo: parseInt(document.getElementById('tipo').value),
      imputaDirectamente: document.getElementById('imputa').checked,
    };
    await crearCuentaContable(dto);
    document.getElementById('modal-cuenta').close();
    document.getElementById('form-cuenta').reset();
    await cargar();
  } catch (err) {
    msgError.textContent = err.message ?? 'Error al guardar.';
    msgError.classList.remove('hidden');
  }
}
