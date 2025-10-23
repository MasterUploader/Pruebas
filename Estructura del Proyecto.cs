# Servicio de inactividad + expiración JWT
ng g service core/services/session-idle --skip-tests

# Diálogo de advertencia por inactividad (standalone)
ng g component shared/dialogs/idle-warning --standalone --skip-tests









// src/app/core/services/session-idle.service.ts
import { Injectable, NgZone, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { fromEvent, Subject, merge, timer, Subscription, firstValueFrom } from 'rxjs';
import { debounceTime, mapTo, startWith, switchMap, takeUntil } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { MatDialog } from '@angular/material/dialog';
import { IdleWarningComponent } from '../../shared/dialogs/idle-warning/idle-warning.component';

/**
 * Servicio FRONT para controlar INACTIVIDAD + expiración de JWT con AVISO modal.
 * - A los X ms de inactividad, muestra diálogo con cuenta regresiva (Y segundos).
 * - Si el usuario pulsa "Seguir conectado": reinicia temporizadores y hace KeepAlive().
 * - Si no hace nada, al cumplirse el timeout, se cierra sesión.
 * - Además, cada 30s valida el `exp` del JWT y cierra si expiró.
 *
 * IMPORTANTE: Esto NO limita sesiones simultáneas (eso va en backend).
 */
@Injectable({ providedIn: 'root' })
export class SessionIdleService implements OnDestroy {
  /** Tiempo máximo de INACTIVIDAD (ms). Ej.: 15 min */
  private readonly IDLE_TIMEOUT_MS = 15 * 60 * 1000;

  /** Tiempo de AVISO antes de cierre (ms). Ej.: 60s */
  private readonly WARNING_BEFORE_CLOSE_MS = 60 * 1000;

  /** Verificación periódica del JWT (ms). */
  private readonly JWT_CHECK_INTERVAL_MS = 30_000;

  private stop$ = new Subject<void>();
  private subs: Subscription[] = [];
  private watching = false;

  /** Emisor manual de "actividad" (p.ej., al pulsar 'Seguir conectado') */
  private manualActivity$ = new Subject<void>();

  /** Referencia al diálogo actual (si está abierto) */
  private dialogRef: { close: (result?: 'continue' | 'logout') => void } | null = null;

  constructor(
    private readonly ngZone: NgZone,
    private readonly router: Router,
    private readonly auth: AuthService,
    private readonly dialog: MatDialog
  ) {}

  /** Inicia monitoreo (idempotente) */
  startWatching(): void {
    this.stopWatching();
    this.watching = true;

    // 1) Stream de actividad del usuario + actividad manual
    const domActivity$ = merge(
      fromEvent(document, 'mousemove'),
      fromEvent(document, 'keydown'),
      fromEvent(document, 'click'),
      fromEvent(document, 'scroll'),
      fromEvent(window, 'focus')
    ).pipe(
      debounceTime(300),
      mapTo(Date.now()),
      startWith(Date.now())
    );

    const activity$ = merge(domActivity$, this.manualActivity$.pipe(mapTo(Date.now())));

    // 2) Timers de aviso + logout
    const subTimers = activity$
      .pipe(
        takeUntil(this.stop$),
        switchMap(() => {
          this.closeDialogSafe(); // Si hubo actividad, cierra aviso
          const warnIn = Math.max(0, this.IDLE_TIMEOUT_MS - this.WARNING_BEFORE_CLOSE_MS);
          const warn$ = timer(warnIn).pipe(mapTo<'warning' | 'logout'>('warning'));
          const logout$ = timer(this.IDLE_TIMEOUT_MS).pipe(mapTo<'warning' | 'logout'>('logout'));
          return merge(warn$, logout$);
        })
      )
      .subscribe(evt => {
        if (evt === 'warning') {
          const seconds = Math.floor(this.WARNING_BEFORE_CLOSE_MS / 1000);
          this.openWarningDialog(seconds);
        } else {
          this.forceLogout('Tu sesión expiró por inactividad.');
        }
      });
    this.subs.push(subTimers);

    // 3) Verificación periódica del JWT
    const subJwt = timer(0, this.JWT_CHECK_INTERVAL_MS).subscribe(() => {
      const exp = this.auth.getJwtExp();
      if (exp && exp * 1000 <= Date.now()) {
        this.forceLogout('Tu token ha expirado.');
      }
    });
    this.subs.push(subJwt);
  }

  /** Detiene monitoreo + cierra diálogo si existiera */
  stopWatching(): void {
    this.stop$.next();
    this.subs.forEach(s => s.unsubscribe());
    this.subs = [];
    this.watching = false;
    this.closeDialogSafe();
  }

  /** Permite consultar si el servicio está activo */
  isWatching(): boolean {
    return this.watching;
  }

  ngOnDestroy(): void {
    this.stopWatching();
    this.stop$.complete();
  }

  // ---------- Helpers privados ----------

  private openWarningDialog(seconds: number): void {
    this.closeDialogSafe(); // Evita duplicados

    const ref = this.dialog.open(IdleWarningComponent, {
      disableClose: true,
      width: '420px',
      data: { seconds } // <— se usa en el componente para la cuenta regresiva
    });

    this.dialogRef = ref;

    ref.afterClosed().subscribe(async (result) => {
      this.dialogRef = null;

      if (result === 'continue') {
        // 1) El usuario quiere seguir conectado => reinicia timers
        this.manualActivity$.next();

        // 2) KeepAlive en servidor: extiende sesión y/o rota token
        try {
          await firstValueFrom(this.auth.keepAlive());
        } catch {
          // Si falla y viene 401/403, el AuthService hará logout; aquí no repetimos UI.
        }
      } else if (result === 'logout') {
        this.forceLogout('Cerraste sesión manualmente.');
      }
      // Si undefined: cerró por agotamiento del contador del diálogo; el timer principal hará el logout real.
    });
  }

  private closeDialogSafe(): void {
    if (this.dialogRef) {
      try { this.dialogRef.close(); } catch {}
      this.dialogRef = null;
    }
  }

  private forceLogout(reason: string): void {
    this.stopWatching();
    this.auth.logout();
    this.router.navigate(['/login'], { queryParams: { r: 'expired' } });
  }
}





// src/app/shared/dialogs/idle-warning/idle-warning.component.ts
import { Component, Inject, OnDestroy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { CommonModule } from '@angular/common';

export interface IdleWarningData {
  seconds: number;
}

/**
 * Diálogo de advertencia por INACTIVIDAD (standalone).
 * Muestra cuenta regresiva. El usuario puede:
 *  - "Seguir conectado" => cierra con 'continue'
 *  - "Cerrar sesión"   => cierra con 'logout'
 */
@Component({
  selector: 'app-idle-warning',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title class="title">¿Sigues ahí?</h2>
    <div mat-dialog-content class="content">
      <p>Tu sesión expirará por inactividad en <strong>{{remaining}}</strong> segundos.</p>
      <p>Pulsa <strong>Seguir conectado</strong> para continuar.</p>
    </div>
    <div mat-dialog-actions class="actions">
      <button mat-stroked-button (click)="onLogout()">Cerrar sesión</button>
      <button mat-flat-button color="primary" (click)="onContinue()">Seguir conectado</button>
    </div>
  `,
  styles: [`
    .title { margin:0; }
    .content { font-size: 14px; line-height: 1.4; }
    .actions { display:flex; gap:.75rem; justify-content: flex-end; }
    strong { font-weight: 600; }
  `]
})
export class IdleWarningComponent implements OnDestroy {
  remaining: number;
  private timerId: any;

  constructor(
    @Inject(MAT_DIALOG_DATA) public data: IdleWarningData,
    private ref: MatDialogRef<IdleWarningComponent, 'continue' | 'logout' | undefined>
  ) {
    this.remaining = Math.max(1, Math.floor(data.seconds));
    this.startCountdown();
  }

  private startCountdown(): void {
    this.stopCountdown();
    this.timerId = setInterval(() => {
      this.remaining -= 1;
      if (this.remaining <= 0) {
        this.ref.close(); // cierre silencioso; el servicio hará el logout real
      }
    }, 1000);
  }

  private stopCountdown(): void {
    if (this.timerId) {
      clearInterval(this.timerId);
      this.timerId = null;
    }
  }

  onContinue(): void {
    this.ref.close('continue');
  }

  onLogout(): void {
    this.ref.close('logout');
  }

  ngOnDestroy(): void {
    this.stopCountdown();
  }
}

















