using Microsoft.AspNetCore.Mvc;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Controllers
{
    [ApiController]
    [Route("api/config")]
    public class ConfigController : ControllerBase
    {
        private readonly IConfiguration _config;
        public ConfigController(IConfiguration config) => _config = config;

        /// <summary>Devuelve la duración de sesión (minutos) desde appsettings.</summary>
        [HttpGet("session")]
        [AllowAnonymous]
        public IActionResult GetSessionConfig()
        {
            int minutes = _config.GetValue<int>("JwtSettings:SessionMinutes", 15);
            return Ok(new { sessionMinutes = minutes });
        }
    }
}




var now = DateTime.UtcNow;
int sessionMinutes = 15;
try
{
    // lee desde appsettings
    sessionMinutes = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddEnvironmentVariables()
        .Build()
        .GetValue<int>("JwtSettings:SessionMinutes", 15);
}
catch { /* fallback 15 */ }

var expires = now.AddMinutes(sessionMinutes); // ✅ dinámico





var newExp = DateTime.UtcNow.AddMinutes(15);


int minutes = HttpContext.RequestServices
    .GetRequiredService<IConfiguration>()
    .GetValue<int>("JwtSettings:SessionMinutes", 15);

var newExp = DateTime.UtcNow.AddMinutes(minutes);




/** Obtiene desde el backend la duración de la sesión (minutos). */
public async getSessionDurationFromApi(): Promise<number> {
  try {
    const url = `${this.apiUrl}/api/config/session`;
    const resp = await firstValueFrom(this.http.get<{ sessionMinutes: number }>(url));
    return resp?.sessionMinutes ?? 15;
  } catch {
    return 15;
  }
}




import { Injectable, NgZone, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { fromEvent, Subject, merge, timer, Subscription } from 'rxjs';
import { debounceTime, mapTo, startWith, switchMap, takeUntil } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { MatDialog } from '@angular/material/dialog';
import { IdleWarningComponent } from '../../shared/dialogs/idle-warning/idle-warning.component';

@Injectable({ providedIn: 'root' })
export class SessionIdleService implements OnDestroy {
  /** Config dinámica obtenida desde API */
  private IDLE_TIMEOUT_MS = 15 * 60 * 1000;           // respaldo
  private WARNING_BEFORE_CLOSE_MS = 60 * 1000;        // aviso por defecto (1 min)

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
  async startWatching(): Promise<void> {
    this.stopWatching();
    this.watching = true;

    // 1) 🔄 Traer minutos desde API y ajustar tiempos
    const minutes = await this.auth.getSessionDurationFromApi();
    this.IDLE_TIMEOUT_MS = minutes * 60 * 1000;
    this.WARNING_BEFORE_CLOSE_MS = Math.min(60 * 1000, this.IDLE_TIMEOUT_MS / 4); // aviso en el último cuarto (máx 60s)

    // 2) Streams de actividad (DOM + manual)
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

    const manual$ = this.manualActivity$.pipe(mapTo(Date.now()));

    // 3) Armar lógica de avisos y cierre
    this.ngZone.runOutsideAngular(() => {
      const sub = merge(domActivity$, manual$)
        .pipe(
          switchMap(() => {
            const warningAt = this.IDLE_TIMEOUT_MS - this.WARNING_BEFORE_CLOSE_MS;
            return merge(
              timer(warningAt).pipe(mapTo<'warning'>('warning')),
              timer(this.IDLE_TIMEOUT_MS).pipe(mapTo<'logout'>('logout'))
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

  // ---------- Helpers privados ----------

  private openWarningDialog(): void {
    this.closeDialogSafe(); // Evita duplicados
    const seconds = Math.floor(this.WARNING_BEFORE_CLOSE_MS / 1000);

    const ref = this.dialog.open(IdleWarningComponent, {
      width: '420px',
      disableClose: true,
      data: { seconds }
    });
    this.dialogRef = { close: (r?: 'continue' | 'logout') => ref.close(r) };

    const sub = ref.afterClosed().subscribe(result => {
      if (result === 'continue') {
        // usuario desea seguir: registrar actividad y opcionalmente llamar keepAlive()
        this.manualActivity$.next();
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




