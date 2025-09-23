// Lista completa que quieres dejar en LIBL (ajusta el orden a tu necesidad)
var libl = "QTEMP BCAH96DTA BNKPRD01 QGPL";

// Comando CL en un SOLO statement
var clCmd = $"CHGLIBL LIBL({libl})";

// Longitud para QCMDEXC = número de caracteres del comando, con escala 5
static decimal QcmdexcLen(string s) => Convert.ToDecimal(s.Length.ToString() + ".00000", System.Globalization.CultureInfo.InvariantCulture);

using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
cmd.CommandText = "CALL QSYS2.QCMDEXC(?, ?)";
var p1 = cmd.CreateParameter(); p1.DbType = System.Data.DbType.String;  p1.Value = clCmd;         cmd.Parameters.Add(p1);
var p2 = cmd.CreateParameter(); p2.DbType = System.Data.DbType.Decimal; p2.Precision = 15; p2.Scale = 5; p2.Value = QcmdexcLen(clCmd); cmd.Parameters.Add(p2);

cmd.ExecuteNonQuery();
