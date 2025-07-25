var query = new SelectQueryBuilder("RSAGE01", "BCAH96DTA")
    .Select("CODCCO", "NOMAGE")
    .Where<RSAGE01>(x => x.MARQUESINA == "SI")
    .OrderByCase(
        CaseWhenBuilder
            .When<RSAGE01>(x => x.CODCCO == "0")
            .Then(0)
            .Else(1),
        null // sin alias porque es para ORDER BY
    )
    .OrderBy("NOMAGE")
    .Build();
