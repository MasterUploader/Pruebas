Con este codigo
var query = QueryBuilder.Core.QueryBuilder
    .From("USUADMIN", "BCAH96DTA")
    .Select("TIPUSU", "ESTADO", "PASS")
    .Where<USUADMIN>(x => x.TIPUSU == "A" || x.TIPUSU == "B")
    .Where<USUADMIN>(x => x.ESTADO != null)
    .Build();

Da este error:

System.InvalidOperationException: 'variable 'x' of type 'CAUAdministracion.Models.USUADMIN' referenced from scope '', but it is not defined'
