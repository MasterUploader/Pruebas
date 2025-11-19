@layer components {
  @mixin btn-base {
    display: inline-flex;
    gap: .5rem;
    padding: .375rem .75rem;
    border-radius: .375rem;
    background: var(--primary-500);
    color: #fff;
    border: 1px solid transparent;
    cursor: pointer;
  }

  .btn { @include btn-base; }

  .btn:disabled { opacity: .6; cursor: not-allowed; }
}



/* Importa el módulo y consume el mixin */
@use '../components/button' as button;

@layer legacy {
  .btn-imprimir { @include button.btn-base; } /* sin @extend */
  .contenedor { padding: var(--space-6); }
  .titulo { font-weight: 600; }
}
