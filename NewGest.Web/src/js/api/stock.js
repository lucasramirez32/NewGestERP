import { apiFetch } from '../auth.js';

export async function getDepositos() {
  return apiFetch('/api/depositos');
}

export async function crearDeposito(dto) {
  return apiFetch('/api/depositos', { method: 'POST', body: JSON.stringify(dto) });
}

export async function getExistencias(idArticulo) {
  return apiFetch(`/api/stock/${idArticulo}/existencias`);
}

export async function getHistorialMovimientos(idArticulo, { page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams({ page, pageSize });
  return apiFetch(`/api/stock/${idArticulo}/movimientos?${params}`);
}

export async function registrarMovimiento(dto) {
  return apiFetch('/api/stock/movimientos', { method: 'POST', body: JSON.stringify(dto) });
}

export async function getAlertasReposicion() {
  return apiFetch('/api/stock/alertas-reposicion');
}

export async function getStockValorizado() {
  return apiFetch('/api/stock/valorizado');
}
