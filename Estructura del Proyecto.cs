En este codigo

 public bool TieneDependencias(string codcco, int codVideo)
 {
     try
     {
         _as400.Open();

         //Construimos el Query
         var query = QueryBuilder.Core.QueryBuilder
         .From("MANTVIDEO", "BCAH96DTA")
         .Select("COUNT(*)")
         .Where<MANTVIDEO>(x => x.CODCCO == codcco && x.CODVIDEO == codVideo)
         .Build();

         using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);
         command.CommandText = query.Sql;

         var count = Convert.ToInt32(command.ExecuteScalar());
         if(count > 0)
         {
             return false;
         }
         return true;
     }
     catch
     {
         return true; // Si hay error, asumimos que tiene dependencias para prevenir borrado
     }
     finally
     {
         _as400.Close();
     }
 }

Tengo eñl problema que me gener el sql así 

SELECT COUNT(*) AS COUNT_* FROM BCAH96DTA.MANTVIDEO WHERE ((CODCCO = '0') AND (CODVIDEO = 1))

