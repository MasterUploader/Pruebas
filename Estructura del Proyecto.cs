<!-- idle-warning.component.html (reemplazar todo el contenido) -->
<div class="idle-wrapper">
  <div class="icon-row">
    <mat-icon aria-hidden="true">hourglass_empty</mat-icon>
  </div>

  <!-- Título accesible gestionado por MatDialog -->
  <h2 mat-dialog-title id="idle-title" class="title">¿Sigues ahí?</h2>

  <!-- Contenido accesible -->
  <div mat-dialog-content id="idle-desc">
    <p class="subtitle">Por seguridad, cerraremos tu sesión si no respondes.</p>

    <div class="countdown" aria-live="polite">
      Tu sesión expirará en <strong>{{ seconds }}</strong> segundos.
    </div>

    <mat-progress-bar
      [value]="progressPercent"
      mode="determinate"
      aria-label="Tiempo restante de sesión">
    </mat-progress-bar>
  </div>

  <!-- Acciones -->
  <div mat-dialog-actions class="actions">
    <button
      mat-raised-button
      color="primary"
      class="btn-continue"
      (click)="onContinue()"
      cdkFocusInitial
      aria-label="Seguir conectado">
      <mat-icon aria-hidden="true">check_circle</mat-icon>
      Seguir conectado
    </button>

    <button
      mat-stroked-button
      color="warn"
      class="btn-logout"
      (click)="onLogout()"
      aria-label="Cerrar sesión ahora">
      <mat-icon aria-hidden="true">logout</mat-icon>
      Cerrar sesión
    </button>
  </div>
</div>
