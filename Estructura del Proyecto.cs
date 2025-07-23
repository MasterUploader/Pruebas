Con este codigo me genero este SQL
var query = QueryBuilder.Core.QueryBuilder
    .From("USUADMIN", "BCAH96DTA")
    .Select("TIPUSU", "ESTADO", "PASS")
    .Where<USUADMIN>(x => (x.TIPUSU == "A" || x.TIPUSU == "B") && x.ESTADO != null)
    .Build();


SELECT TIPUSU, ESTADO, PASS FROM BCAH96DTA.USUADMIN WHERE (((TIPUSU = 'A') OR (TIPUSU = 'B')) AND ESTADO IS NOT NULL)

    Y me da este error

    [SQL0302] Error de conversión en variable o parámetro *N., 22023, -302
