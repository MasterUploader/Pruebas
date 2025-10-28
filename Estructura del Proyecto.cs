private openWarningDialog(): void {
  this.closeDialogSafe(); // evita duplicados
  const seconds = Math.floor(this.WARNING_BEFORE_CLOSE_MS / 1000);

  const ref = this.dialog.open(IdleWarningComponent, {
    width: '420px',
    disableClose: true,
    data: { seconds }
  });
  this.dialogRef = { close: (r?: 'continue' | 'logout') => ref.close(r) };

  // ⬇️ Suscripción al resultado del diálogo
  const sub = ref.afterClosed().subscribe(result => {
    if (result === 'continue') {
      // 1) cerrar YA cualquier posible temporizador del diálogo
      this.closeDialogSafe();

      // 2) reiniciar los timers del servicio de inactividad inmediatamente
      this.manualActivity$.next();          // resetea el switchMap
      this.stop$.next();                    // cancela timers actuales
      this.subs.forEach(s => s.unsubscribe());
      this.subs = [];
      this.initWatchPipelines();            // (si separaste init en un método) o vuelve a llamar startWatching()

      // 3) opcional: keepAlive para extender en backend
      this.auth.keepAlive().subscribe({ error: () => this.forceLogout('Sesión inválida en servidor.') });
      return;
    }
    if (result === 'logout') {
      this.forceLogout('Cierre de sesión solicitado.');
    }
  });
  this.subs.push(sub);
}

/** Extrae la construcción de pipelines para poder relanzarla al reset */
private initWatchPipelines(): void {
  const domActivity$ = merge(
    fromEvent(document, 'mousemove'),
    fromEvent(document, 'keydown'),
    fromEvent(document, 'click'),
    fromEvent(document, 'scroll'),
    fromEvent(window, 'focus')
  ).pipe(debounceTime(300), map(() => Date.now()), startWith(Date.now()));

  const manual$ = this.manualActivity$.pipe(map(() => Date.now()));

  this.ngZone.runOutsideAngular(() => {
    const sub = merge(domActivity$, manual$)
      .pipe(
        switchMap(() => {
          const warningAt = this.IDLE_TIMEOUT_MS - this.WARNING_BEFORE_CLOSE_MS;
          return merge(
            timer(warningAt).pipe(map(() => 'warning' as const)),
            timer(this.IDLE_TIMEOUT_MS).pipe(map(() => 'logout' as const))
          );
        }),
        takeUntil(this.stop$)
      )
      .subscribe(evt => {
        if (evt === 'warning') this.ngZone.run(() => this.openWarningDialog());
        else this.ngZone.run(() => this.forceLogout('Tu sesión expiró por inactividad.'));
      });

    this.subs.push(sub);
  });
}




export class IdleWarningComponent implements OnInit, OnDestroy {
  seconds = 30;
  private sub?: Subscription;

  constructor(private dialogRef: MatDialogRef<IdleWarningComponent>,
              @Inject(MAT_DIALOG_DATA) public data: { seconds: number }) {
    this.seconds = data.seconds ?? 30;
  }

  ngOnInit(): void {
    this.sub = interval(1000).subscribe(() => {
      this.seconds--;
      if (this.seconds <= 0) {
        this.onLogout(); // dispara cierre por timeout del propio diálogo
      }
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe(); // ✨ evita que el contador “siga corriendo”
  }

  onContinue(): void {
    this.dialogRef.close('continue'); // ✨ cierra con el resultado correcto
  }

  onLogout(): void {
    this.dialogRef.close('logout');
  }
}

<button mat-raised-button color="primary" (click)="onContinue()">Seguir conectado</button>
<button mat-stroked-button color="warn" (click)="onLogout()">Cerrar sesión</button>
<p>Tu sesión expirará en {{seconds}} s.</p>








