/**
 * ng-modal — Web Component para modales reutilizables.
 *
 * Uso:
 *   <ng-modal id="mi-modal" titulo="Crear Cliente">
 *     <!-- contenido -->
 *   </ng-modal>
 *
 * API JS:
 *   document.getElementById('mi-modal').open('Título opcional');
 *   document.getElementById('mi-modal').close();
 *
 * Eventos:
 *   ng-modal:close — emitido al cerrar
 *
 * Cierre automático con:
 *   - Clic en backdrop
 *   - Tecla Escape
 *   - Cualquier elemento con atributo [data-close]
 */
class NgModal extends HTMLElement {
  connectedCallback() {
    this._render();
    this._bindEvents();
  }

  _render() {
    const titulo = this.getAttribute('titulo') ?? '';
    const anchura = this.getAttribute('width') ?? 'max-w-lg';

    // Estructura del modal — el contenido original se mueve al body del modal
    const contenidoOriginal = this.innerHTML;
    this.innerHTML = `
      <div class="ng-modal-backdrop fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
           style="display: none;">
        <div class="ng-modal-panel bg-white rounded-xl shadow-xl w-full ${anchura} max-h-[90vh] overflow-y-auto">
          <div class="ng-modal-header flex items-center justify-between px-6 py-4 border-b border-gray-200">
            <h2 class="ng-modal-title text-lg font-semibold text-gray-800">${this._escapeHtml(titulo)}</h2>
            <button type="button" data-close
              class="text-gray-400 hover:text-gray-600 transition-colors"
              aria-label="Cerrar">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>
          <div class="ng-modal-body">
            ${contenidoOriginal}
          </div>
        </div>
      </div>
    `;
    this._backdrop = this.querySelector('.ng-modal-backdrop');
  }

  _bindEvents() {
    // Cerrar con [data-close]
    this.addEventListener('click', (e) => {
      if (e.target.closest('[data-close]')) this.close();
    });

    // Cerrar al hacer clic en el backdrop (fuera del panel)
    this._backdrop?.addEventListener('click', (e) => {
      if (e.target === this._backdrop) this.close();
    });

    // Cerrar con Escape
    this._escListener = (e) => {
      if (e.key === 'Escape') this.close();
    };
  }

  open(titulo = '') {
    if (titulo) {
      const titleEl = this.querySelector('.ng-modal-title');
      if (titleEl) titleEl.textContent = titulo;
    }
    this._backdrop.style.display = 'flex';
    document.addEventListener('keydown', this._escListener);
    // Focus trap: enfocar primer input del modal
    setTimeout(() => {
      const firstInput = this.querySelector('input, select, textarea, button:not([data-close])');
      firstInput?.focus();
    }, 50);
  }

  close() {
    this._backdrop.style.display = 'none';
    document.removeEventListener('keydown', this._escListener);
    this.dispatchEvent(new CustomEvent('ng-modal:close', { bubbles: true }));
  }

  _escapeHtml(str) {
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }
}

customElements.define('ng-modal', NgModal);
export { NgModal };
