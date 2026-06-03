/**
 * pages/cambiar-password.js — Lógica del cambio de contraseña obligatorio.
 */
import { requireAuth, logout, getUser } from '../auth.js';
import { cambiarPassword } from '../api/auth.js';

// Redirigir si no hay sesión
requireAuth();

// ─── Referencias DOM ─────────────────────────────────────────────────────────
const form               = document.getElementById('form-password');
const inputActual        = document.getElementById('password-actual');
const inputNueva         = document.getElementById('password-nueva');
const inputConfirmar     = document.getElementById('password-confirmar');
const btnCambiar         = document.getElementById('btn-cambiar');
const btnCancelar        = document.getElementById('btn-cancelar');
const spinner            = document.getElementById('submit-spinner');
const btnText            = document.getElementById('btn-text');
const alertError         = document.getElementById('alert-error');
const alertErrorMsg      = document.getElementById('alert-error-msg');
const alertSuccess       = document.getElementById('alert-success');

// ─── Indicador de fortaleza de contraseña ────────────────────────────────────
function calcularFortaleza(password) {
  let puntos = 0;
  if (password.length >= 8)  puntos++;
  if (password.length >= 12) puntos++;
  if (/\d/.test(password))   puntos++;
  if (/[A-Z]/.test(password) && /[a-z]/.test(password)) puntos++;
  return puntos; // 0-4
}

const coloresFortaleza = ['', 'bg-red-400', 'bg-orange-400', 'bg-yellow-400', 'bg-green-500'];

inputNueva.addEventListener('input', () => {
  const nivel = calcularFortaleza(inputNueva.value);
  document.querySelectorAll('[data-level]').forEach(bar => {
    const barLevel = parseInt(bar.dataset.level, 10);
    bar.className = `h-1 flex-1 rounded ${barLevel <= nivel ? coloresFortaleza[nivel] : 'bg-gray-200'}`;
  });
});

// ─── Validación ───────────────────────────────────────────────────────────────
function limpiarErrores() {
  document.querySelectorAll('.form-error').forEach(el => el.classList.add('hidden'));
  document.querySelectorAll('.form-input').forEach(el => el.classList.remove('error'));
  alertError.classList.add('hidden');
}

function validarFormulario() {
  let valido = true;

  if (!inputActual.value) {
    document.getElementById('password-actual-error').classList.remove('hidden');
    inputActual.classList.add('error');
    valido = false;
  }

  const nueva = inputNueva.value;
  if (!nueva || nueva.length < 8 || !/\d/.test(nueva)) {
    document.getElementById('password-nueva-error').classList.remove('hidden');
    inputNueva.classList.add('error');
    valido = false;
  }

  if (nueva !== inputConfirmar.value) {
    document.getElementById('password-confirmar-error').classList.remove('hidden');
    inputConfirmar.classList.add('error');
    valido = false;
  }

  return valido;
}

// ─── Submit ───────────────────────────────────────────────────────────────────
form.addEventListener('submit', async (e) => {
  e.preventDefault();
  limpiarErrores();

  if (!validarFormulario()) return;

  btnCambiar.disabled = true;
  spinner.classList.remove('hidden');
  btnText.textContent = 'Guardando...';

  try {
    await cambiarPassword({
      passwordActual: inputActual.value,
      passwordNueva: inputNueva.value,
    });

    alertSuccess.classList.remove('hidden');
    form.reset();

    // Redirigir tras éxito
    setTimeout(() => {
      window.location.href = '/pages/index.html';
    }, 1500);

  } catch (err) {
    alertErrorMsg.textContent = err.message ?? 'No se pudo cambiar la contraseña.';
    alertError.classList.remove('hidden');
    inputActual.value = '';
    inputActual.focus();
  } finally {
    btnCambiar.disabled = false;
    spinner.classList.add('hidden');
    btnText.textContent = 'Cambiar contraseña';
  }
});

// ─── Cancelar ─────────────────────────────────────────────────────────────────
btnCancelar.addEventListener('click', () => logout());
