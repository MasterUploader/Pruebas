<!-- Overlay de CARGANDO -->
@if (isLoading) {
  <div class="loading-overlay" style="position: fixed; inset: 0; background: rgba(0,0,0,.35);
       display: grid; place-items: center; z-index: 1000;">
    <div style="background: #fff; padding: 24px 28px; border-radius: 12px;
         display: flex; flex-direction: column; align-items: center; gap: 12px;
         box-shadow: 0 10px 30px rgba(0,0,0,.25);">
      <mat-progress-spinner mode="indeterminate" [diameter]="56"></mat-progress-spinner>
      <div style="font-weight:600;">Cargando…</div>
    </div>
  </div>
}

<!-- Form de agencias: hints, validación y handlers de 3 dígitos -->
<form [formGroup]="formularioAgencias" (ngSubmit)="actualizarTabla()">
  <div class="agencia-info">

    <div class="fila">
      <span class="titulo">Agencia Imprime:</span>

      <mat-form-field appearance="fill" class="campo-corto">
        <mat-label>Código</mat-label>
        <input
          matInput placeholder="Código" formControlName="codigoAgenciaImprime"
          autocomplete="off" inputmode="numeric" pattern="[0-9]*" maxlength="3"
          (keydown)="onKeyDownDigits($event, 'codigoAgenciaImprime')"
          (paste)="onPasteDigits($event, 'codigoAgenciaImprime')"
          (input)="onInputSanitize('codigoAgenciaImprime')"
          (keyup.enter)="actualizarTabla()"
        />
        <mat-hint align="end">{{ (formularioAgencias.get('codigoAgenciaImprime')?.value?.length || 0) }}/3</mat-hint>

        @if (hasFormControlError('codigoAgenciaImprime', 'required')) { <mat-error>Este campo es requerido.</mat-error> }
        @if (hasFormControlError('codigoAgenciaImprime', 'pattern'))  { <mat-error>Solo números son permitidos.</mat-error> }
        @if (hasFormControlError('codigoAgenciaImprime', 'maxlength')){ <mat-error>Máximo 3 dígitos.</mat-error> }
      </mat-form-field>

      <span class="nombre-agencia">
        {{ getDetalleTarjetasImprimirResponseDto?.agencia?.agenciaImprimeNombre }}
      </span>
    </div>

    <div class="fila">
      <span class="titulo">Agencia Apertura:</span>

      <mat-form-field appearance="fill" class="campo-corto">
        <mat-label>Código</mat-label>
        <input
          matInput placeholder="Código" formControlName="codigoAgenciaApertura"
          autocomplete="off" inputmode="numeric" pattern="[0-9]*" maxlength="3"
          (keydown)="onKeyDownDigits($event, 'codigoAgenciaApertura')"
          (paste)="onPasteDigits($event, 'codigoAgenciaApertura')"
          (input)="onInputSanitize('codigoAgenciaApertura')"
          (keyup.enter)="actualizarTabla()"
        />
        <mat-hint align="end">{{ (formularioAgencias.get('codigoAgenciaApertura')?.value?.length || 0) }}/3</mat-hint>

        @if (hasFormControlError('codigoAgenciaApertura', 'required')) { <mat-error>Este campo es requerido.</mat-error> }
        @if (hasFormControlError('codigoAgenciaApertura', 'pattern'))  { <mat-error>Solo números son permitidos.</mat-error> }
        @if (hasFormControlError('codigoAgenciaApertura', 'maxlength')){ <mat-error>Máximo 3 dígitos.</mat-error> }
      </mat-form-field>

      <span class="nombre-agencia">
        {{ getDetalleTarjetasImprimirResponseDto?.agencia?.agenciaAperturaNombre }}
      </span>
    </div>

  </div>
</form>

<!-- Banner adicional cuando no hay data -->
@if (noDataMessage) {
  <div class="alerta-sin-datos" style="margin: 8px 0; display:flex; align-items:center; gap:8px; color:#b00020;">
    <mat-icon color="warn">error</mat-icon>
    <span>{{ noDataMessage }}</span>
  </div>
}

<!-- Encabezado -->
<div class="contenedor-titulo">
  <mat-card>
    <mat-card-header>
      <mat-card-title>Detalle Tarjetas Por Imprimir</mat-card-title>
    </mat-card-header>
  </mat-card>
</div>

<!-- Filtro y botón refrescar -->
<div class="filtro-tabla">
  <mat-form-field appearance="fill">
    <mat-label>Filtro por No. Tarjeta</mat-label>
    <input matInput (input)="applyFilterFromInput($event, 'numero')" placeholder="Escribe para filtrar" />
  </mat-form-field>

  <button mat-button (click)="recargarDatos()">
    <mat-icon>refresh</mat-icon>
    Refrescar
  </button>
</div>

<!-- Tabla -->
<div class="table-container">
  <mat-table [dataSource]="dataSource" matSort class="mat-elevation-z8">

    <ng-container matColumnDef="numero">
      <mat-header-cell *matHeaderCellDef>No. de Tarjeta</mat-header-cell>
      <mat-cell *matCellDef="let tarjetas">{{ tarjetas.numero | maskCardNumber }}</mat-cell>
    </ng-container>

    <ng-container matColumnDef="nombre">
      <mat-header-cell *matHeaderCellDef>Nombre en Tarjeta</mat-header-cell>
      <mat-cell *matCellDef="let tarjetas">{{ tarjetas.nombre | uppercase }}</mat-cell>
    </ng-container>

    <ng-container matColumnDef="motivo">
      <mat-header-cell *matHeaderCellDef>Motivo</mat-header-cell>
      <mat-cell *matCellDef="let tarjetas">{{ tarjetas.motivo }}</mat-cell>
    </ng-container>

    <ng-container matColumnDef="numeroCuenta">
      <mat-header-cell *matHeaderCellDef>Número de Cuenta</mat-header-cell>
      <mat-cell *matCellDef="let tarjetas">{{ tarjetas.numeroCuenta | maskAccountNumber }}</mat-cell>
    </ng-container>

    <!-- Botón Eliminar accesible -->
    <ng-container matColumnDef="eliminar">
      <mat-header-cell *matHeaderCellDef>Eliminar</mat-header-cell>
      <mat-cell *matCellDef="let tarjetas">
        <button
          mat-icon-button
          type="button"
          aria-label="Eliminar"
          (click)="eliminarTarjeta($event, tarjetas.numero)">
          <mat-icon>delete</mat-icon>
        </button>
      </mat-cell>
    </ng-container>

    <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>

    <!-- Fila accesible por teclado: Enter/Espacio abre modal -->
    <mat-row
      *matRowDef="let row; columns: displayedColumns;"
      (click)="abrirModal(row)"
      (keydown.enter)="onRowKeyOpen($event, row)"
      (keydown.space)="onRowKeyOpen($event, row)"
      tabindex="0"
      [attr.aria-label]="'Abrir modal de la tarjeta ' + (row?.numero || '')">
    </mat-row>

  </mat-table>
</div>



/**
 * ConsultaTarjetaComponent (OnPush)
 * -----------------------------------------------------------------------------
 * - Overlay de carga (spinner) para las 3 vías de consulta.
 * - Snackbar cuando el API no devuelve datos.
 * - Validación de agencias: solo 3 dígitos (keydown/paste/input).
 * - Accesibilidad: fila abre modal con Enter/Espacio; botón Eliminar es <button>.
 * - Actualización en vivo del nombre desde el modal (solo UI).
 * -----------------------------------------------------------------------------
 */

import {
  Component,
  OnInit,
  ChangeDetectorRef,
  ViewChild,
  OnDestroy,
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

  // ───────── Configuración / Constantes ─────────
  private readonly BIN = '411052';
  private readonly MAX_DIGITS = 3;
  private readonly ONLY_DIGITS_RE = /^\d+$/;

  // ───────── Material Table ─────────
  @ViewChild(MatSort, { static: true }) sort!: MatSort;
  public readonly dataSource = new MatTableDataSource<Tarjeta>([]);
  public readonly displayedColumns: ReadonlyArray<string> =
    ['numero', 'nombre', 'motivo', 'numeroCuenta', 'eliminar'];

  // ───────── Form ─────────
  public formularioAgencias!: FormGroup<{
    codigoAgenciaImprime: FormControl<string>;
    codigoAgenciaApertura: FormControl<string>;
  }>;

  // ───────── Estado UI ─────────
  public activateFilter = '';
  public usuarioICBS = '';
  public tarjetaSeleccionada: Tarjeta = {
    nombre: '', numero: '', fechaEmision: '', fechaVencimiento: '',
    motivo: '', numeroCuenta: ''
  };
  /** Banner adicional cuando no hay data (además del snackbar). */
  public noDataMessage: string | null = null;
  /** Overlay de carga. */
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

  // ───────── Ciclo de vida ─────────
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

    // Valores desde la sesión + consulta inicial (con overlay)
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

  // ───────── Helpers infra ─────────
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

  private showSnack(message: string): void {
    this.snackBar.dismiss();
    this.snackBar.open(message, 'Cerrar', {
      duration: 4000, verticalPosition: 'top', horizontalPosition: 'center'
    });
  }

  // ───────── Filtro de tabla ─────────
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

  // ───────── Consultas ─────────
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
        error: (error) => {
          console.error('Error al consultar el microservicio', error);
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

  // ───────── Modal / nombre en vivo ─────────
  public abrirModal(row: Tarjeta): void {
    this.tarjetaSeleccionada = row;

    const ref = this.dialog.open(ModalTarjetaComponent, {
      data: row, width: '720px', disableClose: true
    });

    // Reflejar nombre en la grilla mientras se escribe en el modal (solo UI)
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

  /** Accesibilidad: abrir modal con teclado (Enter/Espacio) desde la fila. */
  public onRowKeyOpen(e: Event, row: Tarjeta): void {
    e.preventDefault();      // evita scroll cuando se usa Space
    this.abrirModal(row);
  }

  private updateRowName(numero: string, nuevoNombre: string): void {
    const nueva = this.dataSource.data.map(r =>
      r.numero === numero ? { ...r, nombre: (nuevoNombre ?? '').toUpperCase() } : r
    );
    this.dataSource.data = nueva;
    this.cdr.markForCheck();
  }

  public onNombreCambiado(nuevoNombre: string): void {
    this.tarjetaSeleccionada.nombre = nuevoNombre;
    this.cdr.markForCheck();
  }

  // ───────── Validación visual de form ─────────
  public hasFormControlError(
    controlName: keyof ConsultaTarjetaComponent['formularioAgencias']['controls'],
    errorName: string
  ): boolean {
    const control = this.formularioAgencias.get(controlName as string);
    return !!control && control.touched && control.hasError(errorName);
  }

  // ───────── Solo 3 dígitos: handlers ─────────
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

  // ───────── Eliminar (click o teclado en <button>) ─────────
  public eliminarTarjeta(event: Event, numeroTarjeta: string): void {
    event.stopPropagation();
    // Calcula el nombre una sola vez, evitando optional chaining innecesario.
    const seleccionado = this.tarjetaSeleccionada.nombre;
    const encontrado = this.dataSource.data.find(x => x.numero === numeroTarjeta)?.nombre;
    const nombreParaRegistrar = (seleccionado || encontrado || '').toUpperCase();

    this.withActiveSession(() => {
      // Quita la fila de la UI
      this.dataSource.data = this.dataSource.data.filter(item => item.numero !== numeroTarjeta);
      this.cdr.markForCheck();

      // Registrar estado de impresión
      this.datosTarjetaServices
        .guardaEstadoImpresion(numeroTarjeta, this.usuarioICBS, nombreParaRegistrar)
        .pipe(take(1))
        .subscribe({
          next: () => { /* sin acciones extra: flujo actual */ },
          error: (error) => console.error('Error al registrar impresión', error)
        });
    });
  }
}
