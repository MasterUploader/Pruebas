Si bien con ese cambio ahora solo la tabla tiene scroll, ese scroll no permite ver todos los datos de la tabla, te muestro el codigo modificado


/* ===== Layout general: header fijo + área con scroll ===== */
/* contenedor de toda la vista del componente */
.vista-consulta {
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;  /* 👈 importante para que el hijo pueda scrollear */
}

/* header fijo (agencias, mensajes, título, filtros) */
.panel-fijo {
  flex: 0 0 auto;
  padding: 12px 16px 8px;
  background: #fafafa;
  border-bottom: 1px solid #e0e0e0;
}

/* SOLO aquí habrá scroll vertical */
.tabla-scroll {
  flex: 1 1 auto;
  min-height: 0;      /* 👈 clave en layouts flex */
  overflow-y: auto;   /* 👈 la única barra de scroll */
  overflow-x: auto;
  padding: 8px 16px;
  background: #fff;
}

/* header de la mat-table pegado arriba dentro del área con scroll */
.tabla-tarjetas .mat-header-row {
  position: sticky;
  top: 0;
  z-index: 2;
  background: #fff;
  box-shadow: 0 1px 0 rgba(0,0,0,.06);
}

@media (max-width: 768px) {
  .vista-consulta { height: calc(100vh - 48px); } /* navbar móvil */
}


/* ===== Estilos existentes (ajustados) ===== */
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

/* Filtro */
.filtro-tabla {
  display: flex;
  align-items: center;
  gap: 12px;
  padding-top: 8px;
}

/* Tabla */
.tabla-tarjetas { width: 100%; }


/* Celdas compactas */
.tabla-tarjetas .mat-mdc-cell,
.tabla-tarjetas .mat-mdc-header-cell {
  padding: 8px 12px;
  white-space: nowrap; /* quita si necesitas multilínea */
}

/* Hover fila */
.tabla-tarjetas tr.mat-mdc-row:hover {
  background: rgba(0,0,0,.03);
}

/* Campo corto */
.campo-corto { height: 70px; width: 70px; }

/* Ya no usamos .table-container para el layout del scroll */



styles.css


/* You can add global styles to this file, and also import other style files */


@import "../node_modules/@angular/material/prebuilt-themes/deeppurple-amber.css";

/* El documento no scrollea */
html, body {
  height: 100%;
  margin: 0;
  padding: 0;
}
body {
  overflow: hidden; /* 👈 sin scroll global */
}

/* Estructura del app: toolbar arriba + contenido flexible */
app-root {
  height: 100%;
  display: flex;
  flex-direction: column;
}

/* contenedor del contenido debajo del navbar */
.main-outlet {
  flex: 1 1 auto;
  min-height: 0;   /* 👈 clave para permitir scroll interno en hijos */
  overflow: hidden;/* no hagas scroll aquí; lo hará el hijo (tabla) */
}

componente

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

<!-- ===== Vista en columnas (HEADER FIJO + TABLA CON SCROLL) ===== -->
<div class="vista-consulta">
  <!-- ===== Header fijo: agencias + mensajes + título + filtro ===== -->
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

    <!-- Banner sin data -->
    @if (noDataMessage) {
      <div class="alerta-sin-datos">
        <mat-icon color="warn">error</mat-icon>
        <span>{{ noDataMessage }}</span>
      </div>
    }

    <!-- Encabezado centrado -->
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
          (input)="applyFilterFromInput($event, 'numero')"
          placeholder="Escribe para filtrar"
        />
      </mat-form-field>

      <button mat-button (click)="recargarDatos()">
        <mat-icon>refresh</mat-icon>
        Refrescar
      </button>
    </div>
  </div>

  <!-- ===== SOLO la tabla scrollea ===== -->
  <div class="tabla-scroll">
    <mat-table [dataSource]="dataSource" matSort class="mat-elevation-z8 tabla-tarjetas">
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

      <ng-container matColumnDef="eliminar">
        <mat-header-cell *matHeaderCellDef>Eliminar</mat-header-cell>
        <mat-cell *matCellDef="let tarjetas">
          <button
            mat-icon-button
            type="button"
            aria-label="Eliminar"
            (click)="eliminarTarjeta($event, tarjetas.numero)"
          >
            <mat-icon>delete</mat-icon>
          </button>
        </mat-cell>
      </ng-container>

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
  </div>
</div>

