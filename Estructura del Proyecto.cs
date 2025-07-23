Con este codigo
var query = QueryBuilder.Core.QueryBuilder
    .From("USUADMIN", "BCAH96DTA")
    .Select("TIPUSU", "ESTADO", "PASS")
    .Where<USUADMIN>(x => (x.TIPUSU == "A" || x.TIPUSU == "B") && x.ESTADO != null)
    .Build();

Da este error:

System.InvalidOperationException: 'variable 'x' of type 'CAUAdministracion.Models.USUADMIN' referenced from scope '', but it is not defined'

    Y con este codigo

    var query = QueryBuilder.Core.QueryBuilder
    .From("USUADMIN", "BCAH96DTA")
    .Select("TIPUSU", "ESTADO", "PASS")
    .Where<USUADMIN>(c => c.USUARIO == username)
    .Build();

Genera así SELECT TIPUSU, ESTADO, PASS FROM BCAH96DTA.USUADMIN, en el cual hace falta el where, no se esta generando correctamente, hay que revisar lo que genera el where dentro de la libreria
