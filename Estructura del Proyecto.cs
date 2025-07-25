var builder = new UpdateQueryBuilder("MANTMSG", "BCAH960TA")
    .Set(("SEQ", mensaje.Seq))
    .Set(("MENSAJE", mensaje.Mensaje))
    .Set(("ESTADO", mensaje.Estado))
    .Where<MensajeModel>(x => x.CodMsg == mensaje.CodMsg && x.CodCco == mensaje.CodCco);

var query = builder.Build();

using var command = _as400.GetDbCommand(query, _httpContextAccessor.HttpContext);
_as400.Open();
var updated = command.ExecuteNonQuery() > 0;
_as400.Close();
return updated;


var builder = new SelectQueryBuilder("MANTMSG", "BCAH960TA")
    .Select("MAX(CODMSG)");

var query = builder.Build();

using var command = _as400.GetDbCommand(query, _httpContextAccessor.HttpContext);
_as400.Open();
var result = command.ExecuteScalar();
_as400.Close();

return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
