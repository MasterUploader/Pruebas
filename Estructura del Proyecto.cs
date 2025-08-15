/* ====== DISEÑO 2 (dos filas) ======
   Alineados a la izquierda en la misma columna, con margen fijo desde el borde de la tarjeta
   y ancho limitado para no chocar con el borde derecho.
*/
.nombres {
  position: absolute;
  top: 53%;
  left: 12%; /* margen izquierdo respecto al borde de la tarjeta */
  font-size: 6pt;
  color: black;
  text-align: left;
  max-width: 76%; /* ancho disponible antes de llegar al borde derecho */
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.apellidos {
  position: absolute;
  top: 58%;
  left: 12%; /* mismo margen que .nombres para alinear columnas */
  font-size: 6pt;
  color: black;
  text-align: left;
  max-width: 76%; /* igual que nombres */
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cuenta {
  position: absolute;
  top: 65%;
  left: 12%; /* mismo alineado */
  font-size: 7pt;
  text-align: left;
  max-width: 76%;
  color: white;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
