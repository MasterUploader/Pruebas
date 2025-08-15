/**
 * ConsultaTarjetaComponent (OnPush)
 * -----------------------------------------------------------------------------
 * NUEVO: snackbar de ÉXITO cuando se marca la tarjeta como impresa (icono eliminar).
 * Se conservan: overlay de carga, snackbar "sin datos", validación 3 dígitos,
 * accesibilidad, filtro, actualización en vivo del nombre desde el modal.
 * -----------------------------------------------------------------------------
 */

import {
  Component, OnInit, ChangeDetectorRef, ViewChild, OnDestroy,
  ChangeDetectionStrategy
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

// Forms
import { ReactiveFormsModule, FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';

// RxJS
import { Subscription, take, finalize } from 'rxjs';

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
    MatProgressSpinnerModule, MatSnackBarModule,
    MaskCardNumberPipe, MaskAccountNumberPipe
  ],
  templateUrl: './consulta-tarjeta.component.html',
  styleUrl: './consulta-tarjeta.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConsultaTarjetaComponent implements OnInit, OnDestroy {

  // ===== Configuración =====
  private readonly BIN = '411052';
  private readonly MAX_DIGITS = 3;
  private readonly ONLY_DIGITS_RE = /^\d+$/;

  // ===== Table / Sort =====
  @ViewChild(MatSort, { static: true }) sort!: MatSort;
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

  public getDetalleTarjetasImprimirResponseDto!: GetDetalleTarjetasImprimirResponseDto;

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

      const codigoAgenciaImprime  = ad?.agenciaImprimeCodigo ?? '';
      const codigoAgenciaApertura = ad?.agenciaAperturaCodigo ?? '';

      this.formularioAgencias.patchValue({ codigoAgenciaImprime, codigoAgenciaApertura });
      this.noDataMessage = null;
      this.consultarMicroservicio(codigoAgenciaImprime, codigoAgenciaApertura, true);
      this.setSort();
    });
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
  private showSnackOk(message: string): void {
    this.showSnack(message);
  }

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

          const sinTarjetas = !response?.tarjetas?.length;
          this.noDataMessage = sinTarjetas
            ? `No hay datos para esa Agencia de apertura ${codigoAgenciaApertura} y Agencia de impresión ${codigoAgenciaImprime}.`
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
      const agenciaImprimeCodigo  = this.formularioAgencias.get('codigoAgenciaImprime')?.value ?? '';
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

  // ===== Solo 3 dígitos: handlers =====
  private isControlKey(e: KeyboardEvent): boolean {
    const k = e.key; const ctrl = e.ctrlKey || e.metaKey;
    return (k === 'Backspace' || k === 'Delete' || k === 'ArrowLeft' || k === 'ArrowRight' ||
            k === 'Tab' || k === 'Home' || k === 'End' || (ctrl && ['a','c','v','x'].includes(k.toLowerCase())));
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
  public eliminarTarjeta(event: Event, numeroTarjeta: string): void {
    event.stopPropagation();

    // Calcula el nombre una sola vez
    const seleccionado = this.tarjetaSeleccionada.nombre;
    const encontrado = this.dataSource.data.find(x => x.numero === numeroTarjeta)?.nombre;
    const nombreParaRegistrar = (seleccionado || encontrado || '').toUpperCase();

    this.withActiveSession(() => {
      // Quita la fila de la UI
      this.dataSource.data = this.dataSource.data.filter(item => item.numero !== numeroTarjeta);
      this.cdr.markForCheck();

      // Registrar como impresa en backend
      this.datosTarjetaServices
        .guardaEstadoImpresion(numeroTarjeta, this.usuarioICBS, nombreParaRegistrar)
        .pipe(take(1))
        .subscribe({
          // ✅ Snackbar de ÉXITO
          next: () => this.showSnackOk('Tarjeta marcada como impresa.'),
          // (mantenemos el snack de error genérico si falla)
          error: () => this.showSnack('No se pudo registrar la impresión. Intenta de nuevo.')
        });
    });
  }
}


















/**
 * ModalTarjetaComponent (OnPush)
 * -----------------------------------------------------------------------------
 * NUEVO: snackbar de ÉXITO cuando la impresión se envía (validaciones OK).
 * Sigue permitiendo hacer click en "Imprimir" aunque esté inválido; en ese caso,
 * NO imprime y muestra mensajes de error + snackbar de validación.
 * Validaciones:
 *  - requerido
 *  - mínimo 2 palabras (cualquier tamaño)
 *  - solo letras y espacios en MAYÚSCULAS
 *  - máximo 40 caracteres, dividido en 2 líneas (20 y 20 máx) sin cortar palabras
 * Comunicación con el padre:
 *  - Emite `nombreCambiado` en cada cambio para reflejarlo en la grilla al vuelo.
 * -----------------------------------------------------------------------------
 */

import {
  Component, Inject, ChangeDetectionStrategy, ChangeDetectorRef, EventEmitter, Output
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

// Modelo mínimo que usa el modal (coincide con la fila)
export interface TarjetaLite {
  nombre: string;
  numero: string;
  fechaEmision?: string;
  fechaVencimiento?: string;
  motivo?: string;
  numeroCuenta?: string;
}

@Component({
  selector: 'app-modal-tarjeta',
  standalone: true,
  imports: [
    CommonModule, MatDialogModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatButtonModule, MatSnackBarModule
  ],
  templateUrl: './modal-tarjeta.component.html',
  styleUrl: './modal-tarjeta.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ModalTarjetaComponent {

  // ===== Form reactivo =====
  public form!: FormGroup<{ nombre: FormControl<string> }>;

  // Límite total y por línea
  private readonly MAX_NAME_LEN = 40;
  private readonly MAX_LINE = 20;

  // Para mostrar (por si el template los usa)
  public line1 = '';
  public line2 = '';

  /** Emite el nombre en mayúsculas mientras el usuario escribe (padre lo usa para refrescar UI). */
  @Output() nombreCambiado = new EventEmitter<string>();

  constructor(
    @Inject(MAT_DIALOG_DATA) public readonly data: TarjetaLite,
    private readonly dialogRef: MatDialogRef<ModalTarjetaComponent>,
    private readonly fb: FormBuilder,
    private readonly cdr: ChangeDetectorRef,
    private readonly snackBar: MatSnackBar
  ) {
    // Inicializa el form con el nombre existente en MAYÚSCULAS
    const inicio = (data?.nombre ?? '').toUpperCase();
    this.form = this.fb.group({
      nombre: this.fb.control(inicio, {
        nonNullable: true,
        validators: [
          Validators.required,
          this.minTwoWords(),
          Validators.maxLength(this.MAX_NAME_LEN),
          Validators.pattern(/^[A-ZÑÁÉÍÓÚÜ\s]+$/) // solo letras mayúsculas y espacios
        ]
      })
    });

    // Emite al padre en cada cambio y calcula las dos líneas
    this.form.get('nombre')!.valueChanges.subscribe((val) => {
      const v = (val ?? '').toUpperCase();
      this.nombreCambiado.emit(v);
      const { line1, line2 } = this.computeTwoLines(v);
      this.line1 = line1; this.line2 = line2;
      this.cdr.markForCheck();
    });

    // Calcula líneas iniciales
    const init = this.computeTwoLines(inicio);
    this.line1 = init.line1; this.line2 = init.line2;
  }

  // ===== Validadores y helpers =====

  /** Al menos 2 palabras (si está vacío, deja que 'required' dispare). */
  private minTwoWords() {
    return (control: AbstractControl): ValidationErrors | null => {
      const raw = String(control.value ?? '').trim();
      if (!raw) return null; // required se encarga
      const words = raw.split(/\s+/).filter(Boolean);
      return words.length >= 2 ? null : { twoWords: true };
    };
  }

  /** Calcula 2 líneas (máx. 20 c/u) sin cortar palabras; si no cabe, prioriza no cortar. */
  private computeTwoLines(full: string): { line1: string; line2: string } {
    const tokens = (full ?? '').split(' ').filter(Boolean);
    let l1 = '', l2 = '';
    for (const t of tokens) {
      if (!l1.length || (l1.length + 1 + t.length) <= this.MAX_LINE) {
        l1 = l1 ? `${l1} ${t}` : t;
      } else if (!l2.length || (l2.length + 1 + t.length) <= this.MAX_LINE) {
        l2 = l2 ? `${l2} ${t}` : t;
      } else {
        // Si tampoco cabe en la 2da, lo dejamos fuera (regla original)
        break;
      }
    }
    return { line1: l1, line2: l2 };
  }

  // ===== Acciones =====

  /** Cierra el modal sin cambios. */
  public cerrarModal(): void {
    this.dialogRef.close(); // comportamiento actual
  }

  /**
   * Imprime:
   * - Permite click siempre, pero si es inválido: NO imprime, muestra errores y snackbar.
   * - Si es válido: actualiza líneas, llama `window.print()` y muestra snackbar de ÉXITO.
   *   Luego cierra el modal (se mantiene tu comportamiento actual).
   */
  public imprimirTarjeta(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      // Mensaje de validación (si está vacío, el 'required' dispara en el template)
      this.snackBar.open('No puedes imprimir: corrige los errores del nombre.', 'Cerrar', {
        duration: 4000, verticalPosition: 'top', horizontalPosition: 'center'
      });
      return;
    }

    const nombre = this.form.get('nombre')!.value.toUpperCase();
    const { line1, line2 } = this.computeTwoLines(nombre);
    this.line1 = line1; this.line2 = line2;
    this.cdr.markForCheck();

    // Aquí iría tu lógica real de impresión; se mantiene `window.print()`
    window.print();

    // ✅ Snackbar de ÉXITO
    this.snackBar.open('Impresión enviada a la impresora.', 'Cerrar', {
      duration: 3500, verticalPosition: 'top', horizontalPosition: 'center'
    });

    // Cerrar el modal (mantiene el flujo que ya usas)
    this.dialogRef.close({ printed: true });
  }
}









<!--
  Campo de "Nombre en tarjeta"
  - Muestra los mat-error cuando: requerido, dos palabras, patrón, maxlength.
  - El botón "Imprimir" siempre se puede presionar; si el form es inválido,
    NO imprime y se muestra snackbar + errores.
-->
<div class="modal-wrapper">
  <h3>Impresión de tarjeta</h3>

  <mat-form-field appearance="fill" class="w-100">
    <mat-label>Nombre en tarjeta</mat-label>
    <input matInput [formControl]="form.get('nombre')" placeholder="NOMBRE EN TARJETA" />
    <!-- Errores -->
    <mat-error *ngIf="form.get('nombre')?.hasError('required')">
      El nombre es obligatorio.
    </mat-error>
    <mat-error *ngIf="form.get('nombre')?.hasError('twoWords')">
      Ingresa al menos dos nombres.
    </mat-error>
    <mat-error *ngIf="form.get('nombre')?.hasError('pattern')">
      Solo letras y espacios en MAYÚSCULAS.
    </mat-error>
    <mat-error *ngIf="form.get('nombre')?.hasError('maxlength')">
      Máximo 40 caracteres.
    </mat-error>
  </mat-form-field>

  <!-- Vista previa simple de las dos líneas (opcional) -->
  <div class="preview">
    <div>{{ line1 }}</div>
    <div>{{ line2 }}</div>
  </div>

  <!-- Botones -->
  <div class="actions">
    <button mat-button color="primary" (click)="imprimirTarjeta()">Imprimir</button>
    <button mat-button color="warn" (click)="cerrarModal()">Cerrar</button>
  </div>
</div>
