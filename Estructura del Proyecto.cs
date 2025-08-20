Me quedare con esta version porque da complejidad 7, y las ultimas 2 que me entregastes dan complejidad 12 y 13 respectivamente

import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { BehaviorSubject, Observable, map, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';
import { LoginResponse } from '../models/login.Response.model';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly currentUserSubject: BehaviorSubject<LoginResponse | null>;
  public currentUser: Observable<LoginResponse | null>;

  private readonly currentUserSubject2 = new BehaviorSubject<any>(null); // Prueba
  public currentUser2 = this.currentUserSubject2.asObservable(); // Prueba

  private readonly sessionActive: BehaviorSubject<boolean>;
  private readonly apiUrl = environment.apiBaseUrl;
  public agenciaAperturaCodigo: string | null = null;
  public agenciaImprimeCodigo: string | null = null;
  public usuarioICBS: string = '';
  private isBrowser = false;

  constructor(
    @Inject(PLATFORM_ID) private readonly platformId: Object,
    private readonly router: Router,
    private readonly http: HttpClient
  ) {
    let initialUser: LoginResponse | null;
    let sessionIsActive = false;
    this.isBrowser = isPlatformBrowser(this.platformId);

    if (this.isBrowser) {
      initialUser = JSON.parse(localStorage.getItem('currentUser') || 'null');
      sessionIsActive = this.sessionIsActive();
    } else {
      initialUser = null;
    }

    this.currentUserSubject = new BehaviorSubject<LoginResponse | null>(initialUser);
    this.currentUser = this.currentUserSubject.asObservable();
    this.sessionActive = new BehaviorSubject<boolean>(sessionIsActive);
  }

  /** Normaliza cualquier HttpErrorResponse a un Error con message legible */
  private toUserFacingError(error: HttpErrorResponse): Error {
    const msgFromApi =
      (error?.error && (error.error?.codigo?.message || error.error?.message)) ||
      (typeof error?.error === 'string' ? error.error : null);
    return new Error(msgFromApi || 'Ocurrió un error durante el inicio de sesión');
  }

  login(userName: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/api/Auth/Login`, { user: userName, password })
      .pipe(
        map(response => {
          if (isPlatformBrowser(this.platformId)) {
            const expiresAt = new Date(response.token.expiration);

            localStorage.setItem('currentUser', JSON.stringify(response));
            localStorage.setItem('token', response.token.token);
            localStorage.setItem('expiresAt', expiresAt.getTime().toString());

            this.sessionActive.next(true);
            this.currentUserSubject.next(response);
            this.agenciaAperturaCodigo = response.activeDirectoryData.agenciaAperturaCodigo;
            this.agenciaImprimeCodigo = response.activeDirectoryData.agenciaImprimeCodigo;
            this.usuarioICBS = response.activeDirectoryData.usuarioICBS;

            // Mantengo tu navegación aquí para no duplicarla en el componente
            this.router.navigate(['/tarjetas']);
          }
          return response;
        }),
        // ⬇️ Centralizamos el manejo del error: el componente recibirá un Error con message claro
        catchError((err: HttpErrorResponse) => throwError(() => this.toUserFacingError(err)))
      );
  }

  logout(): void {
    localStorage.clear();
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    const currentUser = this.currentUserValue;
    return !!currentUser && !!currentUser.token;
  }

  sessionIsActive(): boolean {
    if (this.isBrowser) {
      const expiresAt = localStorage.getItem('expiresAt');
      return new Date().getTime() < Number(expiresAt);
    }
    return false;
  }

  public get currentUserValue(): LoginResponse | null {
    return this.currentUserSubject.value;
  }

  public get sessionActive$(): Observable<boolean> {
    return this.sessionActive.asObservable();
  }

  public get currentUserName(): string | null {
    return this.currentUserValue?.activeDirectoryData.nombreUsuario || null;
  }
}
