Tengo un problema no puedo usar el codigo así:

var query = new DeleteQueryBuilder("RSAGE01", "BCAH96DTA")
        .Where<RSAGE01>(X=> X.CODCCO == codcco)
        .Build();

Porque no existe una opción que use algo similar a <RSAGE01> como lo hacen otros metodos
