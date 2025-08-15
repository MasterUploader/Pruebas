Te paso el codigo para que corrijas lo del titulo, es posible que otra sentencia CSS lo este sobreescribiendo:

.agencia-info .fila {
    display: flex;
    /* Alinea los elementos de la fila horizontalmente */
    align-items: center;
    /* Centra los elementos verticalmente en la fila */
    margin-bottom: 10px;
    /* Espacio entre filas para mejor legibilidad */
  }

  .agencia-info .titulo {
    font-weight: bold;
    /* Títulos en negrita para destacar */
    margin-right: 15px;
    /* Espacio después del título */
    margin-left: 15px;
  }

  .agencia-info .codigo-agencia,
  .agencia-info .nombre-agencia {
    margin-right: 15px;
    /* Espacio entre el código y el nombre */
    margin-left: 15px;
  }

  .table-title{
    text-align: center;
    align-content: center;
    justify-content: center;
    align-items: center;
    width: 100%;
  }

  .filtro-tabla{
    padding-top: 20px;
    margin-left: 10px;
  }

  .content-imagen-tarjeta {
    width: 100%;
    height: 400px;
    display: flex;
    align-content: center;
    justify-content: center;
    align-items: center;
  }

  .imagen-tarjeta {
    width: 300px;
    height: 400px;
    object-fit: contain;
  }

  .nombre {
    position: absolute;
    top: 50%;
    right: 100px;
    font-size: 7pt;
    color: white;
  }

  .modal-footer {
    padding: 10px;
    display: flex;
    flex-direction: column;
    justify-content: space-around;
    height: 100px;
  }

  .campo-corto {
    height: 70px;
    width: 70px;

  }

  .table-container{
    width: 98%;
    margin-left: 10px;
    margin-bottom: 100px;
  }

  .matIcon{
    cursor: pointer;
  }

  mat-card-title {
  text-align: center;
  width: 100%;
}


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
        <mat-hint align="end">
          {{ (formularioAgencias.get('codigoAgenciaImprime')?.value?.length || 0) }}/3
        </mat-hint>

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
<div >
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
