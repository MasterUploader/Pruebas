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
  standalone: true, // <- IMPORTANTE para poder usar "imports" sin módulo
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
  /** Controla mostrar/ocultar la contraseña */
  hidePassword = true;

  /** Mensaje de error mostrado bajo el botón */
  errorMessage: string = '';

  /** Form reactivo */
  loginForm: FormGroup;

  /**
   * Bandera de carga:
   * - Muestra el overlay flotante "Cargando..."
   * - Deshabilita botón/inputs y bloquea clicks
   */
  isLoading = false;

  constructor(
    private readonly authService: AuthService,
    private readonly router: Router,
    private readonly fb: FormBuilder,
    private readonly snackBar: MatSnackBar
  ) {
    // Construcción del formulario con validaciones mínimas
    this.loginForm = this.fb.group({
      userName: ['', Validators.required],
      password: ['', Validators.required]
    });
  }

  /**
   * Envía las credenciales al API.
   * Muestra overlay de carga y bloquea interacciones hasta recibir respuesta.
   */
  login(): void {
    // Si el form es inválido o ya hay una petición en curso, no hacemos nada
    if (this.loginForm.invalid || this.isLoading) {
      return;
    }

    const { userName, password } = this.loginForm.value;

    // Activamos el modo de carga:
    // - aparecerá el overlay
    // - el botón "Entrar" se deshabilita
    // - se bloquean clicks en la tarjeta del login
    this.isLoading = true;

    this.authService.login(userName, password).subscribe({
      next: (data) => {
        // Limpia mensaje de error previo si existiera
        this.errorMessage = '';

        // Ejemplo: tu backend parece traer data.codigo.message
        // Si necesitas validar códigos, hazlo aquí.
        // this.errorMessage = data.codigo?.message ?? '';

        // Redirigimos a la ruta solicitada
        this.router.navigate(['/tarjetas']);
      },
      error: (error: HttpErrorResponse) => {
        // Mensaje de error robusto
        this.errorMessage =
          error?.error?.codigo?.message ||
          error?.error?.message ||
          'Ocurrió un error durante el inicio de sesión';

        // Notificación flotante opcional (si no la quieres, bórrala)
        this.snackBar.open(this.errorMessage, 'Cerrar', { duration: 5000 });
      },
      complete: () => {
        // Siempre apagar el loading cuando termina
        this.isLoading = false;
      }
    });
  }

  /** Alterna la visibilidad de la contraseña */
  togglePasswordVisibility(): void {
    this.hidePassword = !this.hidePassword;
  }
}


<!-- Contenedor global del login -->
<div class="login-container">

  <!-- Tarjeta del formulario -->
  <!-- La clase 'blocked' evita clics mientras isLoading=true -->
  <mat-card class="content-form" [class.blocked]="isLoading" aria-busy="{{ isLoading }}">
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

        <!-- Error del servidor bajo el botón -->
        @if (errorMessage) {
          <div class="alert alert-danger">{{ errorMessage }}</div>
        }
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



/* Fondo general */
body {
  background-color: rgb(241, 239, 239);
}

/* Contenedor principal del login */
.login-container {
  position: relative;
}

/* Tarjeta del formulario */
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

/* Mientras está cargando, bloquea interacciones sobre la tarjeta */
.content-form.blocked {
  pointer-events: none;   /* No permite clics/inputs */
  filter: grayscale(0.2); /* Sutil feedback visual opcional */
  opacity: 0.95;
}

.content-title-login {
  display: block;
  width: 100%;
  padding: 30px 0;
  text-align: center;
  display: flex;
  justify-content: center;
  align-items: center;
}

.imgLogo {
  position: absolute;
  top: -60px;
  width: 110px;
  height: 100px;
  display: block;
}

.title-login {
  font-size: 1.5rem;
  font-weight: bold;
}

.form {
  height: 80%;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

/* Campo de password con icono */
.inputPass {
  position: relative;
  display: flex;
}

.iconPass {
  position: absolute;
  top: -10px;
  right: 10px;
  width: 30px;
  height: 30px;
  border: none;
}

.full-width {
  border: none !important;
  background-color: #fff !important;
  width: 100%;
}

.form input {
  width: 100%;
  height: 30px !important;
  outline: none;
}

/* Botón principal */
.loginNow {
  background-color: #e4041c;
  color: #fff;
  padding: 13px 0;
  border-radius: 10px;
  border: none;
  font-size: 1rem;
  font-weight: bold;
  cursor: pointer;
}

.loginNow:hover {
  box-shadow: 2px 2px 3px rgba(0, 0, 0, 0.349);
}

/* Mensaje de error bajo el botón (puedes reemplazarlo por mat-error si prefieres) */
.alert.alert-danger {
  background: #fde7e9;
  color: #b3261e;
  border: 1px solid #f5c2c7;
  padding: 10px 12px;
  border-radius: 8px;
}

/* ---------- OVERLAY DE CARGA ---------- */
.overlay {
  position: fixed;          /* Cubrir pantalla completa */
  inset: 0;                 /* top/right/bottom/left: 0 */
  background: rgba(0, 0, 0, 0.35);
  z-index: 9999;            /* Por encima del card */
  display: grid;
  place-items: center;
  backdrop-filter: blur(1px);
}

.overlay-content {
  min-width: 240px;
  max-width: 90vw;
  padding: 20px 22px;
  border-radius: 12px;
  background: #fff;
  box-shadow: 0 10px 30px rgba(0,0,0,0.25);
  display: flex;
  align-items: center;
  gap: 14px;
}

.overlay-text {
  font-weight: 600;
}

/* ---------- Media Queries ---------- */
@media only screen and (max-width: 992px) {
  .content-form {
    max-width: 320px;
    height: 380px;
  }
  .form {
    gap: 50px;
  }
}
