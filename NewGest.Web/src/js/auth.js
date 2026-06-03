/**
 * auth.js — JWT handling + apiFetch interceptor
 * El token se guarda en localStorage como 'newgest_token'.
 */

const TOKEN_KEY = 'newgest_token';
const USER_KEY  = 'newgest_user';

// ─── Token storage ───────────────────────────────────────────────────────────
export function getToken()           { return localStorage.getItem(TOKEN_KEY); }
export function setToken(token)      { localStorage.setItem(TOKEN_KEY, token); }
export function removeToken()        { localStorage.removeItem(TOKEN_KEY); localStorage.removeItem(USER_KEY); }

export function setUser(userData)    { localStorage.setItem(USER_KEY, JSON.stringify(userData)); }
export function getUser() {
  try { return JSON.parse(localStorage.getItem(USER_KEY) ?? 'null'); }
  catch { return null; }
}

/**
 * Parsea el payload del JWT sin verificar firma (solo en cliente).
 * @param {string} token
 */
export function parseJwtPayload(token) {
  try {
    const base64 = token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
    return JSON.parse(atob(base64));
  } catch {
    return null;
  }
}

/**
 * Retorna true si el usuario tiene la flag debeResetearPassword en el JWT.
 */
export function debeResetearPassword() {
  const token = getToken();
  if (!token) return false;
  const payload = parseJwtPayload(token);
  return payload?.['newgest:debe_resetear'] === 'true';
}

/**
 * Verifica si hay sesión activa y redirige si no la hay.
 * @param {string} [redirectTo='/pages/login.html']
 */
export function requireAuth(redirectTo = '/pages/login.html') {
  const token = getToken();
  if (!token) {
    window.location.href = redirectTo;
    return false;
  }
  // Verificar expiración
  const payload = parseJwtPayload(token);
  if (payload?.exp && Date.now() >= payload.exp * 1000) {
    removeToken();
    window.location.href = redirectTo;
    return false;
  }
  return true;
}

/**
 * Fetch wrapper que agrega JWT automáticamente y maneja errores comunes.
 * @param {string} url
 * @param {RequestInit} [options]
 * @returns {Promise<any>}
 */
export async function apiFetch(url, options = {}) {
  const token = getToken();

  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  if (response.status === 401) {
    removeToken();
    window.location.href = '/pages/login.html';
    return;
  }

  if (!response.ok) {
    let errorMsg = `Error ${response.status}: ${response.statusText}`;
    try {
      const body = await response.json();
      errorMsg = body.message ?? errorMsg;
    } catch { /* ignore parse error */ }
    throw new Error(errorMsg);
  }

  if (response.status === 204) return null;
  return response.json();
}

/**
 * Cierra la sesión del usuario.
 */
export function logout() {
  removeToken();
  window.location.href = '/pages/login.html';
}
