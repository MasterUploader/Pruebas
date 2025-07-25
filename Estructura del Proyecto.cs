/// <summary>
/// Crea un comando configurado con la consulta SQL generada por QueryBuilder y sus parámetros asociados.
/// </summary>
/// <param name="queryResult">Objeto que contiene el SQL generado y la lista de parámetros.</param>
/// <param name="context">Contexto HTTP actual para trazabilidad opcional.</param>
/// <returns>DbCommand listo para ejecución.</returns>
public DbCommand GetDbCommand(QueryResult queryResult, HttpContext? context)
{
    var command = GetDbCommand(context);

    // Establece el SQL
    command.CommandText = queryResult.Sql;

    // Limpia cualquier parámetro anterior
    command.Parameters.Clear();

    // Agrega los parámetros a la posición correspondiente
    if (queryResult.Parameters is not null && queryResult.Parameters.Count > 0)
    {
        foreach (var paramValue in queryResult.Parameters)
        {
            var parameter = command.CreateParameter();
            parameter.Value = paramValue ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
    }

    return command;
}
