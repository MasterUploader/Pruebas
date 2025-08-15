<h1 mat-dialog-title> Detalle Tarjeta</h1>
<div mat-dialog-content id="contenidoImprimir">
  <div class="contenedor">
    <!-- La imagen es el contenedor de referencia (position: relative) -->
    <div class="content-imagen-tarjeta">
      <img
        [src]="disenoSeleccionado === 'unaFila' ? '/assets/TarjetaDiseño2.png' : '/assets/Tarjeta3.PNG'"
        alt="imagen tarjeta"
        class="imagen-tarjeta no-imprimir">

      <!-- ==================== DISEÑO 1 (una fila) ==================== -->
      <ng-container *ngIf="disenoSeleccionado === 'unaFila'">
        <div class="nombres-una-fila">
          <b>{{tarjeta.nombre}}</b>
        </div>
        <div class="cuenta-una-fila">
          <b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b>
        </div>
      </ng-container>

      <!-- ==================== DISEÑO 2 (dos filas) ==================== -->
      <!-- AHORA las dos filas se posicionan respecto a la IMAGEN -->
      <ng-container *ngIf="disenoSeleccionado === 'dosFilas'">
        <div class="nombres"><b>{{nombres}}</b></div>
        <div class="apellidos"><b>{{apellidos}}</b></div>
        <div class="cuenta"><b>{{tarjeta.numeroCuenta | maskAccountNumber}}</b></div>
      </ng-container>
    </div>
  </div>

  <div mat-dialog-actions class="action-buttons">
    <!-- Selector de Diseño -->
    <mat-form-field appearance="fill" class="diseño-input">
      <mat-label>Diseño</mat-label>
      <mat-select [(value)]="disenoSeleccionado" (selectionChange)="cambiarDiseno()">
        <mat-option value="unaFila">Diseño 1</mat-option>
        <mat-option value="dosFilas">Diseño 2</mat-option>
      </mat-select>
    </mat-form-field>

    <!-- Input nombre en tarjeta (26 caracteres siempre) -->
    <mat-form-field appearance="fill" class="nombre-input">
      <mat-label>Nombre:</mat-label>
      <input
        placeholder="Nombre en Tarjeta"
        matInput
        [(ngModel)]="tarjeta.nombre"
        (input)="validarEntrada($event)"
        (keypress)="prevenirNumeroCaracteres($event)"
        maxlength="26">
      <mat-hint align="end">{{nombreCharsCount}}/{{nombreMaxLength}}</mat-hint>
      <mat-error *ngIf="nombreError">{{nombreError}}</mat-error>
    </mat-form-field>

    <!-- Botón siempre habilitado; la validación es interna en imprimir() -->
    <button mat-button class="imprimir-btn" (click)="imprimir(tarjeta)">Imprimir</button>
    <span class="spacer"></span>
    <button mat-button class="cerrar-btn" (click)="cerrarModal()" [mat-dialog-close]="true">Cerrar</button>
  </div>
</div>


/* ---------- Layout general ---------- */
.modal { display:none; position:fixed; z-index:1; inset:0; overflow:auto; background-position:center; }

.modal-content {
  background:#fefefe; margin:15% auto; padding:20px; border:1px solid #888;
  width:400px; height:600px; background-repeat:no-repeat; background-size:cover;
}

.contenedor{
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
}

/* La imagen de la tarjeta es el marco de referencia */
.content-imagen-tarjeta {
  position: relative;                 /* clave: los textos se posicionan vs esta caja */
  width: 207.87404194px;
  height: 321.25988299px;
  display: block;
}

@media print { .no-imprimir{ display:none; } }

.imagen-tarjeta { width:100%; height:100%; object-fit:contain; display:block; }

/* ---------- DISEÑO 1 (una fila) centrado, como ya tenías ---------- */
.nombres-una-fila {
  position: absolute;
  top: 60%;
  left: 50%;
  transform: translateX(-50%);
  font-size: 6pt;
  color: white;
  text-align: center;
  max-width: 90%;
  white-space: nowrap; overflow:hidden; text-overflow:ellipsis;
}

.cuenta-una-fila{
  position: absolute;
  top: 67%;
  left: 50%;
  transform: translateX(-50%);
  font-size: 7pt;
  text-align: center;
  max-width: 80%;
  color: white;
  white-space: nowrap; overflow:hidden; text-overflow:ellipsis;
}

/* ---------- DISEÑO 2 (dos filas) ---------- */
/*
  Requisitos:
  - Máximo 16 caracteres por fila (ya lo cortas en TS).
  - El texto “nace” en el borde derecho y crece hacia la izquierda.
  - Ambas filas comienzan en la MISMA columna (alineado a la derecha).
  - NO pegarse al borde derecho de la tarjeta (dejamos margen).
  Estrategia:
  - Caja de texto anclada al borde derecho de la imagen con 'right'.
  - Se usa 'text-align: right' y 'max-width' en % de la imagen.
*/
.nombres, .apellidos, .cuenta {
  position: absolute;
  right: 6%;            /* margen desde el borde derecho de la imagen */
  max-width: 82%;       /* área útil horizontal (evita pegarse al borde izquierdo) */
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  text-align: right;    /* ancla a la derecha: “nace” a la derecha y se corre a la izquierda */
}

/* Alturas en % para respetar la proporción de la tarjeta */
.nombres   { top: 53%; font-size: 6pt; color: white; }
.apellidos { top: 58%; font-size: 6pt; color: white; }
.cuenta    { top: 65%; font-size: 7pt; color: white; }

/* ---------- Footer y acciones ---------- */
.modal-footer { padding:10px; display:flex; flex-direction:column; justify-content:space-around; height:100px; }

.mat-dialog-actions { align-items:center; justify-content:space-between; display:flex; flex-wrap:wrap; }

.action-buttons .flex-container { display:flex; justify-content:space-between; align-items:center; width:100%; }

.nombre-input { flex-grow:1; margin-right:20px; width:100%; text-transform:uppercase; }

.spacer { flex:1; }

.imprimir-btn { background:#4CAF50; color:#fff; } .imprimir-btn:hover { background:#45a049; }
.cerrar-btn { background:#f44336; color:#fff; } .cerrar-btn:hover { background:#da190b; }
