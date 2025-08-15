import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
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
    CommonModule,
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

    // Enciende overlay y limpia errores previos
    this.isLoading = true;
    this.errorMessage = '';
    const serverCtrl = this.loginForm.get('server');
    serverCtrl?.setErrors(null);
    serverCtrl?.markAsPristine();
    serverCtrl?.markAsUntouched();
    serverCtrl?.updateValueAndValidity();

    this.authService.login(userName, password)
      .pipe(
        take(1),
        finalize(() => { this.isLoading = false; }) // apaga SIEMPRE el overlay (éxito o error)
      )
      .subscribe({
        next: _ => this.router.navigate(['/tarjetas']),
        error: (error: HttpErrorResponse) => {
          // Mensaje robusto incluso si el 401 no trae body
          const msgFromApi =
            (error?.error && (error.error.codigo?.message || error.error.message)) ||
            (typeof error?.error === 'string' ? error.error : null);

          this.errorMessage = msgFromApi || 'Ocurrió un error durante el inicio de sesión';

          // Activa estado de error para que el mat-error se muestre
          serverCtrl?.setErrors({ server: true });
          serverCtrl?.markAsDirty();
          serverCtrl?.markAsTouched();
          serverCtrl?.updateValueAndValidity();

          // Opcional: snackbar; comenta si no deseas doble mensaje
          this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
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

        <!-- Error GENERAL bajo el botón con estilo Material (sin recuadro) -->
        <mat-form-field appearance="outline" class="full-width error-field">
          <!-- Input dummy: mantiene estado del form-field -->
          <input matInput formControlName="server" class="server-dummy-input" tabindex="-1" aria-hidden="true" />
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
/* ---------- Tu CSS existente (container, card, inputs, botón, overlay, etc.) ---------- */

body { background-color: rgb(241, 239, 239); }

.login-container { position: relative; }

.content-form {
  position: absolute;
  top: 270px;
  left: 50%;
  transform: translate(-50%, -50%);
  max-width: 400px;
  width: 100%;
  height: 400px;
  background-color: #fff;
  padding: 25px;
  border-radius: 12px;
  box-shadow: 1px 1px 5px rgba(0, 0, 0, 0.349);
}

.content-form.blocked { pointer-events: none; filter: grayscale(0.2); opacity: 0.95; }

.content-title-login { display: flex; justify-content: center; align-items: center; width: 100%; padding: 30px 0; }

.imgLogo { position: absolute; top: -60px; width: 110px; height: 100px; display: block; }

.title-login { font-size: 1.5rem; font-weight: bold; }

.form { height: 80%; display: flex; flex-direction: column; gap: 20px; }

.inputPass { position: relative; display: flex; }

.iconPass { position: absolute; top: -10px; right: 10px; width: 30px; height: 30px; border: none; }

.full-width { width: 100%; border: none !important; background-color: #fff !important; }

.form input { width: 100%; height: 30px !important; outline: none; }

.loginNow {
  background-color: #e4041c; color: #fff; padding: 13px 0; border-radius: 10px; border: none;
  font-size: 1rem; font-weight: bold; cursor: pointer;
}

.loginNow:hover { box-shadow: 2px 2px 3px rgba(0, 0, 0, 0.349); }

/* ---------- OVERLAY ---------- */
.overlay {
  position: fixed; inset: 0; background: rgba(0, 0, 0, 0.35);
  z-index: 9999; display: grid; place-items: center; backdrop-filter: blur(1px);
}
.overlay-content {
  min-width: 240px; max-width: 90vw; padding: 20px 22px; border-radius: 12px; background: #fff;
  box-shadow: 0 10px 30px rgba(0,0,0,0.25); display: flex; align-items: center; gap: 14px;
}
.overlay-text { font-weight: 600; }

/* ---------- Form-field de error general (SIN RECUADRO) ---------- */
/* Oculta completamente la “caja” del form-field: outline, infix, flex y la línea inferior */
.error-field .mat-mdc-form-field-flex,
.error-field .mdc-notched-outline,
.error-field .mdc-line-ripple,
.error-field .mat-mdc-form-field-infix,
.error-field .mat-mdc-form-field-icon-prefix,
.error-field .mat-mdc-form-field-icon-suffix {
  display: none !important;
}

/* Asegura que la zona del mensaje (subscript) sí quede visible y sin espacios extra */
.error-field .mat-mdc-form-field-subscript-wrapper {
  display: block !important;
  margin-top: 8px;
  padding: 0;
  position: static; /* evita colapsar */
}

/* Input dummy: invisible pero mantiene el estado de error del form-field */
.server-dummy-input {
  position: absolute;
  width: 0; height: 0; opacity: 0; pointer-events: none; border: 0; margin: 0; padding: 0; line-height: 0;
}





