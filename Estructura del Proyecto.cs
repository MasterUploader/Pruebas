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

        <!-- USUARIO -->
        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Usuario</mat-label>
          <input
            matInput
            formControlName="userName"
            id="userName"
            name="userName"
            placeholder="Ingresa tu usuario"
            autocomplete="username"
            maxlength="150" />
          <mat-icon matSuffix aria-hidden="true">person</mat-icon>

          <!-- Hints y errores -->
          <mat-hint>Tu ID o correo corporativo</mat-hint>
          <mat-hint align="end">
            {{ loginForm.controls['userName'].value?.length || 0 }}/150
          </mat-hint>

          <mat-error *ngIf="loginForm.controls['userName'].hasError('required')">
            El usuario es obligatorio.
          </mat-error>
          <mat-error *ngIf="loginForm.controls['userName'].hasError('maxlength')">
            Máximo 150 caracteres.
          </mat-error>
        </mat-form-field>

        <!-- CONTRASEÑA -->
        <mat-form-field appearance="outline" class="full-width inputPass">
          <mat-label>Contraseña</mat-label>
          <input
            matInput
            [type]="hidePassword ? 'password' : 'text'"
            formControlName="password"
            id="password"
            name="password"
            placeholder="Ingresa tu contraseña"
            autocomplete="current-password"
            minlength="6"
            maxlength="128" />
          <button
            class="iconPass"
            type="button"
            mat-icon-button
            matSuffix
            (click)="togglePasswordVisibility()"
            [attr.aria-label]="hidePassword ? 'Mostrar contraseña' : 'Ocultar contraseña'"
            [attr.aria-pressed]="!hidePassword">
            <mat-icon>{{ hidePassword ? 'visibility_off' : 'visibility' }}</mat-icon>
          </button>

          <!-- Hints y errores -->
          <mat-hint>Mínimo 6 caracteres</mat-hint>
          <mat-hint align="end">
            {{ loginForm.controls['password'].value?.length || 0 }}/128
          </mat-hint>

          <mat-error *ngIf="loginForm.controls['password'].hasError('required')">
            La contraseña es obligatoria.
          </mat-error>
          <mat-error *ngIf="loginForm.controls['password'].hasError('minlength')">
            Debe tener al menos 6 caracteres.
          </mat-error>
          <mat-error *ngIf="loginForm.controls['password'].hasError('maxlength')">
            Máximo 128 caracteres.
          </mat-error>
        </mat-form-field>

        <!-- BOTÓN -->
        <button
          mat-raised-button
          color="primary"
          type="submit"
          class="loginNow"
          [disabled]="isLoading || loginForm.invalid">
          Entrar
        </button>

        <!-- ERROR GENERAL DEL API BAJO EL BOTÓN -->
        <div class="inline-error mat-mdc-form-field-error" *ngIf="errorMessage" role="alert">
          <mat-icon aria-hidden="true">error</mat-icon>
          <span>{{ errorMessage }}</span>
        </div>
      </form>
    </mat-card-content>
  </mat-card>

  <!-- OVERLAY CARGANDO -->
  <div class="overlay" *ngIf="isLoading" role="alert" aria-live="assertive">
    <div class="overlay-content" role="dialog" aria-label="Cargando">
      <mat-progress-spinner mode="indeterminate" diameter="48"></mat-progress-spinner>
      <div class="overlay-text">Cargando…</div>
    </div>
  </div>
</div>
