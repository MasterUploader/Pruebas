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
import { catchError, tap, take, finalize } from 'rxjs/operators';
import { of } from 'rxjs';

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
  /** Controla visibilidad de contraseña */
  hidePassword = true;

  /** Bandera de carga para overlay + deshabilitar form */
  isLoading = false;

  /** Mensaje general de error (se muestra bajo el botón y en snackbar) */
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

  /** Alterna visibilidad del campo password */
  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  /** Activa/desactiva loading y (des)habilita el form de forma segura (evita warnings) */
  private setLoading(loading: boolean): void {
    this.isLoading = loading;
    loading
      ? this.loginForm.disable({ emitEvent: false })
      : this.loginForm.enable({ emitEvent: false });
  }

  /** Navegación encapsulada (reduce ramas en login) */
  private gotoTarjetas(): void {
    this.router.navigate(['/tarjetas']);
  }

  /** Extrae un mensaje entendible del error del backend */
  private getApiErrorMessage(error: unknown): string {
    const err = error as HttpErrorResponse;
    const fromApi =
      (err?.error && (err.error?.codigo?.message || err.error?.message)) ||
      (typeof err?.error === 'string' ? err.error : null);
    return fromApi || 'Ocurrió un error durante el inicio de sesión';
  }

  /**
   * Login (versión RxJS con baja complejidad):
   * - take(1) evita múltiples emisiones
   * - tap navega en éxito
   * - catchError maneja mensaje + snackbar y devuelve of(null) para no romper la cadena
   * - finalize apaga el loading SIEMPRE
   */
  login(): void {
    if (this.loginForm.invalid || this.isLoading) return;

    this.errorMessage = '';
    this.setLoading(true);

    const { userName, password } = this.loginForm.value;

    this.authService.login(userName, password).pipe(
      take(1),
      tap(() => this.gotoTarjetas()),
      catchError((error) => {
        this.errorMessage = this.getApiErrorMessage(error);
        this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
        return of(null); // mantiene viva la cadena para que finalize() corra
      }),
      finalize(() => this.setLoading(false))
    ).subscribe();
  }
}
