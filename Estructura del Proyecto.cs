/**
 * ConsultaTarjetaComponent (OnPush)
 * -----------------------------------------------------------------------------
 * Angular 20 (standalone) + Material Table/Sort + Reactive Forms.
 *
 * COMPORTAMIENTO ACTUAL (con mejora solicitada):
 *  - Carga tarjetas por agencias (desde sesión/form).
 *  - Filtro por número (formato campo:valor).
 *  - Abre modal al click en una fila.
 *  - "Eliminar" remueve en UI y registra impresión en backend (flujo heredado).
 *  - ✅ NUEVO: Mientras escribes en el modal, el nombre se refleja en la tabla
 *    en tiempo real (solo UI). Si cierras sin imprimir, los cambios se ven en
 *    la grilla hasta que refresques; el backend no cambia.
 *
 * MEJORAS:
 *  - ChangeDetectionStrategy.OnPush para mejor rendimiento.
 *  - Uso de `cdr.markForCheck()` tras reasignaciones que afectan la vista.
 *  - `readonly`, optional chaining, helpers para reducir complejidad (Sonar).
 *  - Predicado de filtro centralizado; constantes para “magic numbers”.
 *  - Unsubscribe centralizado con `Subscription`.
 * -----------------------------------------------------------------------------
 */

/**
 * ConsultaTarjetaComponent (OnPush)
 * -----------------------------------------------------------------------------
 * Mejoras:
 *  - Hint "x/3" en campos de agencias.
 *  - Mensaje cuando el servicio NO devuelve data.
 *  - Campos de agencia: solo 3 dígitos (teclado/pegado/input).
 *  - Mantiene OnPush, filtros, modal y actualización de nombre en vivo (solo UI).
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

// Forms
import { ReactiveFormsModule, FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';

// RxJS
import { Subscription, take } from 'rxjs';

// Servicios / modelos
import { TarjetaService } from '../../../../core/services/tarjeta.service';
import { Tarjeta } from '../../../../core/models/tarjeta.model';
import { GetDetalleTarjetasImprimirResponseDto } from '../../../../core/models/getDetalleTarjetasImprimir.model';
import { ModalTarjetaComponent } from '../modal-tarjeta/modal-tarjeta.component';
import { AuthService } from '../../../../core/services/auth.service';

// Pipes standalone usados en el template
import { MaskCardNumberPipe } from '../../../../shared/pipes/mask-card-number.pipe';
import { MaskAccountNumberPipe } from '../../../../shared/pipes/mask-account-number.pipe';

@Component({
  selector: 'app-consulta-tarjeta',
  imports: [
    CommonModule, MatCardModule, MatDialogModule, MatTableModule,
    MatFormFieldModule, ReactiveFormsModule, MatInputModule,
    MatIconModule, MatSortModule, MatMenuModule, MatButtonModule,
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

  @ViewChild(MatSort, { static: true }) sort!: MatSort;

  public readonly dataSource = new MatTableDataSource<Tarjeta>([]);
  public readonly displayedColumns: ReadonlyArray<string> = [
    'numero', 'nombre', 'motivo', 'numeroCuenta', 'eliminar'
  ];

  public formularioAgencias!: FormGroup<{
    codigoAgenciaImprime: FormControl<string>;
    codigoAgenciaApertura: FormControl<string>;
  }>;

  public activateFilter = '';
  public usuarioICBS = '';
  public tarjetaSeleccionada: Tarjeta = {
    nombre: '', numero: '', fechaEmision: '', fechaVencimiento: '',
    motivo: '', numeroCuenta: ''
  };

  /** Mensaje cuando el servicio no devuelve data. */
  public noDataMessage: string | null = null;

  public getDetalleTarjetasImprimirResponseDto!: GetDetalleTarjetasImprimirResponseDto;

  private readonly subscription = new Subscription();

  constructor(
    private readonly authService: AuthService,
    private readonly datosTarjetaServices: TarjetaService,
    private readonly dialog: MatDialog,
    private readonly cdr: ChangeDetectorRef,
    private readonly fb: FormBuilder
  ) {
    this.configurarFiltros();
  }

  // ───────── Ciclo de vida ─────────
  ngOnInit(): void {
    // Form con validación “solo números” + largo máx 3
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

    // Valores desde la sesión + consulta inicial
    this.withActiveSession(() => {
      const ad = this.authService.currentUserValue?.activeDirectoryData;
      this.usuarioICBS = ad?.usuarioICBS ?? '';

      const codigoAgenciaImprime  = ad?.agenciaImprimeCodigo ?? '';
      const codigoAgenciaApertura = ad?.agenciaAperturaCodigo ?? '';

      this.formularioAgencias.patchValue({ codigoAgenciaImprime, codigoAgenciaApertura });
      // Limpiar mensaje previo
      this.noDataMessage = null;
      this.consultarMicroservicio(codigoAgenciaImprime, codigoAgenciaApertura);
      this.setSort();
    });
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  // ───────── Helpers infra ─────────
  private withActiveSession(action: () => void): void {
    this.subscription.add(
      this.authService.sessionActive$.pipe(take(1)).subscribe(isActive => {
        if (!isActive) { this.authService.logout(); return; }
        action();
      })
    );
  }

  private setSort(): void {
    this.dataSource.sort = this.sort;
  }

  private setAgenciasFromResponse(resp: GetDetalleTarjetasImprimirResponseDto): void {
    this.getDetalleTarjetasImprimirResponseDto = resp;
    this.formularioAgencias.patchValue({
      codigoAgenciaApertura: resp.agencia.agenciaAperturaCodigo,
      codigoAgenciaImprime: resp.agencia.agenciaImprimeCodigo
    });
    this.cdr.markForCheck();
  }

  // ───────── Filtro de tabla ─────────
  private configurarFiltros(): void {
    this.dataSource.filterPredicate = (data: Tarjeta, raw: string): boolean => {
      if (!raw) return true;
      const [field, ...rest] = raw.split(':');
      const term = rest.join(':').toLowerCase().trim();
      if (!field || !term) return true;

      const map: Record<string, string | undefined> = {
        numero: data.numero,
        nombre: data.nombre,
        motivo: data.motivo,
        numeroCuenta: data.numeroCuenta
      };
      return (map[field] ?? '').toString().toLowerCase().includes(term);
    };
  }

  public applyFilterFromInput(evt: Event, field: 'numero' | 'nombre' | 'motivo' | 'numeroCuenta'): void {
    const value = (evt.target as HTMLInputElement)?.value ?? '';
    this.activateFilter = `${field}:${value.trim().toLowerCase()}`;
    this.dataSource.filter = this.activateFilter;
  }

  // ───────── Consultas / Acciones ─────────
  private consultarMicroservicio(codigoAgenciaImprime: string, codigoAgenciaApertura: string): void {
    this.datosTarjetaServices
      .obtenerDatosTarjeta(this.BIN, codigoAgenciaImprime, codigoAgenciaApertura)
      .pipe(take(1))
      .subscribe({
        next: (response) => {
          this.dataSource.data = response.tarjetas;

          // Mensaje cuando no hay data
          const sinTarjetas = !response?.tarjetas?.length;
          this.noDataMessage = sinTarjetas
            ? `No hay datos para esa Agencia de apertura ${codigoAgenciaApertura} y Agencia de impresión ${codigoAgenciaImprime}.`
            : null;

          this.setAgenciasFromResponse(response);
          this.cdr.markForCheck();
        },
        error: (error) => {
          console.error('Error al consultar el microservicio', error);
          // Si hubo error, no sobreescribimos noDataMessage con un falso positivo
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
      this.noDataMessage = null; // limpiar antes de consultar
      this.consultarMicroservicio(agenciaImprimeCodigo, agenciaAperturaCodigo);
    });
  }

  public recargarDatos(): void {
    this.actualizarTabla();
  }

  // ───────── Modal / nombre en vivo ─────────
  public abrirModal(row: Tarjeta): void {
    this.tarjetaSeleccionada = row;

    const ref = this.dialog.open(ModalTarjetaComponent, {
      data: row,
      width: '720px',
      disableClose: true
    });

    // Reflejar nombre en la grilla mientras se escribe en el modal (solo UI)
    const cmp = ref.componentInstance;
    if (cmp?.nombreCambiado) {
      const subNombre = cmp.nombreCambiado.subscribe((nuevoNombre: string) => {
        this.updateRowName(row.numero, nuevoNombre);
      });
      const closeSub = ref.afterClosed().pipe(take(1)).subscribe(() => subNombre.unsubscribe());
      this.subscription.add(subNombre);
      this.subscription.add(closeSub);
    }

    this.subscription.add(ref.afterClosed().pipe(take(1)).subscribe());
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

  // ───────── Solo 3 dígitos: handlers de input ─────────

  /** Permite teclas de control (borrar, flechas, tab, combinaciones Ctrl/Cmd). */
  private isControlKey(e: KeyboardEvent): boolean {
    const k = e.key;
    const ctrl = e.ctrlKey || e.metaKey;
    return (
      k === 'Backspace' || k === 'Delete' || k === 'ArrowLeft' || k === 'ArrowRight' ||
      k === 'Tab' || k === 'Home' || k === 'End' ||
      (ctrl && ['a', 'c', 'v', 'x'].includes(k.toLowerCase()))
    );
  }

  /**
   * keydown: bloquea cualquier char que no sea dígito y evita exceder 3.
   * Considera selección: si hay texto seleccionado, permite reemplazarlo.
   */
  public onKeyDownDigits(e: KeyboardEvent, controlName: 'codigoAgenciaImprime' | 'codigoAgenciaApertura'): void {
    if (this.isControlKey(e)) return;

    const input = e.target as HTMLInputElement;
    const key = e.key;

    // Bloquear si no es dígito 0-9
    if (!/^\d$/.test(key)) {
      e.preventDefault();
      return;
    }

    // Longitud resultante tras reemplazar la selección
    const selStart = input.selectionStart ?? input.value.length;
    const selEnd = input.selectionEnd ?? input.value.length;
    const selected = Math.max(0, selEnd - selStart);
    const currentLen = (input.value ?? '').length;
    const resultingLen = currentLen - selected + 1; // +1 por el dígito tecleado

    if (resultingLen > this.MAX_DIGITS) {
      e.preventDefault();
      return;
    }
  }

  /**
   * paste: sanea a dígitos y recorta a 3 según la selección actual.
   */
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

  /**
   * input: red de seguridad. Limpia no-dígitos y recorta a 3 si algo se coló.
   */
  public onInputSanitize(controlName: 'codigoAgenciaImprime' | 'codigoAgenciaApertura'): void {
    const ctrl = this.formularioAgencias.get(controlName);
    const raw = String(ctrl?.value ?? '');
    const clean = raw.replace(/\D+/g, '').slice(0, this.MAX_DIGITS);
    if (clean !== raw) {
      ctrl?.setValue(clean, { emitEvent: false });
      this.cdr.markForCheck();
    }
  }

  // ───────── Eliminar (flujo heredado) ─────────
  public eliminarTarjeta(event: MouseEvent, numeroTarjeta: string): void {
    event.stopPropagation();

    this.withActiveSession(() => {
      this.dataSource.data = this.dataSource.data.filter(item => item.numero !== numeroTarjeta);
      this.cdr.markForCheck();

      const nombreParaRegistrar =
        this.tarjetaSeleccionada?.nombre?.toUpperCase?.() ||
        this.dataSource.data.find(x => x.numero === numeroTarjeta)?.nombre?.toUpperCase?.() ||
        '';

      this.datosTarjetaServices
        .guardaEstadoImpresion(numeroTarjeta, this.usuarioICBS, nombreParaRegistrar)
        .pipe(take(1))
        .subscribe({
          next: () => { /* comportamiento actual: sin refresh adicional */ },
          error: (error) => console.error('Error al registrar impresión', error)
        });
    });
  }
}




<!--
  Campos de agencia:
  - Hint 3/3 y validaciones.
  - Solo 3 dígitos: handlers (keydown/paste/input) + maxlength + inputmode.
-->
<form [formGroup]="formularioAgencias" (ngSubmit)="actualizarTabla()">
  <div class="agencia-info">

    <div class="fila">
      <span class="titulo">Agencia Imprime:</span>

      <mat-form-field appearance="fill" class="campo-corto">
        <mat-label>Código</mat-label>
        <input
          matInput
          placeholder="Código"
          formControlName="codigoAgenciaImprime"
          autocomplete="off"
          inputmode="numeric"
          pattern="[0-9]*"
          maxlength="3"
          (keydown)="onKeyDownDigits($event, 'codigoAgenciaImprime')"
          (paste)="onPasteDigits($event, 'codigoAgenciaImprime')"
          (input)="onInputSanitize('codigoAgenciaImprime')"
          (keyup.enter)="actualizarTabla()"
        />
        <!-- Hint 3/3 -->
        <mat-hint align="end">{{ (formularioAgencias.get('codigoAgenciaImprime')?.value?.length || 0) }}/3</mat-hint>

        @if (hasFormControlError('codigoAgenciaImprime', 'required')) {
          <mat-error>Este campo es requerido.</mat-error>
        }
        @if (hasFormControlError('codigoAgenciaImprime', 'pattern')) {
          <mat-error>Solo números son permitidos.</mat-error>
        }
        @if (hasFormControlError('codigoAgenciaImprime', 'maxlength')) {
          <mat-error>Máximo 3 dígitos.</mat-error>
        }
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
          matInput
          placeholder="Código"
          formControlName="codigoAgenciaApertura"
          autocomplete="off"
          inputmode="numeric"
          pattern="[0-9]*"
          maxlength="3"
          (keydown)="onKeyDownDigits($event, 'codigoAgenciaApertura')"
          (paste)="onPasteDigits($event, 'codigoAgenciaApertura')"
          (input)="onInputSanitize('codigoAgenciaApertura')"
          (keyup.enter)="actualizarTabla()"
        />
        <!-- Hint 3/3 -->
        <mat-hint align="end">{{ (formularioAgencias.get('codigoAgenciaApertura')?.value?.length || 0) }}/3</mat-hint>

        @if (hasFormControlError('codigoAgenciaApertura', 'required')) {
          <mat-error>Este campo es requerido.</mat-error>
        }
        @if (hasFormControlError('codigoAgenciaApertura', 'pattern')) {
          <mat-error>Solo números son permitidos.</mat-error>
        }
        @if (hasFormControlError('codigoAgenciaApertura', 'maxlength')) {
          <mat-error>Máximo 3 dígitos.</mat-error>
        }
      </mat-form-field>

      <span class="nombre-agencia">
        {{ getDetalleTarjetasImprimirResponseDto?.agencia?.agenciaAperturaNombre }}
      </span>
    </div>

  </div>
</form>

<!-- Mensaje cuando no hay data -->
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

    <!-- No. de Tarjeta -->
    <ng-container matColumnDef="numero">
      <mat-header-cell *matHeaderCellDef>No. de Tarjeta</mat-header-cell>
      <mat-cell *matCellDef="let tarjetas">
        {{ tarjetas.numero | maskCardNumber }}
      </mat-cell>
    </ng-container>

    <!-- Nombre en Tarjeta -->
    <ng-container matColumnDef="nombre">
      <mat-header-cell *matHeaderCellDef>Nombre en Tarjeta</mat-header-cell>
      <mat-cell *matCellDef="let tarjetas">
        {{ tarjetas.nombre | uppercase }}
      </mat-cell>
    </ng-container>

    <!-- Motivo -->
    <ng-container matColumnDef="motivo">
      <mat-header-cell *matHeaderCellDef>Motivo</mat-header-cell>
      <mat-cell *matCellDef="let tarjetas">
        {{ tarjetas.motivo }}
      </mat-cell>
    </ng-container>

    <!-- Número de Cuenta -->
    <ng-container matColumnDef="numeroCuenta">
      <mat-header-cell *matHeaderCellDef>Número de Cuenta</mat-header-cell>
      <mat-cell *matCellDef="let tarjetas">
        {{ tarjetas.numeroCuenta | maskAccountNumber }}
      </mat-cell>
    </ng-container>

    <!-- Eliminar -->
    <ng-container matColumnDef="eliminar">
      <mat-header-cell *matHeaderCellDef>Eliminar</mat-header-cell>
      <mat-cell *matCellDef="let tarjetas">
        <mat-icon class="matIcon" (click)="eliminarTarjeta($event, tarjetas.numero)">delete</mat-icon>
      </mat-cell>
    </ng-container>

    <!-- Filas -->
    <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
    <mat-row
      *matRowDef="let row; columns: displayedColumns;"
      (click)="abrirModal(row)">
    </mat-row>

  </mat-table>
</div>
