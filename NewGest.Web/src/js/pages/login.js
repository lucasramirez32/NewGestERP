/**
 * pages/login.js — Lógica de la pantalla de login.
 */
import { setToken, setUser, debeResetearPassword } from '../auth.js';
import { login, getEmpresas } from '../api/auth.js';

// ─── Referencias DOM ─────────────────────────────────────────────────────────
const form          = document.getElementById('form-login');
const selectEmpresa = document.getElementById('empresa');
const inputUsuario  = document.getElementById('usuario');
const inputPassword = document.getElementById('password');
const btnLogin      = document.getElementById('btn-login');
const spinner       = document.getElementById('login-spinner');
const btnText       = document.getElementById('login-btn-text');
const alertError    = document.getElementById('alert-error');
const alertErrorMsg = document.getElementById('alert-error-msg');
const togglePass    = document.getElementById('toggle-password');
const eyeOpen       = document.getElementById('eye-open');
const eyeClosed     = document.getElementById('eye-closed');

// ─── Toggle mostrar/ocultar contraseña ──────────────────────────────────────
togglePass.addEventListener('click', () => {
  const mostrar = inputPassword.type === 'password';
  inputPassword.type = mostrar ? 'text' : 'password';
  eyeOpen.classList.toggle('hidden', mostrar);
  eyeClosed.classList.toggle('hidden', !mostrar);
});

// ─── Cargar empresas ─────────────────────────────────────────────────────────
async function cargarEmpresas() {
  try {
    const empresas = await getEmpresas();
    selectEmpresa.innerHTML = '<option value="">Seleccione una empresa...</option>';
    empresas.forEach(e => {
      const opt = document.createElement('option');
      opt.value = e.idEmpresa;
      opt.textContent = e.nombre;
      selectEmpresa.appendChild(opt);
    });
    if (empresas.length === 1) {
      selectEmpresa.value = empresas[0].idEmpresa;
    }
  } catch (err) {
    selectEmpresa.innerHTML = '<option value="">Error al cargar empresas</option>';
    mostrarError('No se pudo conectar con el servidor. Verifique su conexión.');
  }
}

// ─── Validación inline ───────────────────────────────────────────────────────
function limpiarErrores() {
  document.querySelectorAll('.form-error').forEach(el => el.classList.add('hidden'));
  document.querySelectorAll('.form-input').forEach(el => el.classList.remove('error'));
  alertError.classList.add('hidden');
}

function mostrarError(mensaje, tipo = 'general') {
  if (tipo === 'general') {
    alertErrorMsg.textContent = mensaje;
    alertError.classList.remove('hidden');
  }
}

function validarFormulario() {
  let valido = true;

  if (!selectEmpresa.value) {
    document.getElementById('empresa-error').classList.remove('hidden');
    selectEmpresa.classList.add('error');
    valido = false;
  }
  if (!inputUsuario.value.trim()) {
    document.getElementById('usuario-error').classList.remove('hidden');
    inputUsuario.classList.add('error');
    valido = false;
  }
  if (!inputPassword.value) {
    document.getElementById('password-error').classList.remove('hidden');
    inputPassword.classList.add('error');
    valido = false;
  }
  return valido;
}

// ─── Submit ───────────────────────────────────────────────────────────────────
form.addEventListener('submit', async (e) => {
  e.preventDefault();
  limpiarErrores();

  if (!validarFormulario()) return;

  // Estado cargando
  btnLogin.disabled = true;
  spinner.classList.remove('hidden');
  btnText.textContent = 'Ingresando...';

  try {
    const resultado = await login({
      idEmpresa: parseInt(selectEmpresa.value, 10),
      nombreUsuario: inputUsuario.value.trim(),
      password: inputPassword.value,
    });

    // Guardar token y datos de usuario
    setToken(resultado.token);
    setUser({
      idUsuario: resultado.idUsuario,
      nombre: resultado.nombre,
      idEmpresa: resultado.idEmpresa,
      debeResetearPassword: resultado.debeResetearPassword,
    });

    // Si debe cambiar contraseña → redirigir a esa pantalla
    if (resultado.debeResetearPassword) {
      window.location.href = '/pages/cambiar-password.html';
    } else {
      window.location.href = '/pages/index.html';
    }

  } catch (err) {
    const msg = err.message ?? 'Error al iniciar sesión.';

    if (msg.toLowerCase().includes('bloqueada') || msg.toLowerCase().includes('bloqueado')) {
      mostrarError('Cuenta bloqueada por demasiados intentos. Contacte al administrador.');
    } else {
      mostrarError('Usuario o contraseña incorrectos.');
    }

    inputPassword.value = '';
    inputPassword.focus();
  } finally {
    btnLogin.disabled = false;
    spinner.classList.add('hidden');
    btnText.textContent = 'Ingresar';
  }
});

// ─── Init ─────────────────────────────────────────────────────────────────────
cargarEmpresas();
inputUsuario.focus();
