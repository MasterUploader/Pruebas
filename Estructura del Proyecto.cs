/// <summary>
/// Obtiene la lista completa de videos registrados desde AS400.
/// </summary>
/// <returns>Lista de objetos VideoModel con la información cargada desde la tabla MANTVIDEO.</returns>
public async Task<List<VideoModel>> ObtenerListaVideosAsync()
{
    var videos = new List<VideoModel>();

    try
    {
        // Abrir conexión a AS400
        _as400.Open();

        // Crear comando SQL para obtener los registros de la tabla MANTVIDEO
        using var command = _as400.GetDbCommand();
        command.CommandText = @"
            SELECT CODCCO, CODVIDEO, RUTA, NOMBRE, ESTADO, SEQ 
            FROM BCAH96DTA.MANTVIDEO
            ORDER BY CODCCO, SEQ";

        using var reader = await command.ExecuteReaderAsync();

        // Recorrer los resultados y mapear a objetos VideoModel
        while (await reader.ReadAsync())
        {
            var video = new VideoModel
            {
                Codcco = reader["CODCCO"]?.ToString(),
                Codvideo = Convert.ToInt32(reader["CODVIDEO"]),
                Ruta = reader["RUTA"]?.ToString(),
                Nombre = reader["NOMBRE"]?.ToString(),
                Estado = reader["ESTADO"]?.ToString(),
                Seq = Convert.ToInt32(reader["SEQ"])
            };

            videos.Add(video);
        }
    }
    catch (Exception ex)
    {
        // Manejo de errores controlado (puedes usar logging aquí)
        Console.WriteLine($"Error al obtener lista de videos: {ex.Message}");
    }
    finally
    {
        // Asegurar cierre de conexión
        _as400.Close();
    }

    return videos;
}
