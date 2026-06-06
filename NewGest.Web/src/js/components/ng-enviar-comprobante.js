import { apiFetch } from '../auth.js';

/**
 * Web Component reutilizable para enviar comprobantes por Email y/o WhatsApp.
 * Reemplaza ENVIAR_CORREO.SCX del sistema VFP.
 *
 * Uso:
 *   <ng-enviar-comprobante id-comprobante="42"></ng-enviar-comprobante>
 *
 * O programáticamente:
 *   const el = document.querySelector('ng-enviar-comprobante');
 *   el.abrir(idComprobante, emailSugerido, telefonoSugerido);
 */
class NgEnviarComprobante extends HTMLElement {
  connectedCallback() {
    this._render();
    this._attachEvents();
  }

  abrir(idComprobante, emailSugerido = '', telefonoSugerido = '') {
    this._idComprobante = idComprobante;
    this.shadowRoot.querySelector('#email-dest').value    = emailSugerido;
    this.shadowRoot.querySelector('#telefono-dest').value = telefonoSugerido;
    this.shadowRoot.querySelector('#chk-email').checked    = !!emailSugerido;
    this.shadowRoot.querySelector('#chk-whatsapp').checked = !!telefonoSugerido;
    this.shadowRoot.querySelector('#msg-resultado').textContent = '';
    this.shadowRoot.querySelector('#msg-resultado').className   = '';
    this.shadowRoot.querySelector('dialog').showModal();
  }

  _render() {
    this.attachShadow({ mode: 'open' });
    this.shadowRoot.innerHTML = `
      <style>
        dialog {
          border: none; border-radius: 12px; padding: 0;
          box-shadow: 0 20px 60px rgba(0,0,0,0.3);
          width: 420px; max-width: 95vw;
          font-family: system-ui, sans-serif; font-size: 14px;
        }
        dialog::backdrop { background: rgba(0,0,0,0.45); }
        .header { padding: 20px 24px 0; border-bottom: 1px solid #e5e7eb; padding-bottom: 16px; }
        .header h3 { margin: 0; font-size: 17px; font-weight: 600; color: #111827; }
        .body { padding: 20px 24px; display: flex; flex-direction: column; gap: 14px; }
        label { display: flex; align-items: center; gap: 10px; cursor: pointer; font-weight: 500; color: #374151; }
        input[type=text], input[type=email], input[type=tel] {
          width: 100%; border: 1px solid #d1d5db; border-radius: 8px;
          padding: 8px 12px; font-size: 13px; box-sizing: border-box;
          outline: none; transition: border-color .15s;
        }
        input:focus { border-color: #3b82f6; box-shadow: 0 0 0 3px rgba(59,130,246,.15); }
        .campo { display: flex; flex-direction: column; gap: 4px; padding-left: 26px; }
        .campo label { font-size: 12px; font-weight: 400; color: #6b7280; }
        .footer { padding: 12px 24px 20px; display: flex; justify-content: flex-end; gap: 10px; }
        button {
          padding: 9px 18px; border-radius: 8px; font-size: 13px;
          font-weight: 500; cursor: pointer; border: none;
        }
        .btn-cancel { background: #f3f4f6; color: #374151; }
        .btn-cancel:hover { background: #e5e7eb; }
        .btn-send { background: #2563eb; color: #fff; }
        .btn-send:hover { background: #1d4ed8; }
        .btn-send:disabled { opacity: 0.5; cursor: not-allowed; }
        .msg { padding: 0 24px 16px; font-size: 13px; }
        .msg.ok  { color: #15803d; }
        .msg.err { color: #dc2626; }
      </style>

      <dialog id="dialog">
        <div class="header"><h3>Enviar comprobante</h3></div>
        <div class="body">

          <label>
            <input type="checkbox" id="chk-email" checked />
            Enviar por Email
          </label>
          <div class="campo" id="campo-email">
            <label for="email-dest">Destinatario</label>
            <input type="email" id="email-dest" placeholder="cliente@ejemplo.com" />
          </div>

          <label>
            <input type="checkbox" id="chk-whatsapp" />
            Enviar por WhatsApp
          </label>
          <div class="campo" id="campo-whatsapp" style="display:none">
            <label for="telefono-dest">Teléfono (con código de área)</label>
            <input type="tel" id="telefono-dest" placeholder="011-1234-5678" />
          </div>

        </div>
        <p class="msg" id="msg-resultado"></p>
        <div class="footer">
          <button class="btn-cancel" id="btn-cancelar">Cancelar</button>
          <button class="btn-send"   id="btn-enviar">Enviar</button>
        </div>
      </dialog>
    `;
  }

  _attachEvents() {
    const sr = this.shadowRoot;
    sr.querySelector('#btn-cancelar').addEventListener('click', () =>
      sr.querySelector('dialog').close());

    sr.querySelector('#btn-enviar').addEventListener('click', () => this._enviar());

    // Fix BUG-4: IDs dedicados en vez de nth-child (robusto ante cambios en el DOM)
    const toggleCampo = (chkId, campoId) =>
      sr.querySelector(chkId).addEventListener('change', e =>
        sr.querySelector(campoId).style.display = e.target.checked ? 'flex' : 'none');
    toggleCampo('#chk-email',    '#campo-email');
    toggleCampo('#chk-whatsapp', '#campo-whatsapp');
  }

  async _enviar() {
    const sr        = this.shadowRoot;
    const btnEnviar = sr.querySelector('#btn-enviar');
    const msgEl     = sr.querySelector('#msg-resultado');

    // Fix BUG-5 (guard): asegurar que abrir() fue llamado con un ID válido
    if (!this._idComprobante) {
      msgEl.textContent = 'Error interno: no hay comprobante seleccionado.';
      msgEl.className = 'msg err';
      return;
    }

    const enviarEmail    = sr.querySelector('#chk-email').checked;
    const enviarWhatsApp = sr.querySelector('#chk-whatsapp').checked;

    if (!enviarEmail && !enviarWhatsApp) {
      msgEl.textContent = 'Seleccioná al menos un canal de envío.';
      msgEl.className = 'msg err';
      return;
    }

    btnEnviar.disabled = true;
    btnEnviar.textContent = 'Enviando…';
    msgEl.textContent = '';
    msgEl.className = '';

    try {
      const resultado = await apiFetch('/api/notificaciones/enviar-comprobante', {
        method: 'POST',
        body: JSON.stringify({
          idComprobante:    this._idComprobante,
          enviarEmail,
          enviarWhatsApp,
          emailDestino:     enviarEmail    ? sr.querySelector('#email-dest').value    || null : null,
          telefonoDestino:  enviarWhatsApp ? sr.querySelector('#telefono-dest').value || null : null,
        }),
      });

      const lineas = [];
      if (resultado.emailEnviado)     lineas.push('Email enviado ✓');
      if (resultado.whatsAppEnviado)  lineas.push('WhatsApp enviado ✓');
      if (resultado.errorEmail)       lineas.push(`Email: ${resultado.errorEmail}`);
      if (resultado.errorWhatsApp)    lineas.push(`WhatsApp: ${resultado.errorWhatsApp}`);

      const ok = resultado.emailEnviado || resultado.whatsAppEnviado;
      msgEl.textContent = lineas.join(' · ');
      msgEl.className   = `msg ${ok ? 'ok' : 'err'}`;

      if (ok) setTimeout(() => sr.querySelector('dialog').close(), 2000);

    } catch (err) {
      msgEl.textContent = err.message ?? 'Error al enviar.';
      msgEl.className   = 'msg err';
    } finally {
      btnEnviar.disabled   = false;
      btnEnviar.textContent = 'Enviar';
    }
  }
}

customElements.define('ng-enviar-comprobante', NgEnviarComprobante);
