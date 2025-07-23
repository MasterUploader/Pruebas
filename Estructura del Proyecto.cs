var query = QueryBuilder.Core.QueryBuilder
    .From("USUADMIN", "BCAH96DTA")
    .Select("TIPUSU", "ESTADO", "PASS")
    .Where<Usuario>(x => x.TIPUSU == "A" || x.TIPUSU == "B")
    .Where<Usuario>(x => x.ESTADO != null)
    .Build();
