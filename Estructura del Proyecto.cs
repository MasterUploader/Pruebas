// src/app/core/services/auth.interceptor.ts
import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpContextToken,
  HTTP_INTERCEPTORS,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

/**
 * Contexto para saltar la autenticación en llamadas puntuales.
 * Ejemplo de uso:
 * this.http.get(url, { context: new HttpContext().set(SKIP_AUTH, true) })
 */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

/** Set de estatus HTTP que implican sesión inválida/expirada */
const AUTH_ERROR_STATUSES = new Set([401, 403, 440]);

/**
 * Interceptor de Autenticación:
 * - Adjunta el header Authorization con el JWT (si existe).
 * - Permite omitir autenticación con SKIP_AUTH en el contexto.
 * - Si el backend responde 401/403/440: realiza logout (limpia storage) y redirige a /login.
 *
 * Notas de calidad:
 * - Usa optional chaining para evitar advertencias (“prefer optional chain expression”).
 * - Usa el string real del JWT: `currentUser.token.token` (evita "[object Object]").
 */
@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private readonly authService: AuthService,
    private readonly router: Router
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // 1) Permitir saltar el interceptor si así se indica en el contexto
    if (req.context.get(SKIP_AUTH)) {
      return next.handle(req);
    }

    // 2) Tomar el JWT (string) si existe en el usuario actual
    const jwt = this.authService.currentUserValue?.token?.token;

    // 3) Clonar y adjuntar Authorization solo si hay token
    if (jwt) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${jwt}`
        }
      });
    }

    // 4) Propagar la solicitud y capturar errores de autenticación
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        // Si el backend indica token inválido/expirado o sesión revocada:
        if (AUTH_ERROR_STATUSES.has(error.status)) {
          // Logout centralizado: limpia localStorage y navega a /login
          this.authService.logout();
          // Redundante, pero garantiza la redirección incluso si logout se ajusta en el futuro
          this.router.navigate(['/login'], { queryParams: { r: 'expired' } });
        }
        // Mantener el error para quien lo quiera manejar más arriba
        return throwError(() => error);
      })
    );
  }
}

/**
 * Provider DI para registrar el interceptor.
 * ⚠️ Si ya lo registras en `app.config.ts` con:
 *   { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
 * puedes omitir este export para evitar registros duplicados.
 */
export const authInterceptorProvider = {
  provide: HTTP_INTERCEPTORS,
  useClass: AuthInterceptor,
  multi: true
};
