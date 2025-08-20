import {
  Component, OnInit, ChangeDetectorRef, ViewChild, OnDestroy,
  ChangeDetectionStrategy, AfterViewInit
} from '@angular/core';
import { CommonModule } from '@angular/common';

// Angular Material
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';

// Forms
import { ReactiveFormsModule, FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';

// RxJS
import { Subscription, finalize, firstValueFrom } from 'rxjs';
import { take } from 'rxjs/operators';

// Servicios / modelos
import { TarjetaService } from '../../../../core/services/tarjeta.service';
import { Tarjeta } from '../../../../core/models/tarjeta.model';
import { GetDetalleTarjetasImprimirResponseDto } from '../../../../core/models/getDetalleTarjetasImprimir.model';
import { ModalTarjetaComponent } from '../modal-tarjeta/modal-tarjeta.component';
import { AuthService } from '../../../../core/services/auth.service';

// Pipes standalone
import { MaskCardNumberPipe } from '../../../../shared/pipes/mask-card-number.pipe';
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';

@Component({
  selector: 'app-consulta-tarjeta',
  imports: [
    CommonModule, MatCardModule, MatDialogModule, MatTableModule,
    MatFormFieldModule, ReactiveFormsModule, MatInputModule,
    MatIconModule, MatSortModule, MatMenuModule, MatButtonModule,
    MatProgressSpinnerModule, MatSnackBarModule, MatPaginatorModule,
    MaskCardNumberPipe, MaskAccountNumberPipe
  ],
  templateUrl: './consulta-tarjeta.component.html',
  styleUrl: './consulta-tarjeta.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConsultaTarjetaComponent implements OnInit, AfterViewInit, OnDestroy {

  // ===== Configuración =====
  private readonly BIN = '411052';
  private readonly MAX_DIGITS = 3;
  private readonly ONLY_DIGITS_RE = /^\d+$/;

  // ===== Table / Sort / Paginator =====
  @ViewChild(MatSort, { static: true }) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator; // 👈 nuevo: paginador
  public readonly dataSource = new MatTableDataSource<Tarjeta>([]);
  public readonly displayedColumns: ReadonlyArray<string> =
    ['numero', 'nombre', 'motivo', 'numeroCuenta', 'eliminar'];

  // ===== Form =====
  public formularioAgencias!: FormGroup<{
    codigoAgenciaImprime: FormControl<string>;
    codigoAgenciaApertura: FormControl<string>;
  }>;

  // ===== Estado UI =====
  public activateFilter = '';
  public usuarioICBS = '';
  public tarjetaSeleccionada: Tarjeta = {
    nombre: '', numero: '', fechaEmision: '', fechaVencimiento: '',
    motivo: '', numeroCuenta: ''
  };
  /** Banner bajo el formulario cuando no hay data. */
  public noDataMessage: string | null = null;
  /** Overlay de “Cargando…”. */
  public isLoading = false;

  public getDetalleTarjetasImprimirResponseDto!: GetDetalleTarjetasImprimirResponseDto | null;

  private readonly subscription = new Subscription();

  constructor(
    private readonly authService: AuthService,
    private readonly datosTarjetaServices: TarjetaService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef,
    private readonly fb: FormBuilder,
    private readonly snackBar: MatSnackBar
  ) { this.configurarFiltros(); }

  // ===== Ciclo de vida =====
  ngOnInit(): void {
    // Form con validación: requerido + solo dígitos + máx 3
    this.formularioAgencias = this.fb.group({
      codigoAgenciaImprime: this.fb.control('', {
        nonNullable: true,
        validators: [Validators.required, Validators.pattern(this.ONLY_DIGITS_RE), Validators.maxLength(this.MAX_DIGITS)]
      }),
      codigoAgenciaApertura: this.fb.control('', {
        nonNullable: true,
        validators: [Validators.required, Validators.pattern(this.ONLY_DIGITS_RE), Validators.maxLength(this.MAX_DIGITS)]
      })
    });

    // Carga inicial (con spinner)
    this.withActiveSession(() => {
      const ad = this.authService.currentUserValue?.activeDirectoryData;
      this.usuarioICBS = ad?.usuarioICBS ?? '';

      const codigoAgenciaImprime = ad?.agenciaImprimeCodigo ?? '';
      const codigoAgenciaApertura = ad?.agenciaAperturaCodigo ?? '';

      this.formularioAgencias.patchValue({ codigoAgenciaImprime, codigoAgenciaApertura });
      this.noDataMessage = null;
      this.consultarMicroservicio(codigoAgenciaImprime, codigoAgenciaApertura, true);
      this.setSort();
    });
  }

  // 👇 nuevo: conecta el paginador después de renderizar la vista
  ngAfterViewInit(): void {
    this.dataSource.paginator = this.paginator;
    this.cdr.markForCheck();
  }

  ngOnDestroy(): void { this.subscription.unsubscribe(); }

  // ===== Infra =====
  private withActiveSession(action: () => void): void {
    this.subscription.add(
      this.authService.sessionActive$.pipe(take(1)).subscribe(isActive => {
        if (!isActive) { this.authService.logout(); return; }
        action();
      })
    );
  }

  private setSort(): void { this.dataSource.sort = this.sort; }

  private setAgenciasFromResponse(resp: GetDetalleTarjetasImprimirResponseDto): void {
    this.getDetalleTarjetasImprimirResponseDto = resp;
    this.formularioAgencias.patchValue({
      codigoAgenciaApertura: resp.agencia.agenciaAperturaCodigo,
      codigoAgenciaImprime: resp.agencia.agenciaImprimeCodigo
    });
    this.cdr.markForCheck();
  }

  private setLoading(flag: boolean): void { this.isLoading = flag; this.cdr.markForCheck(); }

  /** Snackbar genérico (info/error). */
  private showSnack(message: string): void {
    this.snackBar.dismiss();
    this.snackBar.open(message, 'Cerrar', {
      duration: 4000, verticalPosition: 'top', horizontalPosition: 'center'
    });
  }

  /** Snackbar de éxito (visual igual; separo método por claridad). */
  private showSnackOk(message: string): void { this.showSnack(message); }

  // ===== Filtro tabla =====
  private configurarFiltros(): void {
    this.dataSource.filterPredicate = (data: Tarjeta, raw: string): boolean => {
      if (!raw) return true;
      const [field, ...rest] = raw.split(':');
      const term = rest.join(':').toLowerCase().trim();
      if (!field || !term) return true;

      const map: Record<string, string | undefined> = {
        numero: data.numero, nombre: data.nombre, motivo: data.motivo, numeroCuenta: data.numeroCuenta
      };
      return (map[field] ?? '').toString().toLowerCase().includes(term);
    };
  }

  public applyFilterFromInput(evt: Event, field: 'numero' | 'nombre' | 'motivo' | 'numeroCuenta'): void {
    const value = (evt.target as HTMLInputElement)?.value ?? '';
    this.activateFilter = `${field}:${value.trim().toLowerCase()}`;
    this.dataSource.filter = this.activateFilter;

    // 👇 al filtrar, volvemos a la primera página
    if (this.paginator) this.paginator.firstPage();
  }

  // ===== Consultas =====
  private consultarMicroservicio(codigoAgenciaImprime: string, codigoAgenciaApertura: string, showLoading = true): void {
    if (showLoading) this.setLoading(true);

    this.datosTarjetaServices
      .obtenerDatosTarjeta(this.BIN, codigoAgenciaImprime, codigoAgenciaApertura)
      .pipe(take(1), finalize(() => { if (showLoading) this.setLoading(false); }))
      .subscribe({
        next: (response) => {
          this.dataSource.data = response.tarjetas;

          // 👇 al recargar datos, volvemos a la primera página
          if (this.paginator) this.paginator.firstPage();

          const sinTarjetas = !response?.tarjetas?.length;
          this.noDataMessage = sinTarjetas
            ? `No hay datos para la combinación de Agencia de impresión ${codigoAgenciaImprime} y Agencia de apertura ${codigoAgenciaApertura}.`
            : null;
          if (sinTarjetas) this.showSnack(this.noDataMessage!);

          this.setAgenciasFromResponse(response);
          this.cdr.markForCheck();
        },
        error: () => {
          this.showSnack('Ocurrió un error al consultar los datos. Intenta nuevamente.');
        }
      });
  }

  public actualizarTabla(): void {
    if (this.formularioAgencias.invalid) {
      this.formularioAgencias.markAllAsTouched();
      return;
    }
    this.withActiveSession(() => {
      const agenciaImprimeCodigo = this.formularioAgencias.get('codigoAgenciaImprime')?.value ?? '';
      const agenciaAperturaCodigo = this.formularioAgencias.get('codigoAgenciaApertura')?.value ?? '';
      this.noDataMessage = null;
      this.consultarMicroservicio(agenciaImprimeCodigo, agenciaAperturaCodigo, true);
    });
  }

  public recargarDatos(): void { this.actualizarTabla(); }

  // ===== Modal / nombre en vivo =====
  public abrirModal(row: Tarjeta): void {
    this.tarjetaSeleccionada = row;

    const ref = this.dialog.open(ModalTarjetaComponent, {
      data: row, width: '720px', disableClose: true
    });

    // Reflejar nombre en grilla mientras se escribe en el modal (solo UI)
    const cmp = ref.componentInstance;
    if (cmp?.nombreCambiado) {
      const subNombre = cmp.nombreCambiado.subscribe((nuevoNombre: string) =>
        this.updateRowName(row.numero, nuevoNombre)
      );
      const closeSub = ref.afterClosed().pipe(take(1)).subscribe(() => subNombre.unsubscribe());
      this.subscription.add(subNombre);
      this.subscription.add(closeSub);
    }

    this.subscription.add(ref.afterClosed().pipe(take(1)).subscribe());
  }

  /** Accesibilidad: Enter/Espacio abren el modal desde la fila. */
  public onRowKeyOpen(e: Event, row: Tarjeta): void {
    e.preventDefault(); // evita scroll con Space
    this.abrirModal(row);
  }

  private updateRowName(numero: string, nuevoNombre: string): void {
    const nueva = this.dataSource.data.map(r =>
      r.numero === numero ? { ...r, nombre: (nuevoNombre ?? '').toUpperCase() } : r
    );
    this.dataSource.data = nueva;
    this.cdr.markForCheck();
  }

  // ===== Validación visual de form =====
  public hasFormControlError(
    controlName: keyof ConsultaTarjetaComponent['formularioAgencias']['controls'],
    errorName: string
  ): boolean {
    const control = this.formularioAgencias.get(controlName as string);
    return !!control && control.touched && control.hasError(errorName);
  }

  // --- teclas de navegación/edición que permitimos sin escribir ---
  private static readonly ALLOWED_KEYS = new Set<string>([
    'Backspace', 'Delete', 'ArrowLeft', 'ArrowRight', 'Tab', 'Home', 'End'
  ]);

  // --- atajos con Ctrl/Meta permitidos ---
  private static readonly CTRL_COMBOS = new Set<string>([
    'a', 'c', 'v', 'x', 'z', 'y'
  ]);

  /** Devuelve true si la tecla es de control/navegación o un atajo Ctrl/Meta permitido */
  private isControlKey(e: KeyboardEvent): boolean {
    const k = e.key;
    const isCtrl = e.ctrlKey || e.metaKey;
    return (
      ConsultaTarjetaComponent.ALLOWED_KEYS.has(k) ||
      (isCtrl && ConsultaTarjetaComponent.CTRL_COMBOS.has(k.toLowerCase()))
    );
  }

  public onKeyDownDigits(e: KeyboardEvent, controlName: 'codigoAgenciaImprime' | 'codigoAgenciaApertura'): void {
    if (this.isControlKey(e)) return;
    const input = e.target as HTMLInputElement;
    const key = e.key;
    if (!/^\d$/.test(key)) { e.preventDefault(); return; }

    const selStart = input.selectionStart ?? input.value.length;
    const selEnd = input.selectionEnd ?? input.value.length;
    const selected = Math.max(0, selEnd - selStart);
    const currentLen = (input.value ?? '').length;
    const resultingLen = currentLen - selected + 1;
    if (resultingLen > this.MAX_DIGITS) e.preventDefault();
  }

  public onPasteDigits(e: ClipboardEvent, controlName: 'codigoAgenciaImprime' | 'codigoAgenciaApertura'): void {
    e.preventDefault();
    const input = e.target as HTMLInputElement;
    const clip = e.clipboardData?.getData('text') ?? '';
    const digits = clip.replace(/\D+/g, '');
    const selStart = input.selectionStart ?? input.value.length;
    const selEnd = input.selectionEnd ?? input.value.length;
    const selected = Math.max(0, selEnd - selStart);
    const current = input.value ?? '';
    const remaining = this.MAX_DIGITS - (current.length - selected);
    if (remaining <= 0) return;

    const toInsert = digits.slice(0, Math.max(0, remaining));
    const newValue = current.slice(0, selStart) + toInsert + current.slice(selEnd);
    this.formularioAgencias.get(controlName)?.setValue(newValue, { emitEvent: true });
    this.cdr.markForCheck();
  }

  public onInputSanitize(controlName: 'codigoAgenciaImprime' | 'codigoAgenciaApertura'): void {
    const ctrl = this.formularioAgencias.get(controlName);
    const raw = String(ctrl?.value ?? '');
    const clean = raw.replace(/\D+/g, '').slice(0, this.MAX_DIGITS);
    if (clean !== raw) { ctrl?.setValue(clean, { emitEvent: false }); this.cdr.markForCheck(); }
  }

  // ===== Eliminar (marcar impresa) =====
  /** Obtiene el nombre a registrar (seleccionado > encontrado) en MAYÚSCULAS */
  private getNombreParaRegistrar(numeroTarjeta: string): string {
    const seleccionado = this.tarjetaSeleccionada?.nombre ?? null;
    const encontrado = this.dataSource.data.find(x => x.numero === numeroTarjeta)?.nombre ?? null;
    const nombre = seleccionado ?? encontrado ?? '';
    return nombre.toUpperCase();
  }

  /** Quita la fila de la UI y marca change detection */
  private removeFromUi(numeroTarjeta: string): void {
    this.dataSource.data = this.dataSource.data.filter(item => item.numero !== numeroTarjeta);
    this.cdr.markForCheck();
  }

  /**
   * Marca una tarjeta como impresa:
   * - NO elimina la fila hasta que el backend confirme
   * - Muestra snackbar de éxito/fracaso
   */
  public eliminarTarjeta(event: Event, numeroTarjeta: string): void {
    event.stopPropagation();
    if (!numeroTarjeta) return;

    const nombreParaRegistrar = this.getNombreParaRegistrar(numeroTarjeta);

    this.withActiveSession(async () => {
      (async () => {
        try {
          await firstValueFrom(
            this.datosTarjetaServices
              .guardaEstadoImpresion(numeroTarjeta, this.usuarioICBS, nombreParaRegistrar)
              .pipe(take(1))
          );

          // ✅ Sólo aquí removemos de la UI
          this.removeFromUi(numeroTarjeta);
          this.showSnackOk('Tarjeta marcada como impresa.');
        } catch {
          this.showSnack('No se pudo registrar la impresión. Intenta de nuevo.');
        }
      })();
    });
  }
}
