import { apiFetch } from '../auth.js';

export async function getComprobantes({ pagina = 1, tamano = 20 } = {}) {
  return apiFetch(`/api/comprobantes?pagina=${pagina}&tamano=${tamano}`);
}

export async function getComprobanteById(id) {
  return apiFetch(`/api/comprobantes/${id}`);
}

export async function emitirFactura(dto) {
  return apiFetch('/api/comprobantes', {
    method: 'POST',
    body: JSON.stringify(dto),
  });
}

export async function emitirLote(comprobantes) {
  return apiFetch('/api/comprobantes/lote', {
    method: 'POST',
    body: JSON.stringify({ comprobantes }),
  });
}
