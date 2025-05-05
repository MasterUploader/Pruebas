/// <summary>
/// Inserta un nuevo mensaje en la tabla MANTMSG en AS400.
/// Genera el nuevo código automáticamente (MAX + 1) y secuencia correlativa por agencia.
/// </summary>
/// <param name="mensaje">Modelo con los datos del mensaje a insertar</param>
/// <returns>True si se insertó correctamente, false si hubo un error</returns>
public bool InsertarMensaje(MensajeModel mensaje)
{
    try
    {
        _as400.Open();

        if (!_as400.IsConnected())
            return false;

        using var command = _as400.GetDbCommand();

        // Obtener nuevo CODMSG
        int nuevoId = GetUltimoId(command);

        // Obtener nueva secuencia por agencia
        int nuevaSecuencia = GetSecuencia(command, mensaje.Codcco);

        // Construir query SQL de inserción
        command.CommandText = $@"
            INSERT INTO BCAH96DTA.MANTMSG (CODMSG, CODCCO, SEQ, MENSAJE, ESTADO)
            VALUES ({nuevoId}, '{mensaje.Codcco}', {nuevaSecuencia}, '{mensaje.Mensaje}', '{mensaje.Estado}')";

        int filas = command.ExecuteNonQuery();
        return filas > 0;
    }
    catch
    {
        // Podrías loguear aquí el error si deseas
        return false;
    }
    finally
    {
        _as400.Close();
    }
}



/// <summary>
/// Obtiene el próximo código de mensaje (CODMSG) a usar
/// </summary>
public int GetUltimoId(DbCommand command)
{
    command.CommandText = "SELECT MAX(CODMSG) FROM BCAH96DTA.MANTMSG";
    var result = command.ExecuteScalar();
    return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
}

/// <summary>
/// Obtiene la próxima secuencia (SEQ) para una agencia específica
/// </summary>
public int GetSecuencia(DbCommand command, string codcco)
{
    command.CommandText = $"SELECT MAX(SEQ) FROM BCAH96DTA.MANTMSG WHERE CODCCO = '{codcco}'";
    var result = command.ExecuteScalar();
    return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
}
