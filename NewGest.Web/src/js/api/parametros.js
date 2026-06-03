/**
 * js/api/parametros.js — Fetch wrappers para parámetros del sistema.
 */
import { apiFetch } from '../auth.js';

/**
 * Obtiene todos los parámetros de la empresa actual.
 * @returns {Promise<Array<{idParametro: number, idEmpresa: number|null, clave: string, valor: string, descripcion: string}>>}
 */
export async function getParametros() {
  return apiFetch('/api/parametros');
}

/**
 * Actualiza el valor de un parámetro.
 * @param {string} clave
 * @param {string} valor
 * @returns {Promise<null>}
 */
export async function actualizarParametro(clave, valor) {
  return apiFetch(`/api/parametros/${encodeURIComponent(clave)}`, {
    method: 'PUT',
    body: JSON.stringify({ valor }),
  });
}
