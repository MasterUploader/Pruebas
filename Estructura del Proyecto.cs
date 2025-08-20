<!-- =========================
  Vista: HEADER FIJO + TABLA CON SCROLL
  Estructura lista para pegar en consulta-tarjeta.component.html
  (Usa las mismas variables/handlers que ya tienes en el componente)
========================== -->
<div class="vista-consulta">
  <!-- ===== Header fijo: agencias + mensajes + título + filtro ===== -->
  <div class="panel-fijo">
    <!-- ====== Form de Agencias ====== -->
    <form [formGroup]="formularioAgencias" (ngSubmit)="actualizarTabla()">
      <div class="agencia-info">
        <!-- Agencia Imprime -->
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

        <!-- Agencia Apertura -->
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

    <!-- ====== Mensaje sin datos (opcional) ====== -->
    @if (noDataMessage) {
      <div class="alerta-sin-datos">
        <mat-icon color="warn">error</mat-icon>
        <span>{{ noDataMessage }}</span>
      </div>
    }

    <!-- ====== Título centrado ====== -->
    <div class="encabezado">
      <mat-card>
        <mat-card-header>
          <div class="title-wrap">
            <mat-card-title>Detalle Tarjetas Por Imprimir</mat-card-title>
          </div>
        </mat-card-header>
      </mat-card>
    </div>

    <!-- ====== Filtro + botón refrescar ====== -->
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

  <!-- ===== SOLO la tabla tiene scroll ===== -->
  <div class="tabla-scroll">
    <mat-table
      [dataSource]="dataSource"
      matSort
      class="mat-elevation-z8 tabla-tarjetas"
      role="table"
    >
      <!-- Col: No. Tarjeta -->
      <ng-container matColumnDef="numero">
        <mat-header-cell *matHeaderCellDef>No. de Tarjeta</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.numero | maskCardNumber }}</mat-cell>
      </ng-container>

      <!-- Col: Nombre -->
      <ng-container matColumnDef="nombre">
        <mat-header-cell *matHeaderCellDef>Nombre en Tarjeta</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.nombre | uppercase }}</mat-cell>
      </ng-container>

      <!-- Col: Motivo -->
      <ng-container matColumnDef="motivo">
        <mat-header-cell *matHeaderCellDef>Motivo</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.motivo }}</mat-cell>
      </ng-container>

      <!-- Col: Número de Cuenta -->
      <ng-container matColumnDef="numeroCuenta">
        <mat-header-cell *matHeaderCellDef>Número de Cuenta</mat-header-cell>
        <mat-cell *matCellDef="let t">{{ t.numeroCuenta | maskAccountNumber }}</mat-cell>
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

    <!-- (Opcional) Paginador sticky dentro del área con scroll
    <mat-paginator
      [length]="total"
      [pageSize]="pageSize"
      [pageSizeOptions]="[10, 25, 50]"
      (page)="onPage($event)"
      class="paginator-sticky">
    </mat-paginator>
    -->
  </div>
</div>
