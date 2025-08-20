OK, deacuerdo haz el cambio acá te dejo el codigo actual:

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
import { HttpErrorResponse } from '@angular/common/http';
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
  /** Control de UI */
  hidePassword = true;
  isLoading = false;

  /** Mensaje general del API mostrado bajo el botón y en snackbar */
  errorMessage = '';

  /** Form reactivo con validaciones */
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

  /** Alterna visibilidad de la contraseña */
  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  /** Enciende/apaga el loading y (des)habilita el form sin warnings de Reactive Forms */
  private setLoading(loading: boolean): void {
    this.isLoading = loading;
    loading
      ? this.loginForm.disable({ emitEvent: false })
      : this.loginForm.enable({ emitEvent: false });
  }

  /** Navegación encapsulada (reduce líneas en login) */
  private gotoTarjetas(): Promise<boolean> {
    return this.router.navigate(['/tarjetas']);
  }

  /** Normaliza el mensaje de error del backend */
  private getApiErrorMessage(error: unknown): string {
    const err = error as HttpErrorResponse;
    const fromApi =
      (err?.error && (err.error?.codigo?.message || err.error?.message)) ||
      (typeof err?.error === 'string' ? err.error : null);
    return fromApi || 'Ocurrió un error durante el inicio de sesión';
  }

  /** Manejo centralizado de errores (reduce complejidad en login) */
  private handleLoginError(error: unknown): void {
    this.errorMessage = this.getApiErrorMessage(error);
    this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
  }

  /** Versión async/await con complejidad reducida */
  async login(): Promise<void> {
    // Guard clauses simples → menos ramas
    if (this.loginForm.invalid || this.isLoading) return;

    this.errorMessage = '';
    this.setLoading(true);

    try {
      const { userName, password } = this.loginForm.value;
      await firstValueFrom(this.authService.login(userName, password));
      await this.gotoTarjetas();
    } catch (error) {
      this.handleLoginError(error);
    } finally {
      this.setLoading(false);
    }
  }
}



import { Injectable , Inject, PLATFORM_ID, inject } from '@angular/core';
import { BehaviorSubject, Observable, map } from 'rxjs';
import { Router } from '@angular/router';
import { isPlatformBrowser } from '@angular/common';
import { LoginResponse } from '../models/login.Response.model';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
//import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
//import exp from 'node:constants';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private currentUserSubject: BehaviorSubject<LoginResponse | null>;
  public currentUser: Observable<LoginResponse | null>;

  private currentUserSubject2 = new  BehaviorSubject<any>(null); //Prueba
  public currentUser2 = this.currentUserSubject2.asObservable(); //Prueba


  private sessionActive: BehaviorSubject<boolean>;
  private apiUrl = environment.apiBaseUrl;
  public agenciaAperturaCodigo: string | null = null;
  public agenciaImprimeCodigo: string | null = null;
  public usuarioICBS: string = "";
  private isBrowser: boolean = false;

  constructor(@Inject(PLATFORM_ID) private platformId: Object, private router: Router, private http: HttpClient) {
    let initialUser: LoginResponse | null;
    let sessionIsActive: boolean = false;
    this.isBrowser = isPlatformBrowser(this.platformId);

    if (isPlatformBrowser(this.platformId)) {
      initialUser = JSON.parse(localStorage.getItem('currentUser') || 'null');
      sessionIsActive = this.sessionIsActive();
    } else {
      initialUser = null;
    }

    this.currentUserSubject = new BehaviorSubject<LoginResponse | null>(initialUser);
    this.currentUser = this.currentUserSubject.asObservable();
    this.sessionActive = new BehaviorSubject<boolean>(sessionIsActive);
  }

  login(userName: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/api/Auth/Login`, { user: userName, password: password })
      .pipe(map(response => {
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

          this.router.navigate(['/tarjetas']);
        }
        return response;
      }));
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

    } else {
      return false;
    }

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



