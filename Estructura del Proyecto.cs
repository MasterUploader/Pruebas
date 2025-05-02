/// <summary>
/// Inserta el registro del video en la base de datos AS400, usando la ruta personalizada si existe.
/// </summary>
public bool GuardarRegistroEnAs400(string codcco, string estado, string nombreArchivo, string rutaContenedorBase)
{
    try
    {
        _as400.Open();
        using var command = _as400.GetDbCommand();

        if (command.Connection.State != ConnectionState.Open)
            command.Connection.Open();

        // Obtener la ruta personalizada desde AS400, si está disponible
        string rutaServidor = GetRutaServer(codcco);

        // Si no hay ruta personalizada, se usa la del archivo de configuración (ContenedorVideos)
        string rutaFinal = !string.IsNullOrWhiteSpace(rutaServidor)
            ? Path.Combine(rutaServidor, "Marquesin")
            : Path.Combine(rutaContenedorBase, codcco, "Marquesin");

        // Asegurar doble barra para compatibilidad con AS400
        rutaFinal = rutaFinal.Replace("\\", "\\\\");

        // Obtener nuevo ID para CODVIDEO
        int codVideo = GetUltimoId(command);

        // Obtener secuencia correlativa por agencia
        int sec = GetSecuencial(command, codcco);

        // Armar consulta SQL para inserción
        string insert = $@"
            INSERT INTO BCAH96DTA.MANTVIDEO
            (CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ)
            VALUES ('{codcco}', {codVideo}, '{rutaFinal}', '{nombreArchivo}', '{estado}', {sec})";

        using var cmd = new OleDbCommand(insert, (OleDbConnection)command.Connection);
        int rowsAffected = cmd.ExecuteNonQuery();

        return rowsAffected > 0;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al insertar en AS400: {ex.Message}");
        return false;
    }
    finally
    {
        _as400.Close();
    }
}





/// <summary>
/// Devuelve la ruta personalizada desde la tabla RSAGE01 según el código de agencia.
/// </summary>
private string GetRutaServer(string codcco)
{
    try
    {
        using var command = _as400.GetDbCommand();
        command.CommandText = $"SELECT NONSER FROM BCAH96DTA.RSAGE01 WHERE CODCCO = '{codcco}'";

        if (command.Connection.State == ConnectionState.Closed)
            command.Connection.Open();

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            var ruta = reader["NONSER"]?.ToString();
            if (!string.IsNullOrWhiteSpace(ruta))
                return $"\\\\{ruta}\\";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al obtener ruta de servidor: {ex.Message}");
    }

    return null; // Si falla, retornará null y se usará ContenedorVideos
}
