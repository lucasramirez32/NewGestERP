/**
 * js/api/usuarios.js — Fetch wrappers para gestión de usuarios.
 */
import { apiFetch } from '../auth.js';

/**
 * Lista usuarios de la empresa actual con paginación.
 * @param {{search?: string, page?: number, pageSize?: number, soloActivos?: boolean}} params
 */
export async function getUsuarios({ search = '', page = 1, pageSize = 20, soloActivos = true } = {}) {
  const params = new URLSearchParams({
    search,
    page: String(page),
    pageSize: String(pageSize),
    soloActivos: String(soloActivos),
  });
  return apiFetch(`/api/usuarios?${params}`);
}

/**
 * Obtiene un usuario por ID.
 * @param {number} id
 */
export async function getUsuario(id) {
  return apiFetch(`/api/usuarios/${id}`);
}

/**
 * Crea un nuevo usuario.
 * @param {object} dto
 */
export async function crearUsuario(dto) {
  return apiFetch('/api/usuarios', {
    method: 'POST',
    body: JSON.stringify(dto),
  });
}

/**
 * Actualiza un usuario existente.
 * @param {number} id
 * @param {object} dto
 */
export async function actualizarUsuario(id, dto) {
  return apiFetch(`/api/usuarios/${id}`, {
    method: 'PUT',
    body: JSON.stringify(dto),
  });
}

/**
 * Resetea la contraseña de un usuario (admin).
 * @param {number} id
 */
export async function resetearPassword(id) {
  return apiFetch(`/api/usuarios/${id}/resetear-password`, {
    method: 'POST',
  });
}

/**
 * Obtiene los módulos disponibles para asignar como permisos.
 */
export async function getModulosPermisos() {
  return apiFetch('/api/usuarios/modulos-permisos');
}
