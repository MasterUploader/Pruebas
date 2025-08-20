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
import { of } from 'rxjs'; // para devolver un observable vacío en catchError

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

  /** Mensaje general del API que mostramos debajo del botón y en snackbar */
  errorMessage = '';

  /** Form reactivo con validaciones y límites para los hints/contadores */
  loginForm: FormGroup;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly fb: FormBuilder,
    private readonly snackBar: MatSnackBar
  ) {
    this.loginForm = this.fb.group({
      userName: [
        '',
        [
          Validators.required,
          Validators.maxLength(150)
        ]
      ],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(6),
          Validators.maxLength(128)
        ]
      ]
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  /** Enciende/apaga loading y (des)habilita el form sin warnings */
  private setLoading(loading: boolean): void {
    this.isLoading = loading;
    loading
      ? this.loginForm.disable({ emitEvent: false })
      : this.loginForm.enable({ emitEvent: false });
  }

  /** Extrae un mensaje entendible del error del backend */
  private getApiErrorMessage(error: unknown): string {
    const err = error as HttpErrorResponse;
    const fromApi =
      (err?.error && (err.error?.codigo?.message || err.error?.message)) ||
      (typeof err?.error === 'string' ? err.error : null);
    return fromApi || 'Ocurrió un error durante el inicio de sesión';
  }

  /** Versión RxJS con baja complejidad */
  login(): void {
    if (this.loginForm.invalid || this.isLoading) return;

    this.errorMessage = '';
    this.setLoading(true);

    const { userName, password } = this.loginForm.value;

    this.authService.login(userName, password).pipe(
      take(1),
      tap(() => { this.router.navigate(['/tarjetas']); }),
      catchError((error) => {
        this.errorMessage = this.getApiErrorMessage(error);
        this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
        // Devolvemos un observable “neutro” para que finalize() se ejecute y no reviente la cadena
        return of(null);
      }),
      finalize(() => this.setLoading(false))
    ).subscribe();
  }
}
