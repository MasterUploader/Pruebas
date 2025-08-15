import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { MatInputModule } from '@angular/material/input';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
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
    MatCardModule,
    FormsModule,
    MatFormFieldModule,
    ReactiveFormsModule,
    MatSelectModule,
    MatInputModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  hidePassword = true;
  errorMessage: string = '';
  isLoading = false;

  /** Form reactivo con control 'server' para mostrar mat-error general */
  loginForm: FormGroup;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly fb: FormBuilder,
    private readonly snackBar: MatSnackBar
  ) {
    this.loginForm = this.fb.group({
      userName: ['', Validators.required],
      password: ['', Validators.required],
      server: [''] // control dummy para mat-error general
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  login(): void {
    if (this.loginForm.invalid || this.isLoading) return;

    const { userName, password } = this.loginForm.value;

    // Encendemos loader y limpiamos errores previos
    this.isLoading = true;
    this.errorMessage = '';
    this.loginForm.get('server')?.setErrors(null);

    this.authService.login(userName, password)
      .pipe(
        take(1),                          // sólo la primera respuesta
        finalize(() => { this.isLoading = false; }) // <-- APAGA SIEMPRE EL LOADER
      )
      .subscribe({
        next: _data => {
          // si necesitas validar códigos de respuesta, hazlo aquí
          this.router.navigate(['/tarjetas']);
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage =
            error?.error?.codigo?.message ||
            error?.error?.message ||
            'Ocurrió un error durante el inicio de sesión';

          // activa estado de error para que mat-error se muestre
          this.loginForm.get('server')?.setErrors({ server: true });

          // opcional: snack
          this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
        }
      });
  }
}
