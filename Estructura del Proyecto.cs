import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormGroup, FormsModule, FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { firstValueFrom } from 'rxjs';

// Angular Material
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatButtonModule } from '@angular/material/button';

// Servicios
import { AuthService } from '../../../../core/services/auth.service';
import { SessionIdleService } from '../../../../core/services/session-idle.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatButtonModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  /** UI */
  hidePassword = true;
  isLoading = false;
  errorMessage = '';

  /** Formulario reactivo */
  loginForm: FormGroup;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly fb: FormBuilder,
    private readonly snackBar: MatSnackBar,
    private readonly idle: SessionIdleService
  ) {
    this.loginForm = this.fb.group({
      userName: ['', [Validators.required, Validators.maxLength(10)]],
      password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]]
    });
  }

  /** Mostrar/ocultar contraseña */
  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  /** Encapsula cambios de estado de carga y bloquea el form */
  private setLoading(loading: boolean): void {
    this.isLoading = loading;
    loading
      ? this.loginForm.disable({ emitEvent: false })
      : this.loginForm.enable({ emitEvent: false });
  }

  /** Normaliza y muestra el error de login */
  private handleLoginError(error: unknown): void {
    this.errorMessage = (error as Error)?.message || 'Ocurrió un error durante el inicio de sesión';
    this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
  }

  /** Enviar formulario: hace login, inicia monitoreo de sesión y navega */
  async login(): Promise<void> {
    if (this.loginForm.invalid || this.isLoading) return;

    this.errorMessage = '';
    this.setLoading(true);

    try {
      const { userName, password } = this.loginForm.value;
      await firstValueFrom(this.authService.login(userName, password));

      // Inicia monitoreo de inactividad + verificación periódica de JWT
      this.idle.startWatching();

      await this.router.navigate(['/tarjetas']);
    } catch (error) {
      this.handleLoginError(error);
    } finally {
      this.setLoading(false);
    }
  }
}
