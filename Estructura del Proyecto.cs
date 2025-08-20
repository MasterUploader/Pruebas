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
      userName: ['', [Validators.required, Validators.maxLength(150)]],
      password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(128)]]
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
