/// <summary>
/// Guarda el registro del video en AS400 con la ruta formateada como C:\Vid{codcco}Marq
/// </summary>
/// <param name="codcco">Código de agencia</param>
/// <param name="estado">Estado del video (A/I)</param>
/// <param name="nombreArchivo">Nombre del archivo de video</param>
/// <param name="rutaServidor">Ruta del servidor físico (no se usa en el insert, solo para guardar el archivo)</param>
/// <returns>True si el registro se insertó correctamente, false si falló</returns>
public bool GuardarRegistroEnAs400(string codcco, string estado, string nombreArchivo, string rutaServidor)
{
    try
    {
        // Abrir conexión a AS400 desde la librería RestUtilities.Connections
        _as400.Open();

        // Obtener comando para ejecutar SQL
        using var command = _as400.GetDbCommand();

        if (command.Connection.State != System.Data.ConnectionState.Open)
            command.Connection.Open();

        // Obtener nuevo ID para el video
        int codVideo = GetUltimoId(command);

        // Obtener secuencia correlativa
        int sec = GetSecuencial(command, codcco);

        // Construir la ruta en el formato que espera AS400: C:\Vid{codcco}Marq
        string rutaAs400 = $"C:\\Vid{codcco}Marq";

        // Construir query de inserción SQL
        command.CommandText = $@"
            INSERT INTO BCAH96DTA.MANTVIDEO 
            (CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ)
            VALUES('{codcco}', {codVideo}, '{rutaAs400}', '{nombreArchivo}', '{estado}', {sec})
        ";

        // Ejecutar inserción
        int result = command.ExecuteNonQuery();

        return result > 0;
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
