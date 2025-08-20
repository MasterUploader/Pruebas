Este es el TS completo

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
import { finalize, take } from 'rxjs/operators';

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

  login(): void {
    if (this.loginForm.invalid || this.isLoading) return;

    const { userName, password } = this.loginForm.value;

    // Reset de estado previo
    this.errorMessage = '';

    // Deshabilita controles correctamente (evita warnings)
    this.isLoading = true;
    this.loginForm.disable({ emitEvent: false });

    this.authService.login(userName, password)
      .pipe(
        take(1),
        finalize(() => {
          this.isLoading = false;
          this.loginForm.enable({ emitEvent: false }); // reactivar SIEMPRE
        })
      )
      .subscribe({
        next: () => this.router.navigate(['/tarjetas']),
        error: (error: HttpErrorResponse) => {
          const msgFromApi =
            (error?.error && (error.error.codigo?.message || error.error.message)) ||
            (typeof error?.error === 'string' ? error.error : null);

          this.errorMessage = msgFromApi || 'Ocurrió un error durante el inicio de sesión';

          // Notificación flotante (si no la quieres, quita esta línea)
          this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
        }
      });
  }
}
