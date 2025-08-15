/* ====== Contenedor base ====== */
.contenedor{
  position: relative;           /* <-- Anchor para el overlay */
  width: 100%;
  max-width: 520px;             /* ajusta si tu tarjeta es más ancha */
  margin: 0 auto;
}

/* ====== Imagen de la tarjeta ====== */
.content-imagen-tarjeta{
  position: relative;
  z-index: 1;                   /* debajo del overlay */
}

.imagen-tarjeta{
  display: block;
  width: 100%;
  height: auto;
  object-fit: cover;
}

/* ====== Overlay de texto (nombres / apellidos / cuenta) ====== */
.nombre-completo{
  position: absolute;
  left: 10%;                    /* ajusta según tu arte */
  right: 10%;
  bottom: 18%;                  /* coloca el bloque cerca de la parte baja */
  z-index: 2;                   /* encima de la imagen */
  display: flex;
  flex-direction: column;
  gap: 2px;
  pointer-events: none;         /* evita robar clics al mover el mouse */
}

/* Estilos del texto (ajusta tamaños a tu gusto/arte) */
.nombre-completo .nombres,
.nombre-completo .apellidos{
  font-weight: 700;
  font-size: 14px;
  line-height: 1.1;
  letter-spacing: 0.8px;
  color: #000;                  /* si tu arte es oscuro, cambia a #fff */
  text-transform: uppercase;
  /* Para mejorar contraste sobre fondos ocupados */
  text-shadow: 0 0 1px rgba(255,255,255,.6);
}

.nombre-completo .cuenta{
  margin-top: 8px;
  font-weight: 600;
  font-size: 12px;
  letter-spacing: 1px;
  color: #000;                  /* cambia a #fff si el fondo es oscuro */
  text-shadow: 0 0 1px rgba(255,255,255,.6);
}

/* ====== Barra de acciones / campo ====== */
.action-buttons{
  display: flex;
  align-items: center;
  gap: 12px;
  margin-top: 12px;
}
.action-buttons .spacer{ flex: 1 1 auto; }
.nombre-input{ width: 100%; }

/* ====== Impresión ====== */
@media print{
  /* Oculta la imagen para imprimir solo el texto en la tarjeta física */
  .no-imprimir{ display: none !important; }

  /* Quita márgenes/sombras del diálogo al imprimir */
  .mat-dialog-container{
    box-shadow: none !important;
    padding: 0 !important;
    border: 0 !important;
    background: transparent !important;
  }

  /* Asegura buena reproducción de color del texto */
  body{
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }

  /* Mantén el overlay en su sitio al imprimir */
  .contenedor{ position: relative !important; }
  .nombre-completo{ position: absolute !important; }
}
