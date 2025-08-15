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
  <!-- Puedes dejar el mat-error si quieres Material-style -->
  <!-- <mat-error *ngIf="nombreError">{{nombreError}}</mat-error> -->
</mat-form-field>

<!-- Mensaje de error bajo el campo (siempre visible cuando hay error) -->
<div class="campo-error" *ngIf="nombreError">{{ nombreError }}</div>


.campo-error {
  margin-top: -10px;   /* acerca el error al campo, ajusta si lo deseas */
  margin-bottom: 8px;
  color: #d32f2f;      /* rojo Material */
  font-size: 12px;
  line-height: 1.2;
}
