Me genero este sql, que esta bien porque lo probe desde otro programa

UPDATE BCAH96DTA.RSAGE01 SET NOMAGE = 'General',ZONA = '1',MARQUESINA = 'SI',RSTBRANCH = 'SI',NOMBD = 'Prueba',NOMSER = 'Prueba',IPSER = '127.0.1.1' WHERE (CODCCO = 0)

  Pero tengo un error
System.NullReferenceException: 'Object reference not set to an instance of an object.'
  
  En esta linea
  return command.ExecuteNonQuery() > 0;

Este es el codigo
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

return command.ExecuteNonQuery() > 0;
