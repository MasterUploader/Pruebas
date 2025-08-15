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

    <!-- Campo Nombre: matcher en el INPUT -->
    <mat-form-field appearance="fill" class="nombre-input">
      <mat-label>Nombre:</mat-label>
      <input
        placeholder="Nombre en Tarjeta"
        matInput
        [(ngModel)]="tarjeta.nombre"
        (input)="validarEntrada($event)"
        (keypress)="prevenirNumeroCaracteres($event)"
        maxlength="26"
        required
        [errorStateMatcher]="nombreErrorMatcher">
      <!-- Cuando NO hay error, mostramos el contador estándar -->
      <mat-hint align="end" *ngIf="!nombreError">{{nombreCharsCount}}/{{nombreMaxLength}}</mat-hint>
      <!-- Cuando hay error, Material oculta el hint; mostramos el error -->
      <mat-error *ngIf="nombreError">{{ nombreError }}</mat-error>
    </mat-form-field>

    <!-- Contador externo: se muestra SOLO cuando hay error, para no perder la referencia 13/26 -->
    <div class="contador-externo" *ngIf="nombreError">
      {{nombreCharsCount}}/{{nombreMaxLength}}
    </div>

    <!-- Botón siempre habilitado; validación se hace al presionar -->
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
.nombres, .apellidos, .cuenta {
  position: absolute;
  right: 6%;            /* margen desde el borde derecho de la imagen */
  max-width: 82%;       /* área útil horizontal (evita pegarse al borde izquierdo) */
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  text-align: right;    /* “nace” a la derecha y se corre a la izquierda */
}

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

/* ---------- Contador externo cuando hay error ---------- */
.contador-externo {
  width: 100%;
  text-align: right;
  margin-top: -8px;       /* acércalo al form-field; ajusta si lo prefieres */
  margin-bottom: 8px;
  font-size: 12px;
  color: rgba(0,0,0,0.6); /* mismo tono del mat-hint */
}
