/* --- Ancla para el overlay --- */
.contenedor{
  position: relative;          /* el overlay se posiciona respecto a este contenedor */
  display: inline-block;       /* el ancho se ajusta al de la imagen */
}

/* evita huecos por línea base */
.content-imagen-tarjeta,
.imagen-tarjeta{
  display: block;
}

/* --- Overlay general (encima de la imagen) --- */
.nombre-completo{
  position: absolute;
  inset: 0;                    /* top/right/bottom/left: 0 */
  z-index: 2;                  /* por encima de la imagen */
  pointer-events: none;        /* no captura clics del usuario */
}

/* ===== Diseño 1: UNA FILA ===== */
.nombres-una-fila{
  position: absolute;
  /* Ajusta estos % si tu arte cambió */
  left: 59%;
  top: 47%;
  width: 34%;
  font-weight: 700;
  font-size: 14px;
  line-height: 1.1;
  letter-spacing: .8px;
  text-transform: uppercase;
  color: #fff;                 /* cambia a #000 si tu imagen es clara */
  text-shadow: 0 0 2px rgba(0,0,0,.6);
}

.cuenta-una-fila{
  position: absolute;
  left: 60%;
  top: 66%;
  width: 32%;
  font-weight: 600;
  font-size: 12px;
  letter-spacing: 1px;
  color: #fff;
  text-shadow: 0 0 2px rgba(0,0,0,.6);
}

/* ===== Diseño 2: DOS FILAS ===== */
.nombres{
  position: absolute;
  left: 59%;
  top: 43%;
  width: 34%;
  font-weight: 700;
  font-size: 14px;
  line-height: 1.1;
  letter-spacing: .8px;
  text-transform: uppercase;
  color: #fff;
  text-shadow: 0 0 2px rgba(0,0,0,.6);
}

.apellidos{
  position: absolute;
  left: 59%;
  top: 50%;
  width: 34%;
  font-weight: 700;
  font-size: 14px;
  line-height: 1.1;
  letter-spacing: .8px;
  text-transform: uppercase;
  color: #fff;
  text-shadow: 0 0 2px rgba(0,0,0,.6);
}

.cuenta{
  position: absolute;
  left: 60%;
  top: 68%;
  width: 32%;
  font-weight: 600;
  font-size: 12px;
  letter-spacing: 1px;
  color: #fff;
  text-shadow: 0 0 2px rgba(0,0,0,.6);
}

/* Botonera / campo (se mantienen) */
.action-buttons{ display:flex; align-items:center; gap:12px; margin-top:12px; }
.action-buttons .spacer{ flex:1 1 auto; }
.nombre-input{ width:100%; }

/* Impresión: oculta solo la imagen, conserva el overlay */
@media print{
  .no-imprimir{ display:none !important; }
  .contenedor{ position: relative !important; }
  .nombre-completo{ position: absolute !important; }
}
