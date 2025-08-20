<mat-toolbar class="navBar" color="primary">
  <mat-toolbar-row class="info-grid">
    <!-- Col 1: vacío (equilibra visualmente la grilla) -->
    <div class="slot-left"></div>

    <!-- Col 2: título SIEMPRE centrado -->
    <span class="title">Servicio Impresión Tarjetas Débito</span>

    <!-- Col 3: usuario + menú (se trunca si es largo) -->
    @if (userName) {
      <div class="slot-right">
        <button
          mat-button
          [matMenuTriggerFor]="userMenu"
          class="user-btn"
          aria-label="Menú de usuario"
          title="{{ userName }}">
          {{ userName }}
        </button>

        <mat-menu #userMenu="matMenu" class="user-menu">
          <button mat-menu-item (click)="logout()" class="logout-item">
            <mat-icon>logout</mat-icon>
            <span>Logout</span>
          </button>
        </mat-menu>
      </div>
    }
  </mat-toolbar-row>
</mat-toolbar>


/* Fondo general app */
body {
  background-color: rgb(241, 239, 239);
  margin: 0;
  height: 100vh;
}

/* Altura cómoda Material (evita problemas con el menú) */
.navBar {
  width: 100%;
  height: 56px;                 /* 56 desktop / 48 mobile */
  background-color: #bd0909 !important;
  color: #fff;
  padding: 0 8px;
}

/* 3 columnas: [izq vacía] [título centrado] [usuario] */
.info-grid {
  width: 100%;
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
}

/* Columna izquierda vacía solo para balancear */
.slot-left {
  justify-self: start;
}

/* Título SIEMPRE centrado independientemente del ancho del usuario */
.title {
  justify-self: center;
  font-size: 1.2rem;
  letter-spacing: 1.2px;
  text-align: center;
  white-space: nowrap;
}

/* Columna derecha: botón usuario alineado al extremo derecho */
.slot-right {
  justify-self: end;
  min-width: 0;                 /* permite truncar dentro */
}

/* Botón usuario: truncado si el nombre es largo */
.user-btn {
  max-width: 240px;             /* ajusta a tu gusto */
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  color: #fff !important;
}

/* Estilos del item de logout dentro del menú */
.logout-item {
  font-size: 0.9rem;
}

/* --- Quitar scrollbar innecesario del mat-menu --- */
/* PONER ESTO EN styles.css GLOBAL o usa ::ng-deep aquí */
.user-menu .mat-mdc-menu-panel {
  max-height: none;             /* evita overflow:auto por altura limitada */
  overflow: visible;            /* sin barra cuando no hace falta */
}

/* Hover sutil para botones en toolbar */
.user-btn:hover,
.logout-item:hover {
  background-color: rgba(255, 255, 255, 0.12);
}

/* Responsivo */
@media (max-width: 768px) {
  .navBar { height: 48px; }
  .title { font-size: 1rem; }
  .user-btn { max-width: 160px; }
}

:host ::ng-deep .user-menu .mat-mdc-menu-panel { max-height:none; overflow:visible; }
