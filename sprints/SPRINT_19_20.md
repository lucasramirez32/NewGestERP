# SPRINT 19–20 — Integraciones + UAT Final + Go-Live
**Semanas:** 77–84 · **Módulos:** M16 (Email/WhatsApp) + M18 (Exportaciones) + UAT final  
**Fase de coexistencia:** D — VFP desactivado al final de este sprint  
**Agente principal:** `fullstack-db` · **Agente QA:** `qa`  
**Riesgo:** Medio — últimas integraciones; el riesgo mayor es el go-live completo

---

## Objetivo

Migrar las integraciones externas (email, WhatsApp, exportaciones) y ejecutar el UAT final completo de todos los módulos. Al final del Sprint 20, VFP queda desactivado y el sistema .NET es el único productivo.

---

## Contexto VFP

| Archivo VFP | Función | Reemplazo .NET |
|---|---|---|
| `correo1.prg` | Envío de email con adjuntos | MailKit |
| `ENVIAR_CORREO.SCX` | Pantalla de composición email | Blazor component |
| `whatsapp.prg` | Envío WhatsApp (API no oficial) | Meta Cloud API / Twilio |
| `EXPORTXLS.SCX` | Export a Excel via COM | ClosedXML (ya implementado) |
| `EXPORDOC.SCX` | Export a Word via COM | DocumentFormat.OpenXml |
| `expor1doc.prg` | Export Word automático | DocumentFormat.OpenXml |
| `foxbarcodeqr.prg` | Generación QR | QRCoder (ya implementado) |
| `generar_certificado.prg` | Certificados AFIP | Windows Certificate Store |
| `CsFoxySmtp.dll` | DLL SMTP custom | MailKit |

---

## Sprint 19 — Email, WhatsApp y Exportaciones (Semanas 77–80)

### S19-1 — Servicio de Email (reemplaza CsFoxySmtp.dll)
**Entregable:** `NewGest.Infrastructure/Email/MailKitEmailService.cs`

```csharp
public class MailKitEmailService : IEmailService
{
    public async Task EnviarAsync(EmailMessage mensaje, CancellationToken ct)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_config.NombreFrom, _config.UsuarioSmtp));
        mensaje.Destinatarios.ForEach(d => email.To.Add(MailboxAddress.Parse(d)));

        if (!string.IsNullOrEmpty(mensaje.CC))
            email.Cc.Add(MailboxAddress.Parse(mensaje.CC));

        email.Subject = mensaje.Asunto;

        var bodyBuilder = new BodyBuilder { HtmlBody = mensaje.CuerpoHtml };
        foreach (var adj in mensaje.Adjuntos)
            bodyBuilder.Attachments.Add(adj.Nombre, adj.Contenido, ContentType.Parse(adj.MimeType));

        email.Body = bodyBuilder.ToMessageBody();

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_config.HostSmtp, _config.Puerto, _config.UsarSsl, ct);
        await smtp.AuthenticateAsync(_config.UsuarioSmtp, _config.PasswordSmtp, ct);
        await smtp.SendAsync(email, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}
```

**Casos de uso integrados con módulos existentes:**
- Enviar comprobante PDF al email del cliente automáticamente al emitir
- Enviar recibo de cobro
- Notificación de stock bajo mínimo

### S19-2 — WhatsApp Business
**Opción recomendada:** Meta Cloud API (oficial)

```csharp
public class WhatsAppService : IWhatsAppService
{
    public async Task EnviarComprobanteAsync(string telefono, string urlPdf, string numeroFactura, CancellationToken ct)
    {
        // Meta Cloud API: POST a graph.facebook.com/messages
        // Template de mensaje: "Su comprobante {1} está disponible: {2}"
        // El template debe estar aprobado por Meta previamente
    }
}
```

**Nota:** verificar si `whatsapp.prg` usa una API oficial o una API no oficial (como Baileys/WhatsApp Web unofficial). Si es no oficial, la migración a la API oficial de Meta requerirá aprobación de cuenta Business y templates.

### S19-3 — Export a Word (reemplaza COM Word)
```csharp
// DocumentFormat.OpenXml — sin Office requerido en el servidor
public class WordExportService
{
    public byte[] ExportarInforme(InformeDto informe)
    {
        using var ms = new MemoryStream();
        using var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document);
        // ... construir el documento
        return ms.ToArray();
    }
}
```

### S19-4 — Pantalla Envío de Email/WhatsApp (HTML + Tailwind + JS)
**Entregable:** `NewGest.Web/src/js/components/ng-enviar-comprobante.js` (Web Component reutilizable)

Reemplaza `ENVIAR_CORREO.SCX`. Se usa como modal desde cualquier página (facturación, cobros):
- Input destinatario: autocomplete sobre emails de `neg.Clientes`
- Campo asunto (pre-completado con nombre del comprobante)
- Textarea del cuerpo con texto por defecto configurable
- Checkboxes: "Enviar por Email" / "Enviar por WhatsApp" (solo si el cliente tiene teléfono)
- Botón Enviar → llamada a `POST /api/notificaciones/enviar` con `{ canal, destinatario, idComprobante }`
- Feedback con toast Tailwind (`fixed bottom-4 right-4`): "Email enviado ✓" o error descriptivo

---

## Sprint 20 — UAT Final y Go-Live (Semanas 81–84)

### S20-1 — UAT completo (todas las áreas)

**Metodología:** 1 semana de operación paralela completa — mismo día, mismo período, en VFP y .NET.

| Área | Responsable UAT | Criterio |
|---|---|---|
| Facturación | Jefe de Ventas | CAEs válidos, numeración sin gaps |
| Cobranzas | Administración | Saldos idénticos |
| Contabilidad | Contador/CPA | Cierre mensual centavo a centavo |
| Stock | Jefe de Depósito | Existencias idénticas |
| Reportes | Gerencia | Reportes estrella idénticos al VFP |
| IT | Admin IT | Performance, backups, logs |

### S20-2 — Checklist de go-live

**Infraestructura:**
- [ ] SQL Server con backup full diario + log cada hora verificado
- [ ] IIS configurado con HTTPS (TLS 1.3)
- [ ] Certificado SSL válido instalado
- [ ] Windows Service de sincronización detenido (ya no es necesario post go-live)
- [ ] Monitoreo: alertas configuradas en SQL Agent para jobs fallidos

**Seguridad:**
- [ ] Todos los usuarios con contraseña BCrypt (no hay ningún usuario con `DebeResetearPassword=true`)
- [ ] Archivos `.p12` en Key Vault, eliminados de `C:\newgest\certificado\`
- [ ] Directorio `C:\newgest\` en VFP desconectado de la red

**Funcional:**
- [ ] UAT firmado por todos los responsables de área
- [ ] Los 40 reportes estrella validados
- [ ] Retenciones del mes validadas por contador
- [ ] Certificado AFIP vigente (> 90 días de vencimiento)

**Plan de rollback (máximo 48hs):**
- VFP permanece disponible pero sin acceso durante los primeros 30 días post go-live
- Si hay incidente crítico en .NET → reactivar VFP inmediatamente
- Desactivación definitiva de VFP: 30 días después del go-live sin incidentes

### S20-3 — Desactivación de VFP

Una vez confirmado el go-live exitoso:
1. Revocar acceso de red al directorio `C:\newgest\`
2. Archivar los DBF en un ZIP con fecha y guardarlo offline
3. Desinstalar Visual FoxPro Runtime de todas las estaciones
4. Documentar el número de serie del software VFP (por si alguna vez se necesita recuperar datos históricos)

---

## Tests finales — Agente QA

### Regresión completa (ejecutar antes del go-live)
```
✓ Suite completa de tests unitarios: 100% green
✓ Suite de tests de integración: 100% green
✓ Test de carga: 20 usuarios concurrentes, 0 errores, respuesta < 2s promedio
✓ Test de emisión concurrente: 50 facturas simultáneas, sin duplicados
✓ Libro IVA del mes de paralelo: diferencia 0 con VFP
✓ Cierre contable del mes de paralelo: diferencia 0 con VFP
```

### Performance benchmarks (criterios de go-live)
```
✓ Login: < 500ms
✓ Búsqueda de clientes (10.000 registros): < 300ms
✓ Emisión de factura con CAE: < 5s (incluye ida y vuelta AFIP)
✓ Generación PDF comprobante: < 2s
✓ Libro IVA mensual (5.000 registros): < 10s
✓ Export Excel clientes (10.000 registros): < 30s
```

---

## Criterios de aceptación

- [ ] UAT firmado por todas las áreas
- [ ] 0 incidentes P1 durante la semana de paralelo
- [ ] Performance dentro de los benchmarks definidos
- [ ] Backup funcional verificado (restore de prueba exitoso)
- [ ] Plan de rollback documentado y probado
- [ ] Capacitación completada: todos los usuarios operativos

---

## Post go-live (30 días)

| Día | Acción |
|---|---|
| D+1 | Monitoreo intensivo: revisar logs cada hora |
| D+7 | Primera revisión: ¿hay incidentes abiertos? |
| D+15 | Cierre quincenal: comparar con VFP si hay dudas |
| D+30 | Cierre mensual: confirmar con contador · Desactivar VFP definitivamente |

---

## Dependencias

- **Requiere:** Sprint 16-18 (Reportes) + todos los módulos anteriores
- **Bloquea:** nada — este es el sprint final
