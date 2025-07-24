var query = new UpdateQueryBuilder("RSAGE01", "BCAH96DTA")
    .Set("NOMAGE", agencia.NomAge)
    .Set("ZONA", agencia.Zona)
    .Set("MARQUESINA", agencia.Marquesina)
    .Set("RSTBRANCH", agencia.RstBranch)
    .Set("NOMBD", agencia.NomBD)
    .Set("NOMSER", agencia.NomSer)
    .Set("IPSER", agencia.IpSer)
    .Where("CODCCO", agencia.Codcco)
    .Build();

using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);
command.CommandText = query.Sql;
command.ExecuteNonQuery();
