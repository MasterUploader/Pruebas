import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

/**
 * Guard que protege rutas privadas:
 * - Revisa `sessionIsActive()` (expiración local).
 * - Revisa el `exp` real del JWT (si existe).
 * Si no cumple, cierra sesión y redirige al login.
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const localActive = auth.sessionIsActive();

  const exp = auth.getJwtExp();
  const jwtActive = exp ? (exp * 1000 > Date.now()) : localActive;

  if (localActive && jwtActive) {
    return true;
  }

  auth.logout();
  return router.createUrlTree(['/login']);
};










import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent, HttpContextToken, HTTP_INTERCEPTORS, HttpErrorResponse } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Injectable } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';

export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService, private router: Router) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const currentUser = this.authService.currentUserValue;

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
        if (error.status === 401 || error.status === 403 || error.status === 440) {
          this.authService.logout(); // limpia storage y redirige a /login
        }
        return throwError(() => error);
      })
    );
  }
}

export const authInterceptorProvider = {
  provide: HTTP_INTERCEPTORS,
  useClass: () => AuthInterceptor,
  multi: true,
};









import { Component, OnInit, NgModule } from '@angular/core';
import { RouterOutlet, RouterModule } from '@angular/router';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { FooterComponent } from './shared/components/footer/footer.component';
import { routes } from './app.routes';
import { SessionIdleService } from './core/services/session-idle.service';
import { AuthService } from './core/services/auth.service';
import { MatDialogModule } from '@angular/material/dialog';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NavbarComponent, FooterComponent, MatDialogModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'EmbosadoTarjetasDebito';

  constructor(
    private readonly idle: SessionIdleService,
    private readonly auth: AuthService
  ) {}

  ngOnInit(): void {
    if (this.auth.sessionIsActive()) {
      this.idle.startWatching();
    }
  }
}

@NgModule({
  declarations: [],
  imports: [
    AppComponent,
    RouterModule.forChild(routes)
  ]
})
export class AppModule {}










// ... tus imports existentes ...
import { SessionIdleService } from '../../../../core/services/session-idle.service';

@Component({
  selector: 'app-login',
  standalone: true,
  // ... imports como ya los tienes ...
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  // ... tu código actual ...
  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly fb: FormBuilder,
    private readonly snackBar: MatSnackBar,
    private readonly idle: SessionIdleService   // <--- NUEVO
  ) {
    // ... tu constructor actual ...
  }

  async login(): Promise<void> {
    if (this.loginForm.invalid || this.isLoading) return;

    this.errorMessage = '';
    this.setLoading(true);

    try {
      const { userName, password } = this.loginForm.value;
      await firstValueFrom(this.authService.login(userName, password));

      // Inicia monitoreo de inactividad + verificación JWT
      this.idle.startWatching();

      await this.router.navigate(['/tarjetas']);
    } catch (error) {
      this.handleLoginError(error);
    } finally {
      this.setLoading(false);
    }
  }
}




























