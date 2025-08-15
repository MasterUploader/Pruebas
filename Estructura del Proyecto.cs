import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AuthService } from 'src/app/core/services/auth.service';
import { MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {

  loginForm!: FormGroup;
  hidePassword: boolean = true; // Para alternar la visibilidad de la contraseña
  loading: boolean = false; // Para mostrar el estado de cargando
  apiError: string | null = null; // Error del API que se mostrará debajo del botón

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    // Inicializamos el formulario reactivo con validaciones
    this.loginForm = this.fb.group({
      userName: ['', Validators.required],
      password: ['', Validators.required]
    });
  }

  /**
   * Método que se ejecuta al presionar el botón "Entrar".
   * Si el formulario es válido, realiza la llamada al API de autenticación.
   */
  onSubmit(): void {
    this.apiError = null; // Limpiar errores previos

    if (this.loginForm.invalid) {
      // Marca todos los campos como tocados para que se muestren los errores
      this.loginForm.markAllAsTouched();
      return;
    }

    this.loading = true;

    this.authService.login(this.loginForm.value).subscribe({
      next: (response) => {
        this.loading = false;
        // Aquí puedes redirigir o manejar la sesión según tu lógica
      },
      error: (err) => {
        this.loading = false;

        // Mostramos error en notificación flotante
        this.snackBar.open(
          err.error?.message || 'Error al iniciar sesión',
          'Cerrar',
          { duration: 4000 }
        );

        // También lo mostramos debajo del botón en el HTML
        this.apiError = err.error?.message || 'Error al iniciar sesión';
      }
    });
  }
}


<div class="login-container">
  <mat-card class="login-card">
    <mat-card-header>
      <mat-card-title>Iniciar Sesión</mat-card-title>
    </mat-card-header>

    <mat-card-content>
      <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" novalidate>
        
        <!-- Usuario -->
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Usuario</mat-label>
          <input matInput formControlName="userName" />
          @if (loginForm.controls['userName'].hasError('required')) {
            <mat-error>El usuario es obligatorio.</mat-error>
          }
        </mat-form-field>

        <!-- Contraseña -->
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Contraseña</mat-label>
          <input
            matInput
            [type]="hidePassword ? 'password' : 'text'"
            formControlName="password"
          />
          <button
            mat-icon-button
            matSuffix
            (click)="hidePassword = !hidePassword"
            type="button"
          >
            <mat-icon>{{ hidePassword ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>
          @if (loginForm.controls['password'].hasError('required')) {
            <mat-error>La contraseña es obligatoria.</mat-error>
          }
        </mat-form-field>

        <!-- Botón -->
        <button
          mat-raised-button
          color="primary"
          class="login-button"
          type="submit"
          [disabled]="loading"
        >
          Entrar
        </button>

        <!-- Error del API -->
        @if (apiError) {
          <mat-error class="api-error">{{ apiError }}</mat-error>
        }
      </form>
    </mat-card-content>
  </mat-card>
</div>
