using System.Data;
using System.Data.Common;
using System.Globalization;

private bool CargarLibrerias(out string? error)
{
    error = null;

    // Ajusta el orden según tu necesidad
    string[] libs = { "QTEMP", "ICBS", "BCAH96", "BCAH96DTA", "BNKPRD01", "QGPL", "GX", "COVENPGMV4" };

    // Comando CL en un solo statement
    var clCmd = $"CHGLIBL LIBL({string.Join(' ', libs)})";

    // Longitud para QCMDEXC: 15,5 (e.g. 23.00000)
    static decimal CmdLen(string s) =>
        decimal.Parse($"{s.Length}.00000", CultureInfo.InvariantCulture);

    try
    {
        // 1) Ejecuta CHGLIBL
        using (var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!))
        {
            cmd.CommandText = "CALL QSYS2.QCMDEXC(?, ?)";
            var p1 = cmd.CreateParameter(); p1.DbType = DbType.String;  p1.Value = clCmd;                  cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.DbType = DbType.Decimal; p2.Precision = 15; p2.Scale = 5;  p2.Value = CmdLen(clCmd); cmd.Parameters.Add(p2);
            cmd.ExecuteNonQuery();
        }

        // 2) Verificación opcional: que todas las librerías estén en el LIBL
        using (var v = _connection.GetDbCommand(_contextAccessor.HttpContext!))
        {
            v.CommandText = "SELECT UPPER(SYSTEM_SCHEMA_NAME) FROM QSYS2.LIBRARY_LIST_INFO";
            using var r = v.ExecuteReader();

            var actuales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (r.Read()) actuales.Add(r.GetString(0));

            foreach (var lib in libs)
            {
                if (!actuales.Contains(lib))
                {
                    error = $"Falta en LIBL: {lib}";
                    return false;
                }
            }
        }

        return true;
    }
    catch (DbException ex)
    {
        error = ex.Message;
        return false;
    }
}
