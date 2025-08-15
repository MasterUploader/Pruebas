/* --- CENTRAR EL TÍTULO DEL CARD --- */
/* Aumentamos especificidad apuntando a la jerarquía real que usa Angular Material */
.encabezado mat-card .mat-card-header {
  /* Por si hay avatar o acciones laterales, centramos el contenido */
  justify-content: center;
}

.encabezado mat-card .mat-card-header .mat-card-header-text {
  /* Este es el contenedor que envuelve al título y subtítulo */
  width: 100%;
  display: flex;
  justify-content: center; /* centra horizontalmente el título */
}

.encabezado mat-card .mat-card-title {
  width: 100%;
  text-align: center;
  margin: 0;              /* evita márgenes que desplacen */
  font-weight: 600;       /* opcional para destacar */
  /* Si en tu hoja global hay algo muy fuerte, descomenta la siguiente línea: */
  /* text-align: center !important; */
}

/* --- (tu CSS existente) --- */
.agencia-info .fila {
  display: flex;
  align-items: center;
  margin-bottom: 10px;
}
.agencia-info .titulo { font-weight: bold; margin: 0 15px; }
.agencia-info .codigo-agencia,
.agencia-info .nombre-agencia { margin: 0 15px; }
.table-title { text-align: center; align-content: center; justify-content: center; align-items: center; width: 100%; }
.filtro-tabla { padding-top: 20px; margin-left: 10px; }
.content-imagen-tarjeta { width: 100%; height: 400px; display: flex; align-content: center; justify-content: center; align-items: center; }
.imagen-tarjeta { width: 300px; height: 400px; object-fit: contain; }
.nombre { position: absolute; top: 50%; right: 100px; font-size: 7pt; color: white; }
.modal-footer { padding: 10px; display: flex; flex-direction: column; justify-content: space-around; height: 100px; }
.campo-corto { height: 70px; width: 70px; }
.table-container { width: 98%; margin-left: 10px; margin-bottom: 100px; }
.matIcon { cursor: pointer; }

/* Si preferías centrar TODOS los títulos del componente, podrías dejar también:
mat-card-title { text-align: center; width: 100%; }
*/

<!-- Encabezado -->
<div class="encabezado">
  <mat-card>
    <mat-card-header>
      <mat-card-title>Detalle Tarjetas Por Imprimir</mat-card-title>
    </mat-card-header>
  </mat-card>
</div>
