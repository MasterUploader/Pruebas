/* Asegurar que todo esté alineado verticalmente */
.contenedor-vertical {
    display: flex;
    flex-direction: column; /* Poner los elementos en columna */
    align-items: center; /* Centrar horizontalmente */
    justify-content: center; /* Centrar verticalmente */
    width: 100%; /* Ocupar todo el ancho disponible */
    max-width: 400px; /* Limitar el ancho máximo */
    margin: 0 auto; /* Centrar en la página */
    padding: 20px;
    gap: 15px; /* Espaciado entre elementos */
}

/* Ajuste para los labels */
.contenedor-vertical label {
    font-weight: bold;
    text-align: center;
    display: block; /* Asegurar que los labels estén en líneas separadas */
}

/* Asegurar que el DropDownList y los inputs ocupen todo el ancho */
.contenedor-vertical select,
.contenedor-vertical input {
    width: 100%;
    max-width: 300px;
    padding: 8px;
}

/* Ajuste para el botón */
.contenedor-vertical button, 
.contenedor-vertical input[type="submit"] {
    padding: 10px 15px;
    font-size: 16px;
    cursor: pointer;
    width: 100%; /* Hacer que el botón ocupe todo el ancho */
    max-width: 300px;
}
