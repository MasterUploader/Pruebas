var query = QueryBuilder
    .From("USUADMIN", "BCAH96DTA").As("U")
    .Join("USUPERFIL", "BCAH96DTA", "P", "U.USUARIO", "P.USUARIO")
    .Select(("U.USUARIO", "CODIGO"), ("P.PERFIL", "ROL"))
    .Where<USUADMIN>(x => x.ESTADO == "A")
    .Build();
