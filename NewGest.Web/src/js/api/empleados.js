import { apiFetch } from '../auth.js';

export async function getEmpleados({ search = '', soloVendedores = null, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams({ page, pageSize });
  if (search) params.set('search', search);
  if (soloVendedores !== null) params.set('soloVendedores', soloVendedores);
  return apiFetch(`/api/empleados?${params}`);
}

export async function getEmpleadoById(id) {
  return apiFetch(`/api/empleados/${id}`);
}

export async function crearEmpleado(dto) {
  return apiFetch('/api/empleados', { method: 'POST', body: JSON.stringify(dto) });
}

export async function actualizarEmpleado(id, dto) {
  return apiFetch(`/api/empleados/${id}`, { method: 'PUT', body: JSON.stringify(dto) });
}

export async function desactivarEmpleado(id) {
  return apiFetch(`/api/empleados/${id}`, { method: 'DELETE' });
}

export async function getVendedores() {
  return apiFetch('/api/vendedores');
}

export async function getViajes() {
  return apiFetch('/api/viajes');
}

export async function crearViaje(dto) {
  return apiFetch('/api/viajes', { method: 'POST', body: JSON.stringify(dto) });
}

export async function actualizarViaje(id, dto) {
  return apiFetch(`/api/viajes/${id}`, { method: 'PUT', body: JSON.stringify(dto) });
}

export async function getMutuales() {
  return apiFetch('/api/mutuales');
}

export async function crearMutual(dto) {
  return apiFetch('/api/mutuales', { method: 'POST', body: JSON.stringify(dto) });
}

export async function actualizarMutual(id, dto) {
  return apiFetch(`/api/mutuales/${id}`, { method: 'PUT', body: JSON.stringify(dto) });
}
