Tengo este codigo

public bool GuardarRegistroEnAs400(string codcco, string estado, string nombreArchivo, string rutaServidor)
{
    try
    {
        // Abrir conexión a AS400 desde la librería RestUtilities.Connections
        _as400.Open();

        if (_as400.IsConnected)
        {
            // Obtener comando para ejecutar SQL
            using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);

            // Obtener nuevo ID para el video
            int codVideo = GetUltimoId(command);

            // Obtener secuencia correlativa
            int sec = GetSecuencia(command, codcco);

            // Construir la ruta en el formato que espera AS400: C:\Vid{codcco}Marq\
            string rutaAs400 = $"C:\\Vid{codcco}Marq\\";

            // Construir query de inserción SQL
            var query = new InsertQueryBuilder("MANTVIDEO", "BCAH96DTA")
            .Values(
                    ("CODCCO", codcco),
                    ("CODVIDEO", codVideo),
                    ("RUTA", rutaAs400),
                    ("NOMBRE", nombreArchivo),
                    ("ESTADO", estado),
                    ("SEQ", sec)
                    )
            .Build();

            command.CommandText = query.Sql;

            // Ejecutar inserción
            int result = command.ExecuteNonQuery();

            return result > 0;

        }
        return false;
    }
    catch (Exception ex)
    {
        // Loguear o manejar el error según tu sistema (puedes usar RestUtilities.Logging aquí si lo deseas)
        // Por ahora, devolvemos false controladamente
        return false;
    }
    finally
    {
        // Cerrar conexión correctamente
        _as400.Close();
    }
}



Y tengo este error

{"SQL0313: Número de variables del lenguaje principal no válido.\r\nCausa . . . . . :   El número de variables del lenguaje principal o de entradas en una SQLDA o área de descriptor especificados en una sentencia EXECUTE u OPEN no es igual que el número de marcadores de parámetro especificado en la sentencia SQL preparada S000001. Si el nombre de sentencia es *N, el número de variables del lenguaje principal o de entradas en una SQLDA o área de descriptor se especificó en una sentencia OPEN, y no es igual que el número de variables del lenguaje principal especificado en la sentencia DECLARE CURSOR para el cursor *N. Recuperación . .:   Cambie el número de variables del lenguaje principal especificado en la cláusula USING, o el número de entradas en la SQLDA o área de descriptor para igualar el número de marcadores de parámetro en la sentencia SQL preparada, o el número de variables del lenguaje principal en la sentencia DECLARE CURSOR. Vuelva a precompilar el programa."}
