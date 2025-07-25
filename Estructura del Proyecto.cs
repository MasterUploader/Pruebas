/// <summary>
/// Inserta una nueva agencia en la base de datos.
/// </summary>
/// <param name="agencia">Objeto que contiene los datos de la agencia a insertar.</param>
/// <returns>True si la inserción fue exitosa; false en caso contrario.</returns>
public bool InsertarAgencia(AgenciaModel agencia)
{
    try
    {
        _as400.Open();

        // Generar la sentencia SQL con QueryBuilder
        var query = new InsertQueryBuilder("RSAGE01", "BCAH96DTA")
            .Values(
                ("CODCCO", agencia.Codcco),
                ("NOMAGE", agencia.NomAge),
                ("ZONA", agencia.Zona),
                ("MARQUESINA", agencia.Marquesina),
                ("RSTBRANCH", agencia.RstBranch),
                ("NOMBD", agencia.NomBD),
                ("NOMSER", agencia.NomSer),
                ("IPSER", agencia.IpSer)
            )
            .Build();

        // Crear y ejecutar el comando
        using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);
        command.CommandText = query.Sql;

        return command.ExecuteNonQuery() > 0;
    }
    finally
    {
        _as400.Close();
    }
}
