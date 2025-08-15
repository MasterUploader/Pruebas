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
   Ajustado a porcentajes y centrado como Diseño 1.
   Equivalentes a ~170px (≈53%), ~185px (≈58%) y ~210px (≈65%) de la altura de 321px.
*/
.nombres {
  position: absolute;
  top: 53%;
  left: 50%;
  transform: translateX(-50%);
  font-size: 6pt;
  color: black;
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
  transform: translateX(-50%);
  font-size: 6pt;
  color: black;
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
  transform: translateX(-50%);
  font-size: 7pt;
  text-align: center;
  max-width: 80%;
  color: white; /* se mantiene como en tu versión previa */
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
