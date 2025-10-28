import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Subject, interval } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

/**
 * Diálogo de advertencia por inactividad.
 * - Muestra una cuenta regresiva (segundos).
 * - "Seguir conectado" cierra con 'continue' para que el servicio reinicie timers y haga keepAlive.
 * - "Cerrar sesión" cierra con 'logout' para que el servicio fuerce el cierre.
 * - A11y: roles y labels para lectores de pantalla.
 *
 * Cómo abrirlo (ejemplo desde un servicio):
 *   const ref = this.dialog.open(IdleWarningComponent, { data: { seconds: 30 }, disableClose: true });
 *   ref.afterClosed().subscribe(result => { if(result==='continue'){...} else if(result==='logout'){...} });
 */
@Component({
  selector: 'app-idle-warning',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule
  ],
  templateUrl: './idle-warning.component.html',
  styleUrl: './idle-warning.component.css'
})
export class IdleWarningComponent implements OnInit, OnDestroy {
  /** Segundos restantes (se inicializa desde data.seconds) */
  seconds = 30;

  /** Valor total inicial para calcular el % de progreso. */
  private initialSeconds = 30;

  /** Para cancelar el interval cuando se destruye el diálogo. */
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly dialogRef: MatDialogRef<IdleWarningComponent, 'continue' | 'logout'>,
    @Inject(MAT_DIALOG_DATA) public data: { seconds?: number; message?: string }
  ) {
    // Permite personalizar los segundos desde el que lo abre
    if (typeof data?.seconds === 'number' && data.seconds > 0) {
      this.seconds = Math.floor(data.seconds);
      this.initialSeconds = this.seconds;
    }
  }

  ngOnInit(): void {
    // Tick cada 1s: decrementa y auto-cierra con 'logout' cuando llega a 0
    interval(1000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.seconds = Math.max(0, this.seconds - 1);
        if (this.seconds === 0) {
          this.onLogout(); // cierra el diálogo informando "logout"
        }
      });
  }

  ngOnDestroy(): void {
    // Limpieza del intervalo y observables
    this.destroy$.next();
    this.destroy$.complete();
  }

  /** Progreso (0..100) para la barra; 100% al arrancar, 0% al expirar. */
  get progressPercent(): number {
    if (this.initialSeconds <= 0) return 0;
    return Math.round((this.seconds / this.initialSeconds) * 100);
  }

  /** Mensaje principal (customizable por quien abre el diálogo) */
  get title(): string {
    return '¿Sigues ahí?';
  }

  /** Subtítulo descriptivo */
  get subtitle(): string {
    return this.data?.message ?? 'Por seguridad, cerraremos tu sesión si no respondes.';
  }

  /** El usuario confirma que desea continuar activo. */
  onContinue(): void {
    this.dialogRef.close('continue');
  }

  /** El usuario decide cerrar sesión (o llegó a 0 la cuenta). */
  onLogout(): void {
    this.dialogRef.close('logout');
  }
}



<div class="idle-wrapper" role="dialog" aria-labelledby="idle-title" aria-describedby="idle-desc">
  <div class="icon-row">
    <mat-icon aria-hidden="true">hourglass_empty</mat-icon>
  </div>

  <h2 id="idle-title" class="title">{{ title }}</h2>
  <p id="idle-desc" class="subtitle">{{ subtitle }}</p>

  <div class="countdown" aria-live="polite">
    Tu sesión expirará en <strong>{{ seconds }}</strong> segundos.
  </div>

  <mat-progress-bar
    [value]="progressPercent"
    mode="determinate"
    aria-label="Tiempo restante de sesión">
  </mat-progress-bar>

  <div class="actions">
    <button
      mat-raised-button
      color="primary"
      class="btn-continue"
      (click)="onContinue()"
      cdkFocusInitial
      aria-label="Seguir conectado">
      <mat-icon aria-hidden="true">check_circle</mat-icon>
      Seguir conectado
    </button>

    <button
      mat-stroked-button
      color="warn"
      class="btn-logout"
      (click)="onLogout()"
      aria-label="Cerrar sesión ahora">
      <mat-icon aria-hidden="true">logout</mat-icon>
      Cerrar sesión
    </button>
  </div>
</div>


.idle-wrapper {
  display: flex;
  flex-direction: column;
  gap: 12px;
  width: 100%;
  max-width: 460px;
  padding: 8px 4px;
}

.icon-row {
  display: flex;
  justify-content: center;
  margin-top: 4px;
}

.icon-row mat-icon {
  font-size: 40px;
  width: 40px;
  height: 40px;
  opacity: 0.85;
}

.title {
  margin: 8px 0 4px;
  text-align: center;
  font-weight: 600;
}

.subtitle {
  margin: 0 0 6px;






