/* Tema Material */
@import "../node_modules/@angular/material/prebuilt-themes/deeppurple-amber.css";

/* Caja consistente */
* { box-sizing: border-box; }

/* El documento no scrollea: el scroll vivirá en la tabla */
html, body {
  height: 100%;
  margin: 0;
  padding: 0;
}
body { overflow: hidden; }

/* Shell de la app: navbar arriba, contenido flexible, footer abajo */
app-root {
  height: 100%;
  display: flex;
  flex-direction: column;
}

/* Contenido entre navbar y footer (no hace scroll; lo hace el hijo) */
.main-outlet {
  flex: 1 1 auto;
  min-height: 0;   /* ✅ permite scroll interno en hijos */
  overflow: hidden;
}





<app-navbar></app-navbar>

<!-- Área de contenido que hereda el layout flex -->
<div class="main-outlet">
  <router-outlet></router-outlet>
</div>

<app-footer></app-footer>





/* El host del componente ocupa todo el alto disponible del .main-outlet */
:host {
  display: block;
  height: 100%;
}

/* ===== Layout: header fijo + área con scroll ===== */
.vista-consulta {
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;         /* ✅ imprescindible para scroll interno */
}

/* Header (agencias + mensajes + título + filtro) siempre visible */
.panel-fijo {
  flex: 0 0 auto;
  padding: 12px 16px 8px;
  background: #fafafa;
  border-bottom: 1px solid #e0e0e0;
}

/* SOLO aquí habrá scroll vertical (todas las filas de la tabla) */
.tabla-scroll {
  flex: 1 1 auto;
  min-height: 0;         /* ✅ clave en contenedores flex */
  overflow-y: auto;      /* ✅ la única barra de scroll */
  overflow-x: auto;
  padding: 8px 16px;
  background: #fff;
}

/* Tabla */
.tabla-tarjetas { width: 100%; }

/* Header sticky dentro del área con scroll */
.tabla-tarjetas .mat-header-row {
  position: sticky;
  top: 0;
  z-index: 2;
  background: #fff;
  box-shadow: 0 1px 0 rgba(0,0,0,.06);
}

/* Celdas compactas */
.tabla-tarjetas .mat-mdc-cell,
.tabla-tarjetas .mat-mdc-header-cell {
  padding: 8px 12px;
  white-space: nowrap;   /* quita si necesitas multilínea */
}

/* Hover fila */
.tabla-tarjetas tr.mat-mdc-row:hover {
  background: rgba(0,0,0,.03);
}

/* ===== Estilos existentes ajustados ===== */
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

/* Centrar título del card */
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

/* Campo corto */
.campo-corto { height: 70px; width: 70px; }



<div class="vista-consulta">
  <div class="panel-fijo">
    <!-- Agencias + errores + título + filtro -->
  </div>

  <div class="tabla-scroll">
    <mat-table class="tabla-tarjetas" ...> … </mat-table>
  </div>
</div>

