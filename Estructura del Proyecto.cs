Como quedaria este insert:

 public bool InsertarAgencia(AgenciaModel agencia)
 {
     try
     {
         _as400.Open();
         using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);

         command.CommandText = $@"
                 INSERT INTO BCAH96DTA.RSAGE01 
                 (CODCCO, NOMAGE, ZONA, MARQUESINA, RSTBRANCH, NOMBD, NOMSER, IPSER)
                 VALUES 
                 ({agencia.Codcco}, '{agencia.NomAge}', {agencia.Zona}, '{agencia.Marquesina}', 
                  '{agencia.RstBranch}', '{agencia.NomBD}', '{agencia.NomSer}', '{agencia.IpSer}')";

         return command.ExecuteNonQuery() > 0;
     }
     finally
     {
         _as400.Close();
     }
 }
