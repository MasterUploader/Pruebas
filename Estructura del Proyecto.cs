Este codigo 
var query = QueryBuilder.Core.QueryBuilder
    .From("USUADMIN", "BCAH96DTA")
    .Select("TIPUSU", "ESTADO", "PASS")
    .Where<USUADMIN>(x => x.TIPUSU == "A" || x.TIPUSU == "B")
    .Where<USUADMIN>(x => x.ESTADO != null)
    .Build();

Solo me genero esto

SELECT TIPUSU, ESTADO, PASS FROM BCAH96DTA.USUADMIN WHERE (ESTADO <> NULL)
