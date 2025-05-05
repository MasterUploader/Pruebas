/// <summary>
/// Obtiene la próxima secuencia (SEQ) a usar para una agencia específica en la tabla MANTMSG.
/// Esto se utiliza para ordenar los mensajes dentro de una misma agencia.
/// </summary>
/// <param name="codcco">Código de la agencia para la cual se desea obtener la secuencia.</param>
/// <returns>Entero que representa la próxima secuencia disponible. Retorna 1 si no hay registros previos o si hay error.</returns>
public int GetSecuencia(string codcco)
{
    try
    {
        // Abrir conexión a AS400
        _as400.Open();
        using var command = _as400.GetDbCommand();

        // Consulta SQL para obtener el valor máximo actual de SEQ en la agencia indicada
        command.CommandText = $"SELECT MAX(SEQ) FROM BCAH96DTA.MANTMSG WHERE CODCCO = '{codcco}'";

        var result = command.ExecuteScalar();

        // Si el resultado no es nulo, convertir a entero y sumar 1
        return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
    }
    catch
    {
        // Si hay error, devolvemos 1 como secuencia inicial
        return 1;
    }
    finally
    {
        // Asegurar cierre de la conexión
        _as400.Close();
    }
}
