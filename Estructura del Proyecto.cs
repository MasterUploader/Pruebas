var query = QueryBuilder.Core.QueryBuilder
    .From("USUADMIN", "BCAH96DTA")
    .Select(("USUARIO", "User"), ("TIPUSU", "Type"))
    .Distinct()
    .Build();


var query = QueryBuilder.Core.QueryBuilder
    .From("LOGS", "APPDTA")
    .Select(("TIPO", "TipoEvento"), ("COUNT(*)", "Cantidad"))
    .GroupBy("TIPO")
    .Build();

var query = QueryBuilder.Core.QueryBuilder
    .From("VENTAS", "COMDTA")
    .Select(("VENDEDOR", "Empleado"), ("SUM(TOTAL)", "TotalVentas"))
    .GroupBy("VENDEDOR")
    .Having<dynamic>(v => v.TotalVentas > 10000)
    .Build();

var query = QueryBuilder.Core.QueryBuilder
    .From("CLIENTES", "VENTASDTA")
    .Select("NOMBRE", "FECHAREGISTRO")
    .OrderBy(("FECHAREGISTRO", SortDirection.Desc), ("NOMBRE", SortDirection.Asc))
    .Build();

var query = QueryBuilder.Core.QueryBuilder
    .From("PEDIDOS", "VENTASDTA")
    .Select(("C.CLIENTEID", "ClienteId"), ("COUNT(P.ID)", "TotalPedidos"))
    .As("P")
    .Join("CLIENTES", "VENTASDTA", "C", "P.CLIENTEID", "C.ID")
    .GroupBy("C.CLIENTEID")
    .Having<dynamic>(x => x.TotalPedidos >= 5)
    .OrderBy(("TotalPedidos", SortDirection.Desc))
    .Build();
