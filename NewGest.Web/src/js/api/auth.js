/**
 * js/api/auth.js — Fetch wrappers para endpoints de autenticación.
 */
import { apiFetch } from '../auth.js';

/**
 * Obtiene la lista de empresas activas (endpoint público).
 * @returns {Promise<Array<{idEmpresa: number, nombre: string, rut: string|null}>>}
 */
export async function getEmpresas() {
  return apiFetch('/api/auth/empresas');
}

/**
 * Hace login y retorna el JWT + datos del usuario.
 * @param {{idEmpresa: number, nombreUsuario: string, password: string}} dto
 * @returns {Promise<{token: string, idUsuario: number, nombre: string, idEmpresa: number, debeResetearPassword: boolean}>}
 */
export async function login(dto) {
  return apiFetch('/api/auth/login', {
    method: 'POST',
    body: JSON.stringify(dto),
  });
}

/**
 * Cambia la contraseña del usuario autenticado.
 * @param {{passwordActual: string, passwordNueva: string}} dto
 * @returns {Promise<null>}
 */
export async function cambiarPassword(dto) {
  return apiFetch('/api/auth/cambiar-password', {
    method: 'POST',
    body: JSON.stringify(dto),
  });
}
