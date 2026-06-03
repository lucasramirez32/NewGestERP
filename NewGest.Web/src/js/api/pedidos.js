import { apiFetch } from '../auth.js';

export async function getPedidos({ estado = null, page = 1, pageSize = 20 } = {}) {
  const params = new URLSearchParams({ page, pageSize });
  if (estado !== null) params.set('estado', estado);
  return apiFetch(`/api/pedidos?${params}`);
}

export async function getPedidoById(id) {
  return apiFetch(`/api/pedidos/${id}`);
}

export async function crearPedido(dto) {
  return apiFetch('/api/pedidos', { method: 'POST', body: JSON.stringify(dto) });
}

export async function anularPedido(id) {
  return apiFetch(`/api/pedidos/${id}/anular`, { method: 'POST' });
}

export async function generarRemito(idPedido, dto) {
  return apiFetch(`/api/pedidos/${idPedido}/remito`, { method: 'POST', body: JSON.stringify(dto) });
}
