Tengo este codigo

public bool ActualizarAgencia(AgenciaModel agencia)
{
    try
    {
        _as400.Open();

        //Construimos el Query
        var query = new UpdateQueryBuilder("RSAGE01", "BCAH96DTA")
            .Set("NOMAGE", agencia.NomAge)
            .Set("ZONA", agencia.Zona)
            .Set("MARQUESINA", agencia.Marquesina)
            .Set("RSTBRANCH", agencia.RstBranch)
            .Set("NOMBD", agencia.NomBD)
            .Set("NOMSER", agencia.NomSer)
            .Set("IPSER", agencia.IpSer)
            .Where<RSAGE01>(x => x.CODCCO == agencia.Codcco)
            .Build();

        using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);
        command.CommandText = query.Sql;

        int rows = command.ExecuteNonQuery();

        return rows > 0;
    }
    finally
    {
        _as400.Close();
    }
}

Uso RestUtilities.Connections y RestUtilities.QueryBuilder, pero al ejecutar el comando
int rows = command.ExecuteNonQuery();

me dice que rows es 1, pero al querer evaluar return rows > 0; , me lanza la excepción System.NullReferenceException: 'Object reference not set to an instance of an object.', pareciera un problema con el wrapper.
