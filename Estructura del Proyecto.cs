<!--
  Overlay de CARGANDO:
  - Se muestra cuando isLoading = true (para las 3 formas de consulta).
  - Usa <mat-progress-spinner>.
-->
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

<!--
  FORM de agencias
  - Validación: requerido + solo dígitos + máx. 3.
  - Hints "x/3".
  - Handlers (keydown/paste/input) para bloquear letras y limitar a 3.
  - Enter ejecuta la búsqueda (dispara overlay de carga).
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
        <!-- Hint: muestra longitud actual sobre 3 -->
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
        <!-- Hint: muestra longitud actual sobre 3 -->
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

<!-- Banner informativo adicional cuando no hay data (además del snackbar) -->
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

<!--
  TABLA
  - Columna "Eliminar": se usa <button mat-icon-button> (accesible nativamente).
    * Esto elimina el warning Sonar S6819 (no usar role="button" en <mat-icon>).
  - Fila <mat-row>:
    * tabindex="0" la hace focuseable.
    * (keydown.enter)/(keydown.space) abren el modal con teclado.
    * aria-label describe la acción para lectores de pantalla.
-->
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

    <!-- ✅ Eliminar con botón real accesible -->
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
