import { apiFetch } from '../auth.js';

export async function getPlanCuentas() {
  return apiFetch('/api/cuentas-contables');
}

export async function crearCuentaContable(dto) {
  return apiFetch('/api/cuentas-contables', { method: 'POST', body: JSON.stringify(dto) });
}

export async function getAsientos({ pagina = 1, tamano = 20 } = {}) {
  return apiFetch(`/api/asientos?pagina=${pagina}&tamano=${tamano}`);
}

export async function crearAsiento(dto) {
  return apiFetch('/api/asientos', { method: 'POST', body: JSON.stringify(dto) });
}

export async function getLibroIvaVentas(anio, mes) {
  return apiFetch(`/api/libro-iva/ventas?anio=${anio}&mes=${mes}`);
}

export async function getLibroIvaCompras(anio, mes) {
  return apiFetch(`/api/libro-iva/compras?anio=${anio}&mes=${mes}`);
}
