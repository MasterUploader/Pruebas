var query = QueryBuilder
    .From("PQR02QRG", "IS4TECHDTA").As("Q")
    .Join("PQR01CLI", "IS4TECHDTA", "C", "Q.PQRCIF", "C.CLINRO")
    .Select("*")
    .Build();

var query = QueryBuilder
    .From("PQR02QRG", "IS4TECHDTA").As("Q")
    .Join("PQR01CLI", "IS4TECHDTA", "C", "Q.PQRCIF", "C.CLINRO")
    .Join("ENTIDAD", "ACH", "E", "Q.PQRABA", "E.CODENT")
    .Select("*")
    .Build();
