import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { BehaviorSubject, Observable, tap, catchError, throwError, map } from 'rxjs';
import { Router } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';
import { LoginResponse } from '../models/login.Response.model';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../environments/environment';

/** Respuesta esperada del KeepAlive (ajústala a tu API real) */
interface KeepAliveResponse {
  expiration?: string | null; // ISO de nueva expiración
  token?: string | null;      // JWT renovado opcional
  full?: LoginResponse | null; // Alternativa: objeto completo
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUserSubject: BehaviorSubject<LoginResponse | null>;
  public readonly currentUser: Observable<LoginResponse | null>;

  private readonly sessionActive: BehaviorSubject<boolean>;
  private readonly apiUrl = environment.apiBaseUrl;

  public agenciaAperturaCodigo: string | null = null;
  public agenciaImprimeCodigo: string | null = null;
  public usuarioICBS = '';

  private readonly isBrowser: boolean = false;

  constructor(
    @Inject(PLATFORM_ID) private readonly platformId: Object,
    private readonly router: Router,
    private readonly http: HttpClient
  ) {
    this.isBrowser = isPlatformBrowser(this.platformId);
    const initialUser = this.isBrowser ? JSON.parse(localStorage.getItem('currentUser') || 'null') : null;

    this.currentUserSubject = new BehaviorSubject<LoginResponse | null>(initialUser);
    this.currentUser = this.currentUserSubject.asObservable();
    this.sessionActive = new BehaviorSubject<boolean>(this.sessionIsActive());

    if (initialUser?.activeDirectoryData) {
      this.agenciaAperturaCodigo = initialUser.activeDirectoryData.agenciaAperturaCodigo;
      this.agenciaImprimeCodigo = initialUser.activeDirectoryData.agenciaImprimeCodigo;
      this.usuarioICBS = initialUser.activeDirectoryData.usuarioICBS;
    }
  }

  /** Normaliza errores Http en mensajes para UI */
  private toUserFacingError(error: HttpErrorResponse): Error {
    const msgFromApi =
      (error?.error && (error.error?.codigo?.message || error.error?.message)) ||
      (typeof error?.error === 'string' ? error.error : null);
    return new Error(msgFromApi || 'Ocurrió un error durante el inicio de sesión');
  }

  /** Persiste sesión (sin navegar) */
  private persistSession(response: LoginResponse): void {
    if (!this.isBrowser) return;

    const expiresAt = new Date(response.token.expiration);
    localStorage.setItem('currentUser', JSON.stringify(response));
    localStorage.setItem('token', response.token.token);
    localStorage.setItem('expiresAt', String(expiresAt.getTime()));

    this.sessionActive.next(true);
    this.currentUserSubject.next(response);

    this.agenciaAperturaCodigo = response.activeDirectoryData.agenciaAperturaCodigo;
    this.agenciaImprimeCodigo = response.activeDirectoryData.agenciaImprimeCodigo;
    this.usuarioICBS = response.activeDirectoryData.usuarioICBS;
  }

  /** Login: persiste y retorna observable */
  login(userName: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/api/Auth/Login`, { user: userName, password })
      .pipe(
        tap(res => this.persistSession(res)),
        catchError((err: HttpErrorResponse) => throwError(() => this.toUserFacingError(err)))
      );
  }

  /** Logout: limpia storage, notifica y navega al login */
  logout(): void {
    if (this.isBrowser) {
      localStorage.clear();
    }
    this.currentUserSubject.next(null);
    this.sessionActive.next(false);
    this.router.navigate(['/login']);
  }

  /** ¿Hay usuario + token? (no valida expiración exacta) */
  isAuthenticated(): boolean {
    const currentUser = this.currentUserValue;
    return !!currentUser && !!currentUser.token;
  }

  /**
   * ¿La sesión está activa? (valida contra expiresAt guardado).
   * TIP: también conviene validar el claim exp real del JWT.
   */
  sessionIsActive(): boolean {
    if (!this.isBrowser) return false;
    const expiresAt = localStorage.getItem('expiresAt');
    if (!expiresAt) return false;
    return Date.now() < Number(expiresAt);
  }

  /** Devuelve el usuario actual (puede ser null) */
  public get currentUserValue(): LoginResponse | null {
    return this.currentUserSubject.value;
  }

  /** Observable del estado de sesión */
  public get sessionActive$(): Observable<boolean> {
    return this.sessionActive.asObservable();
  }

  /** Nombre visible (si existe) */
  public get currentUserName(): string | null {
    return this.currentUserValue?.activeDirectoryData?.nombreUsuario || null;
  }

  /** Devuelve el token actual (string) o null. */
  public getAccessToken(): string | null {
    if (!this.isBrowser) return null;
    return localStorage.getItem('token');
  }

  /** Lee el `exp` del JWT (epoch seconds) si existe, null si no. */
  public getJwtExp(): number | null {
    const token = this.getAccessToken();
    if (!token) return null;
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    try {
      const payload = JSON.parse(atob(parts[1]));
      return typeof payload.exp === 'number' ? payload.exp : null;
    } catch {
      return null;
    }
  }

  // ----------------------- KEEP ALIVE -----------------------

  /**
   * Llama al endpoint de KeepAlive para extender sesión en el servidor.
   * Si la API devuelve token nuevo o nueva expiración, actualiza el storage local.
   * No navega; sólo extiende.
   */
  keepAlive(): Observable<void> {
    return this.http.post<KeepAliveResponse>(`${this.apiUrl}/api/Auth/KeepAlive`, {})
      .pipe(
        map(resp => {
          // 1) Si el backend devuelve objeto completo
          if (resp?.full) {
            this.persistSession(resp.full);
            return;
          }

          // 2) Token y/o expiración sueltos
          const token = resp?.token ?? undefined;
          const expirationIso = resp?.expiration ?? undefined;

          if (!token && !expirationIso) return; // ya es válido: el servidor extendió su lado

          const current = this.currentUserValue;

          // Token renovado
          if (token && current) {
            current.token.token = token;
            localStorage.setItem('token', token);
          }

          // Nueva expiración
          if (expirationIso) {
            const newExp = new Date(expirationIso).getTime();
            localStorage.setItem('expiresAt', String(newExp));

            if (current) {
              current.token.expiration = expirationIso;
              localStorage.setItem('currentUser', JSON.stringify(current));
              this.currentUserSubject.next(current);
            }
          }

          this.sessionActive.next(this.sessionIsActive());
        }),
        catchError((err: HttpErrorResponse) => {
          if (err.status === 401 || err.status === 403) {
            this.logout(); // sesión muerta en servidor
          }
          return throwError(() => err);
        })
      );
  }
}
