Hice el cambio de esta forma

 /// <summary>
 /// Actualiza los datos de una agencia existente.
 /// </summary>
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
       
         var rows = command.ExecuteNonQuery();

         return rows > 0; 
     }
     finally
     {
         _as400.Close();
     }
 }
En esta linea me dice rows = 1
var rows = command.ExecuteNonQuery();

Pero al evaluar me da el error
 return rows > 0; 
