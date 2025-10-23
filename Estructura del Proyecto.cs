import { firstValueFrom } from 'rxjs';
import { HttpContext, HttpHeaders } from '@angular/common/http';
import { SKIP_AUTH } from '../services/auth.interceptor'; // ajusta la ruta si difiere




/**
 * Notifica al backend el cierre de sesión (POST /api/Auth/Logout).
 * - Fuerza el envío de Authorization sin pasar por el interceptor (evita bucles).
 * - Ignora errores: si el token ya venció el backend puede responder 401.
 */
private async notifyServerLogout(token: string): Promise<void> {
  const url = `${this.apiUrl}/api/Auth/Logout`;

  // Armamos headers manualmente y saltamos el interceptor con SKIP_AUTH.
  const headers = new HttpHeaders({ Authorization: `Bearer ${token}` });
  const context = new HttpContext().set(SKIP_AUTH, true);

  try {
    await firstValueFrom(this.http.post(url, {}, { headers, context }));
  } catch {
    // No rompemos el flujo de logout por fallas de red/401/403
  }
}


/**
 * Cierra sesión localmente y avisa al backend para marcar la sesión como cerrada.
 * Flujo:
 *  1) Si hay token, POST /api/Auth/Logout (con header Authorization manual).
 *  2) Limpia storage/estado local.
 *  3) Redirige a /login (puedes cambiar el destino con el parámetro).
 */
async logout(redirectTo: string = '/login'): Promise<void> {
  const token = this.getAccessToken();

  // 1) Avisar al backend (si tenemos token vigente según nuestro reloj local)
  if (token && this.sessionIsActive()) {
    await this.notifyServerLogout(token);
  }

  // 2) Limpieza local (siempre)
  if (this.isBrowser) {
    localStorage.clear();
  }
  this.currentUserSubject.next(null);
  this.sessionActive.next(false);

  // 3) Redirección
  await this.router.navigate([redirectTo]);
}


