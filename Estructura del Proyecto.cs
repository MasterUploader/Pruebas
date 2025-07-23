El codigo quedaria así, no puedo enviarle el query.sql dentro del GetDbCommand, porque este recibe un context

var query = QueryBuilder.Core.QueryBuilder
    .From("USUADMIN", "BCAH96DTA")
    .Select("TIPUSU", "ESTADO", "PASS")
    .Where<USUADMIN>(c => c.USUARIO == username)
    .Build();

using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);
//Validamos si la coneción se establecio
if (command.Connection.State == System.Data.ConnectionState.Closed)
    command.Connection.Open();


Usuario datos = null;

using var reader = command.ExecuteReader();
if (reader.Read())
{
    datos = new Usuario
    {
        TIPUSU = reader["TIPUSU"].ToString(),
        ESTADO = reader["ESTADO"].ToString(),
        PASS = reader["PASS"].ToString()
    };
}
