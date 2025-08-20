<!-- Overlay de CARGANDO -->
@if (isLoading) {
  <div
    class="loading-overlay"
    style="
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.35);
      display: grid;
      place-items: center;
      z-index: 1000;
    "
  >
    <div
      style="
        background: #fff;
        padding: 24px 28px;
        border-radius: 12px;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 12px;
        box-shadow: 0 10px 30px rgba(0, 0, 0, 0.25);
      "
    >
      <mat-progress-spinner mode="indeterminate" [diameter]="56"></mat-progress-spinner>
      <div style="font-weight: 600">Cargando…</div>
    </div>
  </div>
}

<!-- ===== Vista: Header fijo + tabla con paginación (sin scroll vertical) ===== -->
<div class="vista-consulta">
  <!-- ===== Header fijo ===== -->
  <div class="panel-fijo">
    <!-- Form de agencias -->
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
            <mat-hint align="end">
              {{ formularioAgencias.get('codigoAgenciaImprime')?.value?.length || 0 }}/3
            </mat-hint>

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
            <mat-hint align="end">
              {{ formularioAgencias.get('codigoAgenciaApertura')?.value?.length || 0 }}/3
            </mat-hint>

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

    <!-- Mensaje sin data -->
    @if (noDataMessage) {
      <div class="alerta-sin-datos">
        <mat-icon color="warn">error</mat-icon>
        <span>{{ noDataMessage }}</span>
      </div>
    }

    <!-- Título -->
    <div class="encabezado">
      <mat-card>
        <mat-card-header>
          <div class="title-wrap">
            <mat-card-title>Detalle Tarjetas Por Imprimir</mat-card-title>
          </div>
        </mat-card-header>
      </mat-card>
    </div>

    <!-- Filtro + botón -->
    <div class="filtro-tabla">
      <mat-form-field appearance="fill">
        <mat-label>Filtro por No. Tarjeta</mat-label>
        <input
          matInput
          placeholder="Escribe para filtrar"
          (input)="applyFilterFromInput($event, 'numero')"
        />
      </mat-form-field>

      <button mat-button type="button" (click)="recargarDatos()">
        <mat-icon>refresh</mat-icon>
        Refrescar
      </button>
    </div>
  </div>

  <!-- ===== Tabla + Paginador (solo scroll horizontal si hace falta) ===== -->
  <div class="tabla-scroll">
    <mat-table
      [dataSource]="dataSource"
      matSort
      (matSortChange)="onSortChange($event)"
      class="mat-elevation-z8 tabla-tarjetas"
      role="table"
    >
      <!-- Col: No. Tarjeta -->
      <ng-container matColumnDef="numero">
        <mat-header-cell *matHeaderCellDef mat-sort-header>No. de Tarjeta</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.numero }}</mat-cell>
      </ng-container>

      <!-- Col: Nombre -->
      <ng-container matColumnDef="nombre">
        <mat-header-cell *matHeaderCellDef mat-sort-header>Nombre en Tarjeta</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.nombre | uppercase }}</mat-cell>
      </ng-container>

      <!-- Col: Motivo -->
      <ng-container matColumnDef="motivo">
        <mat-header-cell *matHeaderCellDef mat-sort-header>Motivo</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.motivo }}</mat-cell>
      </ng-container>

      <!-- Col: Número de Cuenta -->
      <ng-container matColumnDef="numeroCuenta">
        <mat-header-cell *matHeaderCellDef mat-sort-header>Número de Cuenta</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.numeroCuenta }}</mat-cell>
      </ng-container>

      <!-- Col: Eliminar -->
      <ng-container matColumnDef="eliminar">
        <mat-header-cell *matHeaderCellDef>Eliminar</mat-header-cell>
        <mat-cell *matCellDef="let t">
          <button
            mat-icon-button
            type="button"
            aria-label="Eliminar"
            (click)="eliminarTarjeta($event, t.numero)"
          >
            <mat-icon>delete</mat-icon>
          </button>
        </mat-cell>
      </ng-container>

      <!-- Filas -->
      <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
      <mat-row
        *matRowDef="let row; columns: displayedColumns"
        (click)="abrirModal(row)"
        (keydown.enter)="onRowKeyOpen($event, row)"
        (keydown.space)="onRowKeyOpen($event, row)"
        tabindex="0"
        [attr.aria-label]="'Abrir modal de la tarjeta ' + (row?.numero || '')"
      ></mat-row>
    </mat-table>

    <!-- Paginador sticky (en cliente) -->
    <mat-paginator
      [length]="dataSource?.data?.length || 0"
      [pageSize]="10"
      [pageSizeOptions]="[10, 25, 50, 100]"
      aria-label="Selector de página"
      class="paginator-sticky">
    </mat-paginator>
  </div>
</div>



/* El host del componente ocupa todo el alto disponible del shell */
:host {
  display: block;
  height: 100%;
}

/* ===== Layout: header fijo + área de tabla ===== */
.vista-consulta {
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.panel-fijo {
  flex: 0 0 auto;
  padding: 12px 16px 8px;
  background: #fafafa;
  border-bottom: 1px solid #e0e0e0;
}

/* Solo scroll HORIZONTAL (el paginator limita filas, no hace falta scroll Y) */
.tabla-scroll {
  flex: 1 1 auto;
  min-height: 0;
  overflow-x: auto;   /* 👈 horizontal si hay muchas columnas */
  overflow-y: hidden; /* 👈 sin scroll vertical cuando hay paginador */
  padding: 8px 16px;
  background: #fff;
}

/* Tabla */
.tabla-tarjetas { width: 100%; }

/* Header sticky dentro del área con scroll horizontal */
.tabla-tarjetas .mat-header-row {
  position: sticky;
  top: 0;
  z-index: 2;
  background: #fff;
  box-shadow: 0 1px 0 rgba(0,0,0,.06);
}

/* Celdas compactas */
.tabla-tarjetas .mat-mdc-cell,
.tabla-tarjetas .mat-mdc-header-cell {
  padding: 8px 12px;
  white-space: nowrap; /* quita si quieres multilínea */
}

/* Hover fila */
.tabla-tarjetas tr.mat-mdc-row:hover {
  background: rgba(0,0,0,.03);
}

/* Paginador pegado abajo, visible siempre */
.paginator-sticky {
  position: sticky;
  bottom: 0;
  z-index: 3;
  background: #fff;
  border-top: 1px solid #eee;
}

/* ===== Estilos existentes ===== */
.agencia-info .fila {
  display: flex;
  align-items: center;
  gap: 12px;
  margin-bottom: 10px;
}
.agencia-info .titulo { font-weight: bold; }
.nombre-agencia { margin-left: 8px; }

.alerta-sin-datos {
  margin: 8px 0;
  display: flex;
  align-items: center;
  gap: 8px;
  color: #b00020;
}

.encabezado mat-card .mat-card-header { justify-content: center; }
.encabezado .title-wrap {
  width: 100%;
  display: flex;
  justify-content: center;
}
.encabezado mat-card-title {
  width: 100%;
  text-align: center;
  margin: 0;
  font-weight: 600;
}

.filtro-tabla {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-top: 8px;
}

.campo-corto { height: 70px; width: 70px; }






import { AfterViewInit, Component, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';

import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatSort, MatSortModule, Sort } from '@angular/material/sort';
import { MatPaginator, MatPaginatorModule } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { finalize, firstValueFrom } from 'rxjs';

/* ===== Ajusta este tipo a tu modelo real ===== */
export interface Tarjeta {
  numero: string;
  nombre: string;
  motivo: string;
  numeroCuenta: string;
}

/* ===== Ajusta el nombre/ruta del servicio y su método ===== */
export abstract class TarjetasService {
  abstract obtenerTarjetas(): any; // Observable<Tarjeta[]>
  // abstract obtenerTarjetas(params: ...): any; // si luego haces paginación en servidor
}

@Component({
  selector: 'app-consulta-tarjeta',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,

    // Material
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatCardModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './consulta-tarjeta.component.html',
  styleUrls: ['./consulta-tarjeta.component.css']
})
export class ConsultaTarjetaComponent implements AfterViewInit {
  /* ======= UI / estado ======= */
  isLoading = false;
  noDataMessage = '';

  /* ======= Form ======= */
  formularioAgencias: FormGroup;

  /* ======= Tabla + paginación (cliente) ======= */
  displayedColumns = ['numero', 'nombre', 'motivo', 'numeroCuenta', 'eliminar'];
  dataSource = new MatTableDataSource<Tarjeta>([]);

  @ViewChild(MatSort) sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  /* ======= Datos auxiliares que ya usabas ======= */
  getDetalleTarjetasImprimirResponseDto: any = null;

  constructor(
    private readonly fb: FormBuilder,
    private readonly tarjetasSvc: TarjetasService // 👈 inyecta tu implementación real
  ) {
    // Ajusta validaciones exactamente como las tienes hoy
    this.formularioAgencias = this.fb.group({
      codigoAgenciaImprime: [''],
      codigoAgenciaApertura: ['']
    });

    // Filtro por defecto: case-insensitive sobre "numero"
    this.dataSource.filterPredicate = (row, filter) =>
      (row.numero ?? '').toString().toLowerCase().includes(filter);
  }

  /* ================== Ciclo de vida ================== */
  ngAfterViewInit(): void {
    this.dataSource.sort = this.sort;
    this.dataSource.paginator = this.paginator;

    // Primera carga
    this.recargarDatos().catch(() => {});
  }

  /* ================== Carga de datos ================== */
  async recargarDatos(): Promise<void> {
    this.isLoading = true;
    this.noDataMessage = '';
    try {
      const filas = await firstValueFrom<Tarjeta[]>(
        this.tarjetasSvc.obtenerTarjetas()
      );
      this.dataSource.data = filas ?? [];
      if (this.paginator) this.paginator.firstPage();
      this.noDataMessage = (filas && filas.length > 0) ? '' : 'No hay datos para mostrar.';
    } finally {
      this.isLoading = false;
    }
  }

  /* ================== Filtro (por número) ================== */
  applyFilterFromInput(ev: Event, _campo: 'numero'): void {
    const value = (ev.target as HTMLInputElement).value?.trim().toLowerCase() ?? '';
    this.dataSource.filter = value;
    if (this.paginator) this.paginator.firstPage();
  }

  /* ================== Sort (cliente) ================== */
  onSortChange(_sort: Sort): void {
    // En cliente no necesitas nada: MatTableDataSource reordena solo.
    // Si migras a servidor, dispara aquí la recarga con sort.
  }

  /* ================== Handlers existentes (stubs) ================== */
  actualizarTabla(): void {
    // Si tu submit del form debe recargar, deja esto:
    this.recargarDatos().catch(() => {});
  }

  onKeyDownDigits(_ev: KeyboardEvent, _control: 'codigoAgenciaImprime'|'codigoAgenciaApertura'): void {
    // TODO: tu validación actual de solo dígitos
  }

  onPasteDigits(_ev: ClipboardEvent, _control: 'codigoAgenciaImprime'|'codigoAgenciaApertura'): void {
    // TODO: tu validación actual de solo dígitos
  }

  onInputSanitize(_control: 'codigoAgenciaImprime'|'codigoAgenciaApertura'): void {
    // TODO: tu limpieza actual del input
  }

  hasFormControlError(_control: string, _error: string): boolean {
    // TODO: implementa exactamente como lo tienes hoy
    return false;
  }

  abrirModal(row: Tarjeta): void {
    // TODO: tu implementación actual
    console.log('abrir modal', row);
  }

  onRowKeyOpen(ev: Event, row: Tarjeta): void {
    ev.preventDefault();
    this.abrirModal(row);
  }

  eliminarTarjeta(_ev: MouseEvent, numero: string): void {
    // TODO: tu implementación actual de “marcar impresa” o eliminación
    console.log('eliminar', numero);
  }
}
