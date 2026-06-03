/**
 * articulos.js — Fetch wrappers para los recursos Artículos y GruposArticulos.
 */

import { apiFetch } from '../auth.js';

/**
 * Obtiene la lista paginada de artículos con filtros opcionales.
 */
export async function getArticulos({ search = '', idGrupo = '', page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (search)   params.set('search', search);
  if (idGrupo)  params.set('idGrupo', idGrupo);
  params.set('page', page);
  params.set('pageSize', pageSize);
  return apiFetch(`/api/articulos?${params}`);
}

/**
 * Obtiene un artículo por su ID.
 */
export async function getArticuloById(id) {
  return apiFetch(`/api/articulos/${id}`);
}

/**
 * Crea un nuevo artículo.
 */
export async function crearArticulo(dto) {
  return apiFetch('/api/articulos', {
    method: 'POST',
    body: JSON.stringify(dto),
  });
}

/**
 * Actualiza un artículo existente.
 */
export async function actualizarArticulo(id, dto) {
  return apiFetch(`/api/articulos/${id}`, {
    method: 'PUT',
    body: JSON.stringify(dto),
  });
}

/**
 * Desactiva (soft delete) un artículo.
 */
export async function desactivarArticulo(id) {
  return apiFetch(`/api/articulos/${id}`, { method: 'DELETE' });
}

/**
 * Obtiene el árbol de grupos de artículos.
 */
export async function getGruposArticulos() {
  return apiFetch('/api/grupos-articulos');
}

/**
 * Crea un nuevo grupo de artículos.
 */
export async function crearGrupoArticulo(dto) {
  return apiFetch('/api/grupos-articulos', {
    method: 'POST',
    body: JSON.stringify(dto),
  });
}

/**
 * Obtiene las existencias de un artículo (stub Sprint 5).
 */
export async function getExistencias(id) {
  return apiFetch(`/api/articulos/${id}/existencias`);
}
