:host ::ng-deep .encabezado mat-card .mat-card-header {
  justify-content: center;
}

:host ::ng-deep .encabezado .mat-card-header-text {
  width: 100%;
  display: flex;
  justify-content: center;
}

:host ::ng-deep .encabezado .mat-card-title {
  width: 100%;
  text-align: center !important; /* por si hay reglas previas */
  margin: 0;
}

<!-- Encabezado -->
<div class="encabezado">
  <mat-card>
    <mat-card-header>
      <mat-card-title>Detalle Tarjetas Por Imprimir</mat-card-title>
    </mat-card-header>
  </mat-card>
</div>
