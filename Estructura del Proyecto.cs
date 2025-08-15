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
      // Control dummy para poder mostrar un mat-error general estilado por Material
      server: ['']
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }

  login(): void {
    if (this.loginForm.invalid || this.isLoading) return;

    const { userName, password } = this.loginForm.value;
    this.isLoading = true;

    // limpiar error previo y estado del control 'server'
    this.errorMessage = '';
    this.loginForm.get('server')?.setErrors(null);

    this.authService.login(userName, password).subscribe({
      next: _data => {
        // si tu backend trae códigos/msgs, puedes validarlos aquí
        this.router.navigate(['/tarjetas']);
      },
      error: (error: HttpErrorResponse) => {
        this.errorMessage =
          error?.error?.codigo?.message ||
          error?.error?.message ||
          'Ocurrió un error durante el inicio de sesión';

        // Marcamos el control para que el mat-form-field entre en estado de error
        this.loginForm.get('server')?.setErrors({ server: true });

        // Si no deseas snackbar, comenta la siguiente línea
        this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
      },
      complete: () => {
        this.isLoading = false;
      }
    });
  }
}

<div class="login-container">
  <mat-card class="content-form"
            [class.blocked]="isLoading"
            [attr.aria-busy]="isLoading">
    <mat-card-header class="content-title-login">
      <img src="../../assets/logo.png" alt="Logo" class="imgLogo" />
      <mat-card-title class="title-login">Iniciar Sesión</mat-card-title>
    </mat-card-header>

    <mat-card-content>
      <form (ngSubmit)="login()" [formGroup]="loginForm" class="form" novalidate>

        <!-- Usuario -->
        <mat-form-field appearance="fill" class="full-width">
          <input
            matInput
            placeholder="Introduce tu usuario"
            formControlName="userName"
            id="userName"
            name="userName"
            required
            autocomplete="username"
            [disabled]="isLoading" />
          <mat-error *ngIf="loginForm.controls['userName']?.hasError('required')">
            El usuario es obligatorio.
          </mat-error>
        </mat-form-field>

        <!-- Contraseña -->
        <mat-form-field appearance="fill" class="full-width inputPass">
          <input
            matInput
            [type]="hidePassword ? 'password' : 'text'"
            placeholder="Introduce tu contraseña"
            formControlName="password"
            id="password"
            name="password"
            required
            autocomplete="current-password"
            [disabled]="isLoading" />
          <button
            class="iconPass"
            type="button"
            mat-icon-button
            matSuffix
            (click)="togglePasswordVisibility()"
            [attr.aria-label]="hidePassword ? 'Mostrar contraseña' : 'Ocultar contraseña'"
            [attr.aria-pressed]="!hidePassword"
            [disabled]="isLoading">
            <mat-icon class="iconPass">{{ hidePassword ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>

          <mat-error *ngIf="loginForm.controls['password']?.hasError('required')">
            La contraseña es obligatoria.
          </mat-error>
        </mat-form-field>

        <!-- Botón de login -->
        <button
          mat-raised-button
          color="primary"
          type="submit"
          class="loginNow"
          [disabled]="isLoading || loginForm.invalid">
          Entrar
        </button>

        <!-- Mensaje de error GENERAL bajo el botón alineado como Material -->
        <mat-form-field appearance="fill" class="full-width no-input">
          <!-- input invisible asociado al control 'server' para activar estado de error -->
          <input matInput formControlName="server" style="display:none" tabindex="-1" aria-hidden="true" />
          <mat-error *ngIf="errorMessage">{{ errorMessage }}</mat-error>
        </mat-form-field>
      </form>
    </mat-card-content>
  </mat-card>

  <!-- Overlay flotante de CARGA -->
  @if (isLoading) {
    <div class="overlay" role="alert" aria-live="assertive">
      <div class="overlay-content" role="dialog" aria-label="Cargando">
        <mat-progress-spinner mode="indeterminate" diameter="48"></mat-progress-spinner>
        <div class="overlay-text">Cargando…</div>
      </div>
    </div>
  }
</div>
