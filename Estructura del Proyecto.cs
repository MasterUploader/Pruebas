public sealed class ValidarTransaccionesRequest
{
    public string NumeroDeCorte { get; set; } = "";
    public string IdTransaccionUnico { get; set; } = "";
}

public sealed class Posre01gRecordDto
{
    public string Guid { get; set; } = "";
    public string FechaPosteo { get; set; } = "";
    public string HoraPosteo { get; set; } = "";
    public string NumeroCuenta { get; set; } = "";
    public string MontoDebitado { get; set; } = "";
    public string MontoAcreditado { get; set; } = "";
    public string CodigoComercio { get; set; } = "";
    public string NombreComercio { get; set; } = "";
    public string TerminalComercio { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string NaturalezaContable { get; set; } = "";
    public string NumeroCorte { get; set; } = "";
    public string IdTransaccionUnico { get; set; } = "";
    public string EstadoTransaccion { get; set; } = "";
    public string DescripcionEstado { get; set; } = "";
    public string CodigoError { get; set; } = "";
    public string DescripcionError { get; set; } = "";
}

public sealed class ValidarTransaccionesResponse
{
    public bool Existe { get; set; }
    public string Mensaje { get; set; } = "";
    public Posre01gRecordDto? Registro { get; set; }
}






using System.Text.RegularExpressions;
using System.Data.Common;
using Microsoft.AspNetCore.Http;
using RestUtilities.Connections.Abstractions; // IDatabaseConnection
// using RestUtilities.QueryBuilder.Core;      // Ajusta a tu namespace real
// using RestUtilities.QueryBuilder.Builders;

public interface IValidarTransaccionesService
{
    Task<ValidarTransaccionesResponse> ValidarAsync(string numeroCorte, string idTransaccionUnico, CancellationToken ct = default);
}

public sealed class ValidarTransaccionesService : IValidarTransaccionesService
{
    private readonly IDatabaseConnection _db;
    private readonly IHttpContextAccessor _http;

    public ValidarTransaccionesService(IDatabaseConnection db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<ValidarTransaccionesResponse> ValidarAsync(string numeroCorte, string idTransaccionUnico, CancellationToken ct = default)
    {
        // Normaliza: ambos campos son CHAR(6) en DDS
        static string San(string v, int len)
        {
            if (string.IsNullOrWhiteSpace(v)) return new string('0', len);
            v = v.Trim();
            v = Regex.Replace(v, @"[^A-Za-z0-9_]", "");
            v = v.Length > len ? v[^len..] : v.PadLeft(len, '0');
            return v.ToUpperInvariant();
        }

        var corte = San(numeroCorte, 6);
        var stan  = San(idTransaccionUnico, 6);

        // ============================
        // Query con SelectQueryBuilder
        // ============================
        var qb = new SelectQueryBuilder("POSRE01G01", "BCAH96DTA", SqlDialect.Db2i)
            // Selección (usamos alias para mapear limpio al DTO)
            .Select(
                ("GUID",                     "GUID"),
                ("FECHAPOST",               "FECHA_POSTEO"),
                ("HORAPOST",                "HORA_POSTEO"),
                ("NUMCUENTA",               "NUMERO_CUENTA"),
                ("MTODEBITO",               "MONTO_DEBITADO"),
                ("MTOACREDI",               "MONTO_ACREDITADO"),
                ("CODCOMERC",               "CODIGO_COMERCIO"),
                ("NOMCOMERC",               "NOMBRE_COMERCIO"),
                ("TERMINAL",                "TERMINAL_COMERCIO"),
                ("DESCRIPC",                "DESCRIPCION"),
                ("NATCONTA",                "NATURALEZA_CONTABLE"),
                ("NUMCORTE",                "NUMERO_CORTE"),
                ("IDTRANUNI",               "ID_TRANSACCION_UNICO"),
                ("ESTADO",                  "ESTADO_TRANSACCION"),
                ("DESCESTADO",              "DESCRIPCION_ESTADO"),
                ("CODERROR",                "CODIGO_ERROR"),
                ("DESCERROR",               "DESCRIPCION_ERROR")
            )
            // Filtrado por la clave del LF
            .WhereRaw($"NUMCORTE = '{corte}'")
            .WhereRaw($"IDTRANUNI = '{stan}'")
            .FetchFirst(1); // AS400: FETCH FIRST N ROWS ONLY

        var qr = qb.Build(); // -> QueryResult con .Sql (y .Parameters vacío en tu versión)

        _db.Open();
        if (!_db.IsConnected)
        {
            return new ValidarTransaccionesResponse
            {
                Existe = false,
                Mensaje = "No se pudo establecer conexion con AS400.",
                Registro = null
            };
        }

        try
        {
            // Preferimos el overload que acepta QueryResult (inyecta logging y params si aplica)
            DbCommand cmd;
            try
            {
                cmd = _db.GetDbCommand(qr, _http?.HttpContext);
            }
            catch
            {
                cmd = _db.GetDbCommand();
                cmd.CommandText = qr.Sql;
            }

            using var rd = await cmd.ExecuteReaderAsync(ct);
            if (await rd.ReadAsync(ct))
            {
                var r = new Posre01gRecordDto
                {
                    Guid                = rd["GUID"]?.ToString() ?? "",
                    FechaPosteo         = rd["FECHA_POSTEO"]?.ToString() ?? "",
                    HoraPosteo          = rd["HORA_POSTEO"]?.ToString() ?? "",
                    NumeroCuenta        = rd["NUMERO_CUENTA"]?.ToString() ?? "",
                    MontoDebitado       = rd["MONTO_DEBITADO"]?.ToString() ?? "",
                    MontoAcreditado     = rd["MONTO_ACREDITADO"]?.ToString() ?? "",
                    CodigoComercio      = rd["CODIGO_COMERCIO"]?.ToString() ?? "",
                    NombreComercio      = rd["NOMBRE_COMERCIO"]?.ToString() ?? "",
                    TerminalComercio    = rd["TERMINAL_COMERCIO"]?.ToString() ?? "",
                    Descripcion         = rd["DESCRIPCION"]?.ToString() ?? "",
                    NaturalezaContable  = rd["NATURALEZA_CONTABLE"]?.ToString() ?? "",
                    NumeroCorte         = rd["NUMERO_CORTE"]?.ToString() ?? "",
                    IdTransaccionUnico  = rd["ID_TRANSACCION_UNICO"]?.ToString() ?? "",
                    EstadoTransaccion   = rd["ESTADO_TRANSACCION"]?.ToString() ?? "",
                    DescripcionEstado   = rd["DESCRIPCION_ESTADO"]?.ToString() ?? "",
                    CodigoError         = rd["CODIGO_ERROR"]?.ToString() ?? "",
                    DescripcionError    = rd["DESCRIPCION_ERROR"]?.ToString() ?? ""
                };

                return new ValidarTransaccionesResponse
                {
                    Existe = true,
                    Mensaje = "La transaccion ya existe (NUMCORTE, IDTRANUNI).",
                    Registro = r
                };
            }

            return new ValidarTransaccionesResponse
            {
                Existe = false,
                Mensaje = "No existe el registro. Puede continuar.",
                Registro = null
            };
        }
        finally
        {
            _db.Close();
        }
    }
}




using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public sealed class TransaccionesController : ControllerBase
{
    private readonly IValidarTransaccionesService _svc;
    public TransaccionesController(IValidarTransaccionesService svc) => _svc = svc;

    [HttpPost("ValidarTransacciones")]
    public async Task<IActionResult> ValidarTransacciones([FromBody] ValidarTransaccionesRequest req, CancellationToken ct)
    {
        var r = await _svc.ValidarAsync(req.NumeroDeCorte, req.IdTransaccionUnico, ct);
        if (r.Existe) return Conflict(r); // 409
        return Ok(r);                     // 200
    }
}








