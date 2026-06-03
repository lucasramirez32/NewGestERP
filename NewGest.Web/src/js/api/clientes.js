/**
 * clientes.js — Fetch wrappers para el recurso Clientes.
 * Todos los accesos a la API pasan por apiFetch (agrega JWT automáticamente).
 */

import { apiFetch } from '../auth.js';

/**
 * Obtiene la lista paginada de clientes con filtros opcionales.
 * @param {Object} params
 * @param {string} [params.search]
 * @param {number|string} [params.condicionIva]
 * @param {number|string} [params.zona]
 * @param {number} [params.page]
 * @param {number} [params.pageSize]
 * @returns {Promise<{items: Array, total: number, page: number, pageSize: number}>}
 */
export async function getClientes({ search = '', condicionIva = '', zona = '', page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams();
  if (search)        params.set('search', search);
  if (condicionIva)  params.set('condicionIva', condicionIva);
  if (zona)          params.set('zona', zona);
  params.set('page', page);
  params.set('pageSize', pageSize);
  return apiFetch(`/api/clientes?${params}`);
}

/**
 * Obtiene un cliente por su ID.
 * @param {number} id
 */
export async function getClienteById(id) {
  return apiFetch(`/api/clientes/${id}`);
}

/**
 * Crea un nuevo cliente.
 * @param {Object} dto
 */
export async function crearCliente(dto) {
  return apiFetch('/api/clientes', {
    method: 'POST',
    body: JSON.stringify(dto),
  });
}

/**
 * Actualiza un cliente existente.
 * @param {number} id
 * @param {Object} dto
 */
export async function actualizarCliente(id, dto) {
  return apiFetch(`/api/clientes/${id}`, {
    method: 'PUT',
    body: JSON.stringify(dto),
  });
}

/**
 * Desactiva (soft delete) un cliente.
 * @param {number} id
 */
export async function desactivarCliente(id) {
  return apiFetch(`/api/clientes/${id}`, { method: 'DELETE' });
}

/**
 * Obtiene el saldo pendiente de un cliente (stub Sprint 12).
 * @param {number} id
 */
export async function getSaldoPendiente(id) {
  return apiFetch(`/api/clientes/${id}/saldo-pendiente`);
}
