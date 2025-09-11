Este es el código que me solicitas como lo tengo por el momento:

// app.config.ts
import { ApplicationConfig } from '@angular/core';
import { provideRouter, withHashLocation } from '@angular/router';
import { routes } from './app.routes';

import { provideClientHydration } from '@angular/platform-browser';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { LocationStrategy, HashLocationStrategy } from '@angular/common';

import {
  provideHttpClient,
  withFetch,
  withInterceptorsFromDi, // toma interceptores registrados vía DI
  HTTP_INTERCEPTORS
} from '@angular/common/http';

import { AuthInterceptor } from './core/services/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    // Router standalone con hash (#)
    provideRouter(routes, withHashLocation()),

    // Hidratación/animaciones (opcional)
    provideClientHydration(),
    provideAnimationsAsync(),

    // ✅ HttpClient (sin HttpClientModule) + uso de fetch + interceptores DI
    provideHttpClient(
      withFetch(),
      withInterceptorsFromDi()
    ),

    // Interceptor basado en clase (se inyecta vía DI)
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },

    // Estrategia de navegación con hash
    { provide: LocationStrategy, useClass: HashLocationStrategy }
  ]
};



import { Component, NgModule } from '@angular/core';
import { RouterOutlet, RouterModule } from '@angular/router';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { FooterComponent } from './shared/components/footer/footer.component';

import { routes } from './app.routes';

@Component({
    selector: 'app-root',
    imports: [RouterOutlet,  NavbarComponent, FooterComponent, ],
    templateUrl: './app.component.html',
    styleUrl: './app.component.css'
})
export class AppComponent {
  title = 'EmbosadoTarjetasDebito';
}

@NgModule({
  declarations: [],
  imports:[
    AppComponent,
    RouterModule.forChild(routes)
  ]
  })
  export class AppModule{}


import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './modules/auth/components/login/login.component';
import { ConsultaTarjetaComponent } from './modules/tarjetas/components/consulta-tarjeta/consulta-tarjeta.component';
import { authGuard } from './core/guards/auth.guard';
import { loginGuard } from './core/guards/login.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent, canActivate: [loginGuard], title: 'Iniciar sesión' },
  { path: 'tarjetas', component: ConsultaTarjetaComponent, canActivate: [authGuard], title: 'Tarjetas' },
  { path: '', pathMatch: 'full', redirectTo: 'tarjetas' },
  { path: '**', redirectTo: 'tarjetas' }
];

export const AppRoutingModule = RouterModule.forRoot(routes);




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


import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpContextToken, HTTP_INTERCEPTORS, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Injectable } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';
//import { error } from 'console';

export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService, private router: Router) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const currentUser = this.authService.currentUserValue;

    //Validamos si la solictid actual requiere omitir la autenticacion
    if (req.context.get(SKIP_AUTH)) {
      return next.handle(req);
    }

    if (currentUser && currentUser.token) {
      req = req.clone({
        setHeaders: {
          Authorization: `Bearer ${currentUser.token}`
        }
      });
    }
    return next.handle(req).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 || error.status === 403) {
          this.router.navigate(['/login']);
        }
        return throwError(error);
      })
    );
  }
}


export const authInterceptorProvider = {
  provide: HTTP_INTERCEPTORS,
  useClass: () => AuthInterceptor,
  multi: true,
};


import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Si está activa la sesión, puede pasar
  if (auth.sessionIsActive()) return true;

  // Mejor devolver UrlTree (no navegar imperativamente)
  return router.createUrlTree(['/login']);
};


import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Evita acceder a /login si ya hay sesión activa.
 * Si la sesión está activa, redirige a /tarjetas.
 * Si no, permite ver el login.
 */
export const loginGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.sessionIsActive()
    ? router.createUrlTree(['/tarjetas'])
    : true;
};


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

  /** Llama al servicio, navega si tod ok; UI robusta con try/finally */
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


<div class="login-container">
  <mat-card
    class="content-form"
    [class.blocked]="isLoading"
    [attr.aria-busy]="isLoading"
  >
    <mat-card-header class="content-title-login">
      <img
        src="../login/../../../../../assets/logo.png"
        alt="Logo"
        class="imgLogo"
      />
      <mat-card-title class="title-login">Iniciar Sesión</mat-card-title>
    </mat-card-header>

    <mat-card-content>
      <form
        (ngSubmit)="login()"
        [formGroup]="loginForm"
        class="form"
        novalidate
      >
        <!-- USUARIO -->
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Usuario</mat-label>
          <input
            matInput
            formControlName="userName"
            id="userName"
            name="userName"
            placeholder="Ingresa tu usuario"
            autocomplete="username"
            maxlength="150"
          />
          <mat-icon matSuffix aria-hidden="true">person</mat-icon>

          <!-- Hints y errores -->
          <mat-hint>People Soft</mat-hint>
          <mat-hint align="end">
            {{ loginForm.controls["userName"].value?.length || 0 }}/10
          </mat-hint>

          @if (loginForm.controls['userName'].hasError('required')) {
          <mat-error>El usuario es obligatorio</mat-error>
          } @else if (loginForm.controls['userName'].hasError('maxlength')) {
          <mat-error>El usuario no puede superar los 10 caracteres</mat-error>
          }
        </mat-form-field>

        <!-- CONTRASEÑA -->
        <mat-form-field appearance="outline" class="full-width inputPass">
          <mat-label>Contraseña</mat-label>
          <input
            matInput
            [type]="hidePassword ? 'password' : 'text'"
            formControlName="password"
            id="password"
            name="password"
            placeholder="Ingresa tu contraseña"
            autocomplete="current-password"
            maxlength="128"
          />
          <button
            type="button"
            mat-icon-button
            matSuffix
            (click)="togglePasswordVisibility()"
            [attr.aria-label]="
              hidePassword ? 'Mostrar contraseña' : 'Ocultar contraseña'
            "
            [attr.aria-pressed]="!hidePassword"
          >
            <mat-icon>{{
              hidePassword ? "visibility_off" : "visibility"
            }}</mat-icon>
          </button>

          <!-- Hints y errores -->
          <mat-hint>Ingrese su Contraseña</mat-hint>
          <mat-hint align="end">
            {{ loginForm.controls["password"].value?.length || 0 }}/100
          </mat-hint>

          @if (loginForm.controls['password'].hasError('required')) {
          <mat-error>La contraseña es obligatoria.</mat-error>
          } @else if (loginForm.controls['password'].hasError('maxlength')) {
          <mat-error>La contraseña no puede superar los  Máximo 100 caracteres.</mat-error>
          }
        </mat-form-field>

        <!-- BOTÓN -->
        <button
          mat-raised-button
          type="submit"
          class="loginNow custom-login-btn"
          [disabled]="isLoading || loginForm.invalid"
        >
          Entrar
        </button>

        <!-- ERROR GENERAL DEL API BAJO EL BOTÓN -->
        @if (errorMessage) {
        <div class="inline-error mat-mdc-form-field-error" role="alert">
          <mat-icon aria-hidden="true">error</mat-icon>
          <span>{{ errorMessage }}</span>
        </div>
        }
      </form>
    </mat-card-content>
  </mat-card>

  <!-- OVERLAY Autenticando -->
  @if (isLoading) {
  <div class="overlay" role="alert" aria-live="assertive">
    <div class="overlay-content" role="dialog" aria-label="Autenticando">
      <mat-progress-spinner
        mode="indeterminate"
        diameter="48"
      ></mat-progress-spinner>
      <div class="overlay-text">Autenticando</div>
    </div>
  </div>
  }
</div>




export const environment = {
    production: true,
   // apiBaseUrl: 'https://hncstg015089wws:4043' // DEV
   //apiBaseUrl: 'http://hncstg015189wws:4043' //UAT
    apiBaseUrl: 'https://localhost:7275' //Localhost
  // apiBaseUrl: 'https://hncstg010244wap:8044'  //PROD
}







