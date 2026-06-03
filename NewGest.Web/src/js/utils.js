/**
 * utils.js — Helpers: CUIT, moneda, fechas
 * Misma lógica del backend CuitValidator.cs (algoritmo módulo 11).
 * Migrado de FUNCION.PRG líneas 595-664.
 */

// ─── CUIT / CUIL ─────────────────────────────────────────────────────────────
const MULTIPLICADORES_CUIT = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];

/**
 * Valida un CUIT/CUIL argentino.
 * Acepta formato con o sin guiones: '20-12345678-6' o '20123456786'.
 * @param {string} cuit
 * @returns {boolean}
 */
export function validarCuit(cuit) {
  if (!cuit) return false;
  cuit = cuit.replace(/[-\s]/g, '');
  if (cuit.length !== 11 || !/^\d+$/.test(cuit)) return false;

  const suma = MULTIPLICADORES_CUIT.reduce(
    (acc, m, i) => acc + m * parseInt(cuit[i], 10),
    0
  );
  const resto = suma % 11;
  const digito = resto === 0 ? 0 : resto === 1 ? 9 : 11 - resto;
  return digito === parseInt(cuit[10], 10);
}

/**
 * Formatea un CUIT de 11 dígitos como XX-XXXXXXXX-X.
 * @param {string} cuit
 * @returns {string}
 */
export function formatCuit(cuit) {
  if (!cuit) return '';
  const digits = cuit.replace(/[-\s]/g, '');
  if (digits.length !== 11) return cuit;
  return `${digits.slice(0, 2)}-${digits.slice(2, 10)}-${digits.slice(10)}`;
}

// ─── Moneda ───────────────────────────────────────────────────────────────────
/**
 * Formatea un número como moneda argentina (pesos).
 * @param {number} valor
 * @param {number} [decimales=2]
 * @returns {string}
 */
export function formatMoneda(valor, decimales = 2) {
  if (valor === null || valor === undefined || isNaN(valor)) return '';
  return new Intl.NumberFormat('es-AR', {
    style: 'currency',
    currency: 'ARS',
    minimumFractionDigits: decimales,
    maximumFractionDigits: decimales,
  }).format(valor);
}

/**
 * Formatea un número con separadores de miles.
 * @param {number} valor
 * @param {number} [decimales=2]
 * @returns {string}
 */
export function formatNumero(valor, decimales = 2) {
  if (valor === null || valor === undefined || isNaN(valor)) return '';
  return new Intl.NumberFormat('es-AR', {
    minimumFractionDigits: decimales,
    maximumFractionDigits: decimales,
  }).format(valor);
}

// ─── Fechas ───────────────────────────────────────────────────────────────────
/**
 * Formatea una fecha ISO como DD/MM/YYYY.
 * @param {string|Date|null} fecha
 * @returns {string}
 */
export function formatFecha(fecha) {
  if (!fecha) return '';
  const d = fecha instanceof Date ? fecha : new Date(fecha);
  if (isNaN(d.getTime())) return '';
  return new Intl.DateTimeFormat('es-AR', {
    day: '2-digit', month: '2-digit', year: 'numeric'
  }).format(d);
}

/**
 * Formatea una fecha+hora ISO como DD/MM/YYYY HH:mm.
 * @param {string|Date|null} fecha
 * @returns {string}
 */
export function formatFechaHora(fecha) {
  if (!fecha) return '';
  const d = fecha instanceof Date ? fecha : new Date(fecha);
  if (isNaN(d.getTime())) return '';
  return new Intl.DateTimeFormat('es-AR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit'
  }).format(d);
}

// ─── Otros ────────────────────────────────────────────────────────────────────
/**
 * Crea un debounce sobre una función.
 * @param {Function} fn
 * @param {number} ms
 * @returns {Function}
 */
export function debounce(fn, ms = 300) {
  let timer;
  return (...args) => {
    clearTimeout(timer);
    timer = setTimeout(() => fn(...args), ms);
  };
}

/**
 * Escapa HTML para prevenir XSS al insertar texto dinámico en innerHTML.
 * @param {string} str
 * @returns {string}
 */
export function escapeHtml(str) {
  if (!str) return '';
  return str
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');
}
