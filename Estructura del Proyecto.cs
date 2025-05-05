/// <summary>
/// Obtiene la lista de mensajes registrados en la tabla MANTMSG desde el AS400.
/// </summary>
/// <returns>Una lista de objetos MensajeModel con los datos cargados desde la base de datos.</returns>
public async Task<List<MensajeModel>> ObtenerMensajesAsync()
{
    var mensajes = new List<MensajeModel>();

    try
    {
        _as400.Open();
        using var command = _as400.GetDbCommand();

        // Consulta para obtener todos los mensajes registrados
        command.CommandText = @"
            SELECT CODCCO, CODMSG, SEQ, MENSAJE, ESTADO
            FROM BCAH96DTA.MANTMSG
            ORDER BY CODCCO, SEQ";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            mensajes.Add(new MensajeModel
            {
                Codcco = reader["CODCCO"]?.ToString(),
                CodMsg = Convert.ToInt32(reader["CODMSG"]),
                Seq = Convert.ToInt32(reader["SEQ"]),
                Mensaje = reader["MENSAJE"]?.ToString(),
                Estado = reader["ESTADO"]?.ToString()
            });
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error al obtener los mensajes: " + ex.Message);
        // Puedes usar un logger aquí si ya tienes uno integrado
    }
    finally
    {
        _as400.Close();
    }

    return mensajes;
}


/// <summary>
/// Verifica si un mensaje tiene dependencias en otra tabla antes de eliminarlo.
/// Esto previene eliminar mensajes que aún están en uso.
/// </summary>
/// <param name="codcco">Código de agencia</param>
/// <param name="codMsg">Código del mensaje</param>
/// <returns>True si hay dependencias encontradas, False si no hay o si ocurre un error.</returns>
public bool TieneDependencia(string codcco, int codMsg)
{
    try
    {
        _as400.Open();
        using var command = _as400.GetDbCommand();

        // Consulta a una tabla relacionada (ajústala si se conoce el nombre real)
        command.CommandText = $@"
            SELECT COUNT(*) 
            FROM BCAH96DTA.OTRATABLA 
            WHERE CODCCO = '{codcco}' 
              AND CODMSG = {codMsg}";

        var count = Convert.ToInt32(command.ExecuteScalar());
        return count > 0;
    }
    catch
    {
        return true; // Si hay error, asumimos que sí tiene dependencia
    }
    finally
    {
        _as400.Close();
    }
}
