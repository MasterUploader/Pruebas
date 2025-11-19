/* === Definir el mixin a NIVEL RAÍZ (exportable para otros módulos) === */
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

/* === Reglas del componente dentro de la capa === */
@layer components {
  .btn { @include btn-base; }

  .btn:disabled {
    opacity: .6;
    cursor: not-allowed;
  }
}




/* Importa el módulo y usa su mixin con namespace */
@use '../components/button' as button;

@layer legacy {
  /* Clase legacy que reutiliza el mixin del botón moderno */
  .btn-imprimir { @include button.btn-base; }

  .contenedor { padding: var(--space-6); }
  .titulo { font-weight: 600; }
}



