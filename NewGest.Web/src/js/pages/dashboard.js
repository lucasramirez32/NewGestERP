/**
 * dashboard.js — Lógica del Dashboard principal.
 * Muestra conteos de clientes y artículos activos.
 * Las cards de ventas/cobranzas son stubs hasta Sprints 8 y 12.
 */

import { requireAuth } from '../auth.js';
import { getClientes } from '../api/clientes.js';
import { getArticulos } from '../api/articulos.js';

requireAuth();

// Fecha actual en español
const fechaEl = document.getElementById('fecha-hoy');
if (fechaEl) {
  fechaEl.textContent = new Intl.DateTimeFormat('es-AR', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  }).format(new Date());
}

// Cargar totales en paralelo
async function cargarTotales() {
  const cardClientes  = document.getElementById('card-clientes');
  const cardArticulos = document.getElementById('card-articulos');

  try {
    const [dataClientes, dataArticulos] = await Promise.all([
      getClientes({ page: 1, pageSize: 1 }),
      getArticulos({ page: 1, pageSize: 1 }),
    ]);

    if (cardClientes)  cardClientes.textContent  = dataClientes.total.toLocaleString('es-AR');
    if (cardArticulos) cardArticulos.textContent = dataArticulos.total.toLocaleString('es-AR');
  } catch (err) {
    if (cardClientes)  cardClientes.textContent  = 'Error';
    if (cardArticulos) cardArticulos.textContent = 'Error';
    console.error('Error al cargar totales del dashboard:', err);
  }
}

cargarTotales();
