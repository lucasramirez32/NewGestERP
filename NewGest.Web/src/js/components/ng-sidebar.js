/**
 * ng-sidebar — Web Component de barra lateral de navegación principal.
 *
 * Renderiza el menú con todos los módulos del sistema.
 * Resalta el ítem activo comparando window.location.pathname.
 * Módulos bloqueados muestran un badge "Próximamente".
 *
 * Uso:
 *   <ng-sidebar></ng-sidebar>
 */

import { logout, getUser } from '../auth.js';

const MENU_ITEMS = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    href: '/pages/index.html',
    disponible: true,
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
      d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6"/>`
  },
  {
    id: 'clientes',
    label: 'Clientes',
    href: '/pages/clientes.html',
    disponible: true,
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
      d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0z"/>`
  },
  {
    id: 'articulos',
    label: 'Artículos',
    href: '/pages/articulos.html',
    disponible: true,
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
      d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"/>`
  },
  {
    id: 'facturacion',
    label: 'Facturación',
    href: '/pages/facturacion.html',
    disponible: false,
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>`
  },
  {
    id: 'cobranzas',
    label: 'Cobranzas',
    href: '/pages/cobranzas.html',
    disponible: false,
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
      d="M17 9V7a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2m2 4h10a2 2 0 002-2v-6a2 2 0 00-2-2H9a2 2 0 00-2 2v6a2 2 0 002 2zm7-5a2 2 0 11-4 0 2 2 0 014 0z"/>`
  },
  {
    id: 'contabilidad',
    label: 'Contabilidad',
    href: '/pages/contabilidad.html',
    disponible: false,
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
      d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z"/>`
  },
  { separator: true },
  {
    id: 'parametros',
    label: 'Parámetros',
    href: '/pages/admin/parametros.html',
    disponible: true,
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
      d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>`
  },
  {
    id: 'usuarios',
    label: 'Usuarios',
    href: '/pages/admin/usuarios.html',
    disponible: true,
    icon: `<path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
      d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z"/>`
  },
];

class NgSidebar extends HTMLElement {
  connectedCallback() {
    this._render();
    this._bindEvents();
  }

  _render() {
    const user = getUser();
    const currentPath = window.location.pathname;

    const navItems = MENU_ITEMS.map(item => {
      if (item.separator) {
        return `<hr class="border-gray-700 my-2" />`;
      }

      const isActive = currentPath === item.href ||
        (item.href !== '/pages/index.html' && currentPath.startsWith(item.href.replace('.html', '')));

      const activeClass = isActive
        ? 'bg-white/10 text-white'
        : 'text-gray-400 hover:bg-white/5 hover:text-white';

      const disabledAttr = !item.disponible ? 'aria-disabled="true" tabindex="-1"' : '';
      const tag = item.disponible ? 'a' : 'span';
      const hrefAttr = item.disponible ? `href="${item.href}"` : '';

      const badge = !item.disponible
        ? `<span class="ml-auto text-xs bg-gray-700 text-gray-400 px-1.5 py-0.5 rounded">Próx.</span>`
        : '';

      return `
        <${tag} ${hrefAttr} ${disabledAttr}
          class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors cursor-pointer ${activeClass} ${!item.disponible ? 'opacity-50 cursor-not-allowed' : ''}">
          <svg class="w-5 h-5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            ${item.icon}
          </svg>
          ${item.label}
          ${badge}
        </${tag}>
      `;
    }).join('');

    this.innerHTML = `
      <aside class="w-64 bg-gray-900 text-white flex flex-col flex-shrink-0 fixed top-0 left-0 h-full z-30">
        <!-- Logo / Marca -->
        <div class="px-6 py-5 border-b border-gray-700">
          <span class="text-xl font-bold tracking-tight text-white">NewGest</span>
          <span class="block text-xs text-gray-400 mt-0.5">ERP Administrativo</span>
        </div>

        <!-- Menú de navegación -->
        <nav class="flex-1 px-3 py-4 space-y-0.5 overflow-y-auto">
          ${navItems}
        </nav>

        <!-- Footer: usuario + logout -->
        <div class="px-4 py-4 border-t border-gray-700">
          <div class="flex items-center gap-3 mb-3">
            <div class="w-8 h-8 rounded-full bg-blue-600 flex items-center justify-center text-white text-sm font-bold flex-shrink-0">
              ${this._getInitial(user?.nombre ?? '')}
            </div>
            <div class="min-w-0">
              <p class="text-sm font-medium text-white truncate">${user?.nombre ?? 'Usuario'}</p>
              <p class="text-xs text-gray-400 truncate">Empresa ${user?.idEmpresa ?? ''}</p>
            </div>
          </div>
          <button id="ng-sidebar-logout"
            class="flex items-center gap-2 text-sm text-gray-400 hover:text-white transition-colors w-full">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"/>
            </svg>
            Cerrar sesión
          </button>
        </div>
      </aside>
    `;
  }

  _bindEvents() {
    this.querySelector('#ng-sidebar-logout')?.addEventListener('click', () => logout());
  }

  _getInitial(nombre) {
    return nombre ? nombre.charAt(0).toUpperCase() : '?';
  }
}

customElements.define('ng-sidebar', NgSidebar);
export { NgSidebar };
