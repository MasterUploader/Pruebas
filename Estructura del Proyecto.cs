var query = QueryBuilder.Core.QueryBuilder
    .From("PAGOS", "FINANZAS")
    .Select(
        "TIPO_PAGO",
        "MONEDA",
        "SUM(MONTO)",
        "AVG(MONTO)",
        "COUNT(*)"
    )
    .GroupBy("TIPO_PAGO", "MONEDA")
    .OrderBy(
        ("SUM(MONTO)", SortDirection.Desc),
        ("AVG(MONTO)", SortDirection.Asc)
    )
    .Build();

Console.WriteLine(query.Sql);
