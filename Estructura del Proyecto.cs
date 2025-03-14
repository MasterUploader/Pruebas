/* Contenedor principal para colocar los elementos en columna */
.contenedor-vertical {
    display: flex;
    flex-direction: column; /* Poner los elementos en columna */
    align-items: center; /* Centrar horizontalmente */
    justify-content: center; /* Centrar verticalmente */
    width: 100%; /* Ocupar todo el ancho disponible */
    max-width: 400px; /* Limitar el ancho máximo */
    margin: 0 auto; /* Centrar en la página */
    padding: 20px;
    gap: 20px; /* 🔥 Espacio entre elementos */
}

/* Asegurar que cada label y su campo estén alineados correctamente */
.grupo-campo {
    display: flex;
    flex-direction: column; /* Poner el label sobre su input */
    align-items: flex-start; /* Alinear los elementos a la izquierda */
    width: 100%; /* Que ocupen todo el ancho */
    max-width: 300px;
}

/* Ajuste para los labels */
.grupo-campo label {
    font-weight: bold;
    margin-bottom: 5px; /* Espacio entre label y input */
    font-size: 16px;
}

/* Ajuste para los inputs y el DropDownList */
.grupo-campo select,
.grupo-campo input {
    width: 100%;
    padding: 10px; /* Espaciado interno */
    font-size: 14px;
    box-sizing: border-box; /* Evitar que el padding altere el tamaño */
}

/* Ajuste para el botón */
.contenedor-vertical button, 
.contenedor-vertical input[type="submit"] {
    padding: 12px 18px;
    font-size: 16px;
    cursor: pointer;
    width: 100%; /* Hacer que el botón ocupe todo el ancho */
    max-width: 300px;
}
