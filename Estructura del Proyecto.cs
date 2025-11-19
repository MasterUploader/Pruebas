/* Importa el módulo de botones con namespace */
@use './button' as button;

@layer components {
  .pagination { display: flex; gap: .25rem; align-items: center; }

  /* Aplica el mismo estilo base del botón sin usar @extend */
  .page {
    @include button.btn-base();
    padding: .25rem .5rem; /* ajuste propio de la paginación */
  }
}
