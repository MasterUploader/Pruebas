/* Modal */
.modal {
  display: none;
  position: fixed;
  z-index: 1;
  left: 0;
  top: 0;
  width: 100%;
  height: 100%;
  overflow: auto;
  background-position: center;
}

/* Contenido del modal */
.modal-content {
  background-color: #fefefe;
  margin: 15% auto;
  padding: 20px;
  border: 1px solid #888;
  width: 400px;
  height: 600px;
  background-size: 87404194px 321.25988299px;
  background-repeat: no-repeat;
  background-size: cover;
}

.contenedor{
  position: relative;
  display: flex;
  justify-content: center;
  align-items: center;
}

@media print {
  .no-imprimir{
    display: none;
  }
}

.content-imagen-tarjeta {
  width: 207.87404194px;
  height: 321.25988299px;
  display: flex;
  align-content: center;
  justify-content: center;
  align-items: center;
  position: relative;
}

.imagen-tarjeta {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

/* ====== DISEÑO 1 (una fila) ====== */
.nombres-una-fila {
  position: absolute;
  top: 60%;
  left: 50%;
  font-size: 6pt;
  color: white;
  text-align: center;
  max-width: 90%;
  transform: translate(-50%);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.cuenta-una-fila{
  position: absolute;
  top: 67%;
  left: 50%;
  transform: translate(-50%);
  font-size: 7pt;
  text-align: center;
  max-width: 80%;
  color: white;
}

/* ====== DISEÑO 2 (dos filas) ======
   - El texto se ancla al borde derecho del área útil (right aligned),
     por lo que “nace” a la derecha y se corre hacia la izquierda.
   - Ambas filas comparten la misma caja (mismo left + width), por eso
     el “inicio” (borde derecho) queda alineado en ambas.
   - Los % de top replican aprox. 170px, 185px y 210px de alto de la tarjeta.
*/
.nombres,
.apellidos,
.cuenta {
  position: absolute;
  left: 12%;          /* margen izquierdo fijo del área útil */
  width: 76%;         /* ancho del área útil (no tocar borde derecho) */
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.nombres {
  top: 53%;
  font-size: 6pt;
  color: white;       /* como dejaste en tu versión */
  text-align: right;  /* ancla a la derecha: crece hacia la izquierda */
}

.apellidos {
  top: 58%;
  font-size: 6pt;
  color: white;
  text-align: right;  /* misma columna de inicio (borde derecho) */
}

.cuenta{
  top: 65%;
  font-size: 7pt;
  color: white;
  text-align: right;  /* si deseas que número también “nazca” a la derecha */
}

/* Footer y acciones */
.modal-footer {
  padding: 10px;
  display: flex;
  flex-direction: column;
  justify-content: space-around;
  height: 100px;
}

.mat-dialog-actions {
  align-items: center;
  justify-content: space-between;
  display: flex;
  flex-wrap: wrap;
}

.action-buttons .flex-container {
  display: flex;
  justify-content: space-between;
  align-items: center;
  width: 100%;
}

.nombre-input {
  flex-grow: 1;
  margin-right: 20px;
  width: 100%;
  text-transform: uppercase;
}

.spacer {
  flex: 1;
}

.imprimir-btn {
  background-color: #4CAF50;
  color: white;
}

.imprimir-btn:hover {
  background-color: #45a049;
}

.cerrar-btn {
  background-color: #f44336;
  color: white;
}

.cerrar-btn:hover {
  background-color: #da190b;
}
