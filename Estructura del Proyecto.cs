/* El documento no scrollea */
html, body {
  height: 100%;
  margin: 0;
  padding: 0;
}
body {
  overflow: hidden; /* 👈 sin scroll global */
}

/* Estructura del app: toolbar arriba + contenido flexible */
app-root {
  height: 100%;
  display: flex;
  flex-direction: column;
}

/* contenedor del contenido debajo del navbar */
.main-outlet {
  flex: 1 1 auto;
  min-height: 0;   /* 👈 clave para permitir scroll interno en hijos */
  overflow: hidden;/* no hagas scroll aquí; lo hará el hijo (tabla) */
}

<!-- app.component.html -->
<app-navbar></app-navbar>

<div class="main-outlet">
  <router-outlet></router-outlet>
</div>

/* contenedor de toda la vista del componente */
.vista-consulta {
  height: 100%;
  display: flex;
  flex-direction: column;
  min-height: 0;  /* 👈 importante para que el hijo pueda scrollear */
}

/* header fijo (agencias, mensajes, título, filtros) */
.panel-fijo {
  flex: 0 0 auto;
  padding: 12px 16px 8px;
  background: #fafafa;
  border-bottom: 1px solid #e0e0e0;
}

/* SOLO aquí habrá scroll vertical */
.tabla-scroll {
  flex: 1 1 auto;
  min-height: 0;      /* 👈 clave en layouts flex */
  overflow-y: auto;   /* 👈 la única barra de scroll */
  overflow-x: auto;
  padding: 8px 16px;
  background: #fff;
}

/* header de la mat-table pegado arriba dentro del área con scroll */
.tabla-tarjetas .mat-header-row {
  position: sticky;
  top: 0;
  z-index: 2;
  background: #fff;
  box-shadow: 0 1px 0 rgba(0,0,0,.06);
}

