import { Injectable, NgZone, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { fromEvent, Subject, merge, timer, Subscription } from 'rxjs';
import { debounceTime, map, startWith, switchMap, takeUntil } from 'rxjs/operators'; // 👈 usamos map, no mapTo
import { AuthService } from './auth.service';
import { MatDialog } from '@angular/material/dialog';
import { IdleWarningComponent } from '../../shared/dialogs/idle-warning/idle-warning.component';

@Injectable({ providedIn: 'root' })
export class SessionIdleService implements OnDestroy {
  private IDLE_TIMEOUT_MS = 15 * 60 * 1000;    // respaldo
  private WARNING_BEFORE_CLOSE_MS = 60 * 1000; // respaldo

  private stop$ = new Subject<void>();
  private subs: Subscription[] = [];
  private watching = false;

  /** Emisor manual de actividad (p.ej., “Seguir conectado”) */
  private manualActivity$ = new Subject<void>();

  /** Ref al diálogo para evitar duplicados */
  private dialogRef: { close: (result?: 'continue' | 'logout') => void } | null = null;

  constructor(
    private readonly ngZone: NgZone,
    private readonly router: Router,
    private readonly auth: AuthService,
    private readonly dialog: MatDialog
  ) {}

  /** Inicia monitoreo (idempotente) */
  async startWatching(): Promise<void> {
    this.stopWatching();
    this.watching = true;

    // 1) Lee minutos desde el API y ajusta tiempos
    const minutes = await this.auth.getSessionDurationFromApi();
    this.IDLE_TIMEOUT_MS = minutes * 60 * 1000;
    this.WARNING_BEFORE_CLOSE_MS = Math.min(60 * 1000, this.IDLE_TIMEOUT_MS / 4);

    // 2) Streams de actividad (DOM + manual)
    const domActivity$ = merge(
      fromEvent(document, 'mousemove'),
      fromEvent(document, 'keydown'),
      fromEvent(document, 'click'),
      fromEvent(document, 'scroll'),
      fromEvent(window, 'focus')
    ).pipe(
      debounceTime(300),
      map(() => Date.now()),          // 👈 antes: mapTo(Date.now())
      startWith(Date.now())
    );

    const manual$ = this.manualActivity$.pipe(
      map(() => Date.now())           // 👈 antes: mapTo(Date.now())
    );

    // 3) Aviso y cierre por inactividad
    this.ngZone.runOutsideAngular(() => {
      const sub = merge(domActivity$, manual$)
        .pipe(
          switchMap(() => {
            const warningAt = this.IDLE_TIMEOUT_MS - this.WARNING_BEFORE_CLOSE_MS;
            return merge(
              timer(warningAt).pipe(map(() => 'warning' as const)), // 👈 antes: mapTo('warning')
              timer(this.IDLE_TIMEOUT_MS).pipe(map(() => 'logout' as const)) // 👈 antes: mapTo('logout')
            );
          }),
          takeUntil(this.stop$)
        )
        .subscribe(evt => {
          if (evt === 'warning') {
            this.ngZone.run(() => this.openWarningDialog());
          } else {
            this.ngZone.run(() => this.forceLogout('Tu sesión expiró por inactividad.'));
          }
        });

      this.subs.push(sub);
    });
  }

  /** Detiene monitoreo + cierra diálogo si existiera */
  stopWatching(): void {
    this.stop$.next();
    this.subs.forEach(s => s.unsubscribe());
    this.subs = [];
    this.watching = false;
    this.closeDialogSafe();
  }

  isWatching(): boolean { return this.watching; }

  ngOnDestroy(): void {
    this.stopWatching();
    this.stop$.complete();
  }

  // ---------- Privados ----------

  private openWarningDialog(): void {
    this.closeDialogSafe();
    const seconds = Math.floor(this.WARNING_BEFORE_CLOSE_MS / 1000);

    const ref = this.dialog.open(IdleWarningComponent, {
      width: '420px',
      disableClose: true,
      data: { seconds }
    });
    this.dialogRef = { close: (r?: 'continue' | 'logout') => ref.close(r) };

    const sub = ref.afterClosed().subscribe(result => {
      if (result === 'continue') {
        this.manualActivity$.next(); // reinicia timers
        this.auth.keepAlive().subscribe({ error: () => this.forceLogout('Sesión inválida en servidor.') });
      } else if (result === 'logout') {
        this.forceLogout('Cierre de sesión solicitado.');
      }
    });
    this.subs.push(sub);
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
    this.router.navigate(['/login'], { queryParams: { r: 'expired', msg: reason } });
  }
}
