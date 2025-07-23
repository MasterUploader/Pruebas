var query = QueryBuilder
    .From("USUADMIN", "BCAH96DTA")
    .As("U")
    .Select(("U.USUARIO", "CODIGO"), ("U.TIPUSU", "TIPO"))
    .Where<USUADMIN>(x => x.ESTADO == "A")
    .OrderBy(("U.USUARIO", SortDirection.Desc))
    .Limit(5)
    .Build();
