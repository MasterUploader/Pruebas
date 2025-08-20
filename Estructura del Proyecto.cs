Me quedare con este codigo, no hagas más cambios, pronto te pedire mejoras en otros puntos:

import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { BehaviorSubject, Observable, tap, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';
import { LoginResponse } from '../models/login.Response.model';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUserSubject: BehaviorSubject<LoginResponse | null>;
  public currentUser: Observable<LoginResponse | null>;

  private readonly sessionActive: BehaviorSubject<boolean>;
  private readonly apiUrl = environment.apiBaseUrl;

  public agenciaAperturaCodigo: string | null = null;
  public agenciaImprimeCodigo: string | null = null;
  public usuarioICBS = '';

  private isBrowser = false;

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
  }

  /** Convierte HttpErrorResponse en Error con message listo para UI */
  private toUserFacingError(error: HttpErrorResponse): Error {
    const msgFromApi =
      (error?.error && (error.error?.codigo?.message || error.error?.message)) ||
      (typeof error?.error === 'string' ? error.error : null);
    return new Error(msgFromApi || 'Ocurrió un error durante el inicio de sesión');
  }

  /** Persiste sesión local y actualiza subjects (sin navegar) */
  private persistSession(response: LoginResponse): void {
    if (!this.isBrowser) return;

    const expiresAt = new Date(response.token.expiration);
    localStorage.setItem('currentUser', JSON.stringify(response));
    localStorage.setItem('token', response.token.token);
    localStorage.setItem('expiresAt', expiresAt.getTime().toString());

    this.sessionActive.next(true);
    this.currentUserSubject.next(response);

    this.agenciaAperturaCodigo = response.activeDirectoryData.agenciaAperturaCodigo;
    this.agenciaImprimeCodigo = response.activeDirectoryData.agenciaImprimeCodigo;
    this.usuarioICBS = response.activeDirectoryData.usuarioICBS;
  }

  /** Login: persiste y expone error normalizado; NO navega */
  login(userName: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${this.apiUrl}/api/Auth/Login`, { user: userName, password })
      .pipe(
        tap(res => this.persistSession(res)),
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
    if (!this.isBrowser) return false;
    const expiresAt = localStorage.getItem('expiresAt');
    return new Date().getTime() < Number(expiresAt);
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



import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';
import { FormGroup, FormsModule, FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialogModule } from '@angular/material/dialog';
import { AuthService } from '../../../../core/services/auth.service';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    FormsModule,
    MatFormFieldModule,
    ReactiveFormsModule,
    MatSelectModule,
    MatInputModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatButtonModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  hidePassword = true;
  isLoading = false;
  errorMessage = '';

  loginForm: FormGroup;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly fb: FormBuilder,
    private readonly snackBar: MatSnackBar
  ) {
    this.loginForm = this.fb.group({
      userName: ['', [Validators.required, Validators.maxLength(10)]],
      password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]]
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  private setLoading(loading: boolean): void {
    this.isLoading = loading;
    loading
      ? this.loginForm.disable({ emitEvent: false })
      : this.loginForm.enable({ emitEvent: false });
  }

  private handleLoginError(error: unknown): void {
    this.errorMessage = (error as Error)?.message || 'Ocurrió un error durante el inicio de sesión';
    this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
  }

  /** Llama al servicio, navega si todo ok; UI robusta con try/finally */
  async login(): Promise<void> {
    if (this.loginForm.invalid || this.isLoading) return;

    this.errorMessage = '';
    this.setLoading(true);

    try {
      const { userName, password } = this.loginForm.value;
      await firstValueFrom(this.authService.login(userName, password));
      await this.router.navigate(['/tarjetas']); // navegación aquí
    } catch (error) {
      this.handleLoginError(error);
    } finally {
      this.setLoading(false);
    }
  }
}
