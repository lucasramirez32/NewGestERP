/**
 * ng-alert — Web Component para alertas/notificaciones.
 *
 * Uso (programático):
 *   NgAlert.mostrar('Operación exitosa', 'success');
 *   NgAlert.mostrar('Hubo un error', 'error');
 *   NgAlert.mostrar('Atención', 'warning');
 */
class NgAlert extends HTMLElement {
  connectedCallback() {
    this.setAttribute('aria-live', 'polite');
  }

  static mostrar(mensaje, tipo = 'success', duracion = 4000) {
    // Crear contenedor si no existe
    let container = document.getElementById('ng-alert-container');
    if (!container) {
      container = document.createElement('div');
      container.id = 'ng-alert-container';
      container.className = 'fixed top-4 right-4 z-[60] flex flex-col gap-2 max-w-sm w-full';
      document.body.appendChild(container);
    }

    const colores = {
      success: 'bg-green-50 border-green-300 text-green-800',
      error:   'bg-red-50 border-red-300 text-red-800',
      warning: 'bg-yellow-50 border-yellow-300 text-yellow-800',
      info:    'bg-blue-50 border-blue-300 text-blue-800',
    };
    const iconos = {
      success: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>`,
      error:   `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>`,
      warning: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/>`,
      info:    `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01"/>`,
    };

    const alerta = document.createElement('div');
    alerta.className = `flex items-start gap-3 border rounded-lg p-3 text-sm shadow-md transition-all duration-300 ${colores[tipo] ?? colores.info}`;
    alerta.innerHTML = `
      <svg class="w-5 h-5 flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        ${iconos[tipo] ?? iconos.info}
      </svg>
      <span class="flex-1">${mensaje}</span>
      <button class="text-current opacity-60 hover:opacity-100 ml-2" onclick="this.closest('div').remove()">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
        </svg>
      </button>
    `;
    container.appendChild(alerta);

    // Auto-remover
    if (duracion > 0) {
      setTimeout(() => alerta.remove(), duracion);
    }
  }
}

customElements.define('ng-alert', NgAlert);
export { NgAlert };
