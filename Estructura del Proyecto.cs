El CSS lo deje así porque así va quedando de la forma más cercana a lo que busco.

   /* ====== DISEÑO 2 (dos filas) ======
   Ajustado a porcentajes y centrado como Diseño 1.
   Equivalentes a ~170px (≈53%), ~185px (≈58%) y ~210px (≈65%) de la altura de 321px.
*/
.nombres {
  position: absolute;
  top: 53%;
  left: 50%;
  transform: translateX(0%);
  font-size: 6pt;
  color: white;
  text-align: center;
  max-width: 90%;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.apellidos {
  position: absolute;
  top: 58%;
  left: 50%;
  transform: translateX(0%);
  font-size: 6pt;
  color: white;
  text-align: center;
  max-width: 90%;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cuenta{
  position: absolute;
  top: 65%;
  left: 50%;
  transform: translateX(40%);
  font-size: 7pt;
  text-align: center;
  max-width: 80%;
  color: white; /* se mantiene como en tu versión previa */
}



Pero aun falta para llegar a lo que quiero, cada fila del diseño 2 tiene un maximo de 16 caracteres, necesito que el texto se empiece a posicionar a la derecha y se vaya corriendo a la izquierda, cuando hayan 2 filas que el incio de cada fila este alineado.
