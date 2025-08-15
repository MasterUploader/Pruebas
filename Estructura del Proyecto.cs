<!--
  Vista del modal de impresión de tarjeta.
  - Siempre imprime en 2 filas.
  - El input usa Reactive Forms; el botón siempre está habilitado,
    pero la acción "imprimir" valida y frena si es inválido.
  - Recuerda: si usas | maskAccountNumber aquí, el componente standalone del modal
    debe importar MaskAccountNumberPipe.
-->

<h1 mat-dialog-title>Detalle Tarjeta</h1>

<form [formGroup]="form">
  <div mat-dialog-content id="contenidoImprimir">
    <div class="contenedor">

      <!-- Imagen base de la tarjeta (diseño fijo de 2 filas) -->
      <div class="content-imagen-tarjeta">
        <img src="/assets/Tarjeta3.PNG" alt="tarjeta" class="imagen-tarjeta no-imprimir">
      </div>

      <!-- Overlay: dos líneas de nombre y número de cuenta enmascarado -->
      <div class="nombre-completo">
        <div class="nombres">
          <b>{{ nombres }}</b>
        </div>
        <div class="apellidos">
          <b>{{ apellidos }}</b>
        </div>

        <!-- Número de Cuenta enmascarado con MaskAccountNumberPipe -->
        <div class="cuenta">
          <b>{{ tarjeta.numeroCuenta | maskAccountNumber }}</b>
        </div>
      </div>
    </div>

    <!-- Acciones y campo de entrada -->
    <div mat-dialog-actions class="action-buttons">
      <!-- Campo de nombre (reactivo) -->
      <mat-form-field appearance="fill" class="nombre-input">
        <mat-label>Nombre:</mat-label>
        <input
          matInput
          placeholder="NOMBRE EN TARJETA"
          formControlName="nombre"
          (input)="form.get('nombre')?.setValue((form.get('nombre')?.value || '').toUpperCase(), { emitEvent: true })"
          maxlength="40"
          autocomplete="off"
          cdkFocusInitial
        />

        <!-- Contador de caracteres -->
        <mat-hint align="end">{{ (form.get('nombre')?.value?.length || 0) }}/40</mat-hint>

        <!-- Único mensaje de error, priorizado en TS -->
        @if (nombreError) {
          <mat-error>{{ nombreError }}</mat-error>
        }
      </mat-form-field>

      <!-- Botones de acción -->
      <button mat-button class="imprimir-btn" (click)="imprimir(tarjeta)">Imprimir</button>
      <span class="spacer"></span>
      <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">Cerrar</button>
    </div>
  </div>
</form>
