Toma esto como plantilla, confirma que de acá partiremos:

using Connections.Abstractions;
using Connections.Helpers;
using Microsoft.IdentityModel.Tokens;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BCAH96DTA;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BNKPRD01;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Utils;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using System.Data.Common;
using System.Globalization;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;

/// <summary>
/// Clase de servicio para el procesamiento de transacciones POS.
/// </summary>
/// <param name="_connection">Inyección de clase IDatabaseConnection.</param>
/// <param name="_contextAccessor">Inyección de clase IHttpContextAccessor.</param>
public partial class TransaccionesServices(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : ITransaccionesServices
{
    /// <summary>
    /// Represents the response data for saving transactions.
    /// </summary>
    /// <remarks>This field is intended to store an instance of <see cref="RespuestaGuardarTransaccionesDto"/>
    /// that contains the result of a transaction-saving operation. It is protected and can be accessed  or modified by
    /// derived classes.</remarks>
    protected RespuestaGuardarTransaccionesDto _respuestaGuardarTransaccionesDto = new();

    /// <ineheritdoc/>
    public async Task<RespuestaGuardarTransaccionesDto> GuardarTransaccionesAsync(GuardarTransaccionesDto guardarTransaccionesDto)
    {
        await Task.Yield(); // Simula asincronía para cumplir con la firma async.
        //Procesos Previos

        _connection.Open(); //Abrimos la conexión a la base de datos

        //============================Validaciones Previas============================//

        // Normalización de importes: tolera "." o "," y espacios
        var deb = Utilities.ParseMonto(guardarTransaccionesDto.MontoDebitado);
        var cre = Utilities.ParseMonto(guardarTransaccionesDto.MontoAcreditado);

        //Validamos que al menos uno de los montos sea mayor a 0, no se puede postear ambos en 0.
        if (deb <= 0m && cre <= 0m) return BuildError("400", "No hay importes a postear (ambos montos son 0).");

        decimal monto = deb > 0m ? deb : cre;

        //Obtenemos perfil transerver de la configuración global
        string perfilTranserver = GlobalConnection.Current.PerfilTranserver;

        //Validamos, si no hay perfil transerver, retornamos error porque el proceso no puede continuar.
        if (perfilTranserver.IsNullOrEmpty()) return BuildError("400", "No se ha configurado el perfil transerver a buscar en JSON.");

        //Validamos si existe el comercio en la tabla BCAH96DTA/IADQCOM
        var (existeComercio, codigoError, mensajeComercio) = BuscarComercio(guardarTransaccionesDto.NumeroCuenta, int.Parse(guardarTransaccionesDto.CodigoComercio));
        if (!existeComercio) return BuildError(codigoError, mensajeComercio);

        //Validación de Terminal
        var (existeTerminal, esTerminalVirtual, codigoErrorTerminal, mensajeTerminal) = BuscarTerminal(guardarTransaccionesDto.Terminal, int.Parse(guardarTransaccionesDto.CodigoComercio));
        if (!existeTerminal) return BuildError(codigoErrorTerminal, mensajeTerminal);

        //============================Fin Validaciones Previas============================//

        //============================Inicia Proceso Principal============================//

        // 1. Obtenemos el Perfil Transerver del cliente.
        var respuestaPerfil = VerPerfil(perfilTranserver);

        // Si no existe el perfil, retornar error y no continuar con el proceso.
        if (!respuestaPerfil.existePerfil) return BuildError(respuestaPerfil.codigoError, respuestaPerfil.descripcionError);

        //Buscar estos valores
        decimal tipoCuenta = 6; //Tipo de cuenta fija para POS

        //1. Cargamos las librerías necesarias en la LIBL de la conexión actual.
        var (agregoLibrerias, descripcionErrorLibrerias) = CargaLibrerias(); // Asegura que las librerías necesarias estén en el entorno de ejecución
        //Validamos si se agregaron las librerías, si no, retornamos error y no continuamos con el proceso.
        if (!agregoLibrerias) return BuildError("500", descripcionErrorLibrerias);

        // 2. Resolvemos los parámetros necesarios para llamar a Int_lotes.
        var p = ResolverParametrosIntLotes(
            esEcommerce: esTerminalVirtual,
            perfil: perfilTranserver,
            naturalezaCliente: guardarTransaccionesDto.NaturalezaContable,                 // 'C' o 'D'
            numeroCuenta: guardarTransaccionesDto.NumeroCuenta,
            codigoComercio: guardarTransaccionesDto.CodigoComercio,
            terminal: guardarTransaccionesDto.Terminal,
            nombreComercio: guardarTransaccionesDto.NombreComercio,
            idUnico: guardarTransaccionesDto.IdTransaccionUnico,
            monedaIsoNum: 0                            // si tu RPG lo espera, envía el ISO num correspondiente
        );

        // 3. Ejecutamos el programa INT_LOTES con los parámetros necesarios.
        Task<(int CodigoErrorPosteo, string? DescripcionErrorPosteo, string? nomArc)> respuesta = PosteoLoteAsync(p, tipoCuenta, Convert.ToDecimal(p.CuentaCliente), Convert.ToDecimal(monto), guardarTransaccionesDto.NaturalezaContable, p.CentroCostoGL, p.Moneda, perfilTranserver, p.Des001, p.Des002, p.Des003);

        //Si hay error en el posteo, retornamos error y no continuamos con el proceso.
        if (respuesta.Result.CodigoErrorPosteo != 0) return BuildError(respuesta.Result.CodigoErrorPosteo.ToString(), respuesta.Result.DescripcionErrorPosteo ?? "Error desconocido en INT_LOTES.");

        return BuildError(code: "200", message: "Transacción procesada correctamente.");
    }

    // ============================ Utilidades ============================

    /// <summary>
    /// Método de validación de existencia de comercio en tabla BCAH96DTA/IADQCOM.
    /// </summary>
    /// <param name="cuentaRecibida">Número de cuenta recibido en la petición.</param>
    /// <param name="codigoComercioRecibido">Código de Comercio recibido en la petición.</param>
    /// <returns>Retorna un tupla
    /// /// (existeComercio: true/false, codigoError: "000001" , mensajeComercio: "Descripcioón del error")
    /// </returns>
    private (bool existeComercio, string codigoError, string mensajeComercio) BuscarComercio(string cuentaRecibida, int codigoComercioRecibido)
    {
        try
        {
            // Construimos consulta SQL con QueryBuilder para verificar existencia de perfil
            var buscarComercio = QueryBuilder.Core.QueryBuilder
                .From("IADQCOM", "BCAH96DTA")
                .Select("*")  // Solo necesitamos validar existencia
                .Where<AdqCom>(x => x.ADQCOME == codigoComercioRecibido)
                .Where<AdqCom>(x => x.ADQCTDE == cuentaRecibida) // Filtro dinámico por perfil
                .FetchNext(1)                // Solo necesitamos un registro
                .OrderBy("ADQCOME", SortDirection.Asc)
                .Build();

            using var command = _connection.GetDbCommand(buscarComercio, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (true, "00001", "Existe Comercio."); // Coemrcio existe
            }
            return (false, "00002", "No existe Comercio."); // Comercio no existe
        }
        catch (Exception ex)
        {
            // Manejo de errores en la consulta
            return (false, "0003", ex.Message); // Indica error al consultar comercio
        }
    }

    /// <summary>
    /// Método de validación de existencia de terminal en tabla BCAH96DTA/ADQ03TER.
    /// </summary>
    /// <param name="terminalRecibida">Número de terminal Recibida</param>
    /// <param name="codigoComercioRecibido">Código Comercio Recibido.</param>
    /// <returns></returns>
    private (bool existeTerminal, bool esTerminalvirtual, string codigoError, string mensajeTerminal) BuscarTerminal(string terminalRecibida, int codigoComercioRecibido)
    {
        try
        {
            var terminal = string.Empty;
            // Construimos consulta SQL con QueryBuilder para verificar existencia de perfil
            var buscarComercio = QueryBuilder.Core.QueryBuilder
                .From("ADQ03TER", "BCAH96DTA")
                .Select("*")  // Solo necesitamos validar existencia
                .Where<Adq03Ter>(x => x.A03COME == codigoComercioRecibido)
                .Where<Adq03Ter>(x => x.A03TERM == terminalRecibida)
                .OrderBy(("A03TERM", SortDirection.Asc), ("A03TERM", SortDirection.Asc))
                .Build();

            using var command = _connection.GetDbCommand(buscarComercio, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.HasRows)
            {
                if (reader.Read())
                {
                    // -- Mapeo directo desde columnas proyectadas.
                    terminal = reader.GetString(reader.GetOrdinal("A03TERM"));
                }
                return (true, EsTerminalVirtual(terminal), "00001", "Existe terminal."); // Terminal existe
            }
            return (false, false, "00002", "No existe terminal."); // Terminal no existe
        }
        catch (Exception ex)
        {
            // Manejo de errores en la consulta
            return (false, false, "0003", ex.Message); // Indica error al consultar comercio
        }
    }

    /// <summary>
    /// Valida si la terminal corresponde a un e-commerce (virtual).
    /// Regla: la terminal se considera virtual si el primer carácter es 'E'.
    /// </summary>
    /// <param name="terminal">Número o código de terminal recibido.</param>
    /// <returns>True si es virtual (e-commerce), False en caso contrario.</returns>
    private static bool EsTerminalVirtual(string? terminal)
    {
        if (string.IsNullOrWhiteSpace(terminal))
            return false;

        // Evaluamos únicamente el primer carácter, sin importar minúscula/mayúscula
        return terminal.Trim().StartsWith("E", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifica si existe un perfil en la tabla CFP801 y ejecuta la lógica correspondiente.
    /// </summary>
    /// <param name="perfil">Clave de perfil (CFTSKY en RPGLE).</param>
    /// <returns>Tupla (bool, string,  string), true o false y descripción si existe o no el perfil</returns>
    private (bool existePerfil, string codigoError, string descripcionError) VerPerfil(string perfil)
    {
        try
        {
            // Construimos consulta SQL con QueryBuilder para verificar existencia de perfil
            var verPerfilSql = QueryBuilder.Core.QueryBuilder
                .From("CFP801", "BNKPRD01")
                .Select("CFTSBK", "CFTSKY")  // Solo necesitamos validar existencia
                .Where<Cfp801>(x => x.CFTSBK == 001)       // Condición fija
                .Where<Cfp801>(x => x.CFTSKY == perfil) // Filtro dinámico por perfil
                .FetchNext(1)                // Solo necesitamos un registro
                .Build();

            using var command = _connection.GetDbCommand(verPerfilSql, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (true, "00001", "Existe Perfil Transerver."); // Perfil existe
            }
            return (false, "00002", "No existe Perfil Transerver."); // Perfil no existe
        }
        catch (Exception ex)
        {
            // Manejo de errores en la consulta
            return (false, "0003", "Error general: " + ex.Message); // Indica error al consultar perfil
        }
    }

    /// <summary>
    /// Resuelve la contrapartida GL (cuenta y CC), t-codes, naturalezas y descripciones
    /// necesarias para llamar a <c>Int_lotes</c>, usando únicamente la info ya recibida
    /// (perfil, cuenta, comercio, terminal, etc.).
    /// </summary>
    /// <param name="esEcommerce">Es terminal Ecommerce</param>
    /// <param name="perfil">Perfil Transerver (CFTSKY).</param>
    /// <param name="naturalezaCliente">'C' para acreditar o 'D' para debitar al cliente.</param>
    /// <param name="numeroCuenta">Cuenta del cliente/comercio.</param>
    /// <param name="codigoComercio">Código de comercio (numérico string).</param>
    /// <param name="terminal">Terminal (para saber si es e-commerce y para AL2).</param>
    /// <param name="nombreComercio">Nombre del comercio (AL1).</param>
    /// <param name="idUnico">Identificador único de transacción (para AL3).</param>
    /// <param name="monedaIsoNum">Código ISO numérico de moneda si tu RPG lo requiere (ej: 840=USD). Usa 0 si no aplica.</param>
    /// <returns>DTO con todos lo necesario para armar las 2 líneas (cliente y GL) de Int_lotes.</returns>
    private IntLotesParamsDto ResolverParametrosIntLotes(
        bool esEcommerce,
        string perfil,
        string naturalezaCliente,
        string numeroCuenta,
        string codigoComercio,
        string terminal,
        string nombreComercio,
        string idUnico,
        int monedaIsoNum = 0
        )
    {
        // T-codes estándar de cliente (y su opuesto para GL)
        string tcodeCliente = naturalezaCliente == "C" ? "0783" : "0784";
        string tcodeGL = naturalezaCliente == "C" ? "0784" : "0783";
        char naturalezaGL = naturalezaCliente == "C" ? 'D' : 'C';

        // Para AL3 queremos incluir el tipo de cuenta (Ahorro/Cheques).
        var infoCta = VerCta(numeroCuenta); //Vemos si es Ahorro o Cheques la cuenta del cliente

        var tipoCtaCorto = string.IsNullOrWhiteSpace(infoCta.DescCorta) ? "" : infoCta.DescCorta.Trim();

        // Descripciones equivalentes al RPG (ajusta longitudes si tu Int_lotes limita a 30/40)
        string al1 = Trunc(nombreComercio, 30);
        string al2 = Trunc($"{codigoComercio}-{terminal}", 30);
        string al3 = Trunc($"{EtiquetaConcepto(naturalezaCliente.ToString())}-{idUnico}-{tipoCtaCorto}", 30);
        string al4 = ""; // sin uso de momento

        // === Resolver contrapartida GL ===
        // 1) CFP801 (auto-balance). Si está activo, usamos las cuentas del perfil.
        var auto = TryGetAutoBalance(perfil);
        string? glCuenta = null;
        int glCC = 0;
        string fuente = "N/A";

        if (auto.enabled)
        {
            if (naturalezaCliente == "C")
            {
                // Cliente a CR → GL a DB
                glCuenta = auto.glDebito;   // CFTSGD
                glCC = auto.ccDebito;   // CFTCCD
            }
            else
            {
                // Cliente a DB → GL a CR
                glCuenta = auto.glCredito;  // CFTSGC
                glCC = auto.ccCredito;  // CFTCCC
            }
            fuente = "CFP801";
            // Nota: aunque el core balancea solo, devolvemos igual la GL para formar la descripción
            // o por si tu Int_lotes exige enviarla explícitamente.
        }
        else
        {
            // 2) ADQECTL (si e-commerce) → busca el t-code opuesto (línea GL)
            if (esEcommerce && TryGetGLFromAdqEctl(codigoComercio, tcodeGL, out var glEc, out var ccEc))
            {
                glCuenta = glEc; glCC = ccEc; fuente = "ADQECTL";
            }
            // 3) ADQCTL genérico (por control/secuencia) → t-code opuesto
            else if (TryGetGLFromAdqCtl("GL", 1, tcodeGL, out var glG, out var ccG))
            {
                glCuenta = glG; glCC = ccG; fuente = "ADQCTL";
            }
        }

        // Tasa (si tu RPG la usa). Si no aplica, queda 0.
        decimal tasa = 0m;
        try { tasa = ObtenerTasaCompraUsd(); } catch { /* opcional */ }

        return new IntLotesParamsDto
        {
            Perfil = perfil,
            CuentaCliente = numeroCuenta,
            TipoCuentaCliente = infoCta.EsAhorro ? 1 : infoCta.EsCheques ? 6 : 40, // 1=Ahorros, 6=Cheques, 40=Contable/otro
            CuentaGL = glCuenta,
            CentroCostoGL = glCC,
            TcodeCliente = tcodeCliente,
            TcodeGL = tcodeGL,
            NaturalezaCliente = naturalezaCliente,
            NaturalezaGL = naturalezaGL,
            Des001 = al1,
            Des002 = al2,
            Des003 = string.IsNullOrWhiteSpace(glCuenta) ? al3 : Trunc($"{al3}-{glCuenta}", 30),
            Des004 = al4,
            Moneda = monedaIsoNum,
            TasaTm = tasa,
            EsAutoBalance = auto.enabled,
            FuenteGL = fuente,
            CentroCostoInterno = auto.ccCredito, // Solo para info, no lo usa Int_lotes
            NumeroCuentaInterna = Convert.ToDecimal( auto.glCredito) // Solo para info, no lo usa Int_lotes
        };
    }

    // ======================================================================
    // Helpers repuestos: VerCta, Trunc, EtiquetaConcepto, ObtenerTasaCompraUsd
    // ======================================================================

    

    /// <summary>
    /// Emula la lógica del procedimiento RPG <c>Ver_cta</c> para distinguir
    /// entre Ahorros/Cheques y devolver la etiqueta corta usada en AL3.
    /// 
    /// Nota: si no conoces aún el mapeo exacto por producto/tablas, esta versión
    /// aplica una heurística segura (prefijo) y, si quieres, aquí puedes
    /// reemplazar por una consulta a BNKPRD01.TAP002 cuando tengas las columnas
    /// definitivas.
    /// </summary>
    /// <param name="numeroCuenta">Cuenta del cliente/comercio.</param>
    private VerCtaResult VerCta(string numeroCuenta)
    {
        if (string.IsNullOrWhiteSpace(numeroCuenta))
            return new VerCtaResult { DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };

        // SELECT CFTSGE, CFTSGD, CFTCCD, CFTSGC, CFTCCC FROM BNKPRD01.CFP801 WHERE CFTSBK=1 AND CFTSKY=:perfil
        var q = QueryBuilder.Core.QueryBuilder
            .From("TAP00201", "BNKPRD01")
            .Select("DMTYP")
            .WhereRaw($"DMACCT = {numeroCuenta}")
            .FetchNext(1)
            .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return new VerCtaResult { DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };

        int tipoCuenta = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0));       

       if (tipoCuenta == 1)
            return new VerCtaResult { DescCorta = "AHO", Descripcion = "Ahorros", EsAhorro = true, EsCheques = false };

       if (tipoCuenta == 6)
            return new VerCtaResult { DescCorta = "CHE", Descripcion = "Cheques", EsAhorro = false, EsCheques = true };

        return new VerCtaResult { DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };
    }

    /// <summary>
    /// Trunca una cadena a <paramref name="max"/> caracteres (segura para null).
    /// </summary>
    private static string Trunc(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        if (max <= 0) return string.Empty;
        return s.Length <= max ? s : s[..max];
    }

    /// <summary>
    /// Devuelve la etiqueta de concepto que el RPG imprime en AL3:
    /// "CR" para naturaleza Crédito ("C") y "DB" para Débito ("D").
    /// </summary>
    private static string EtiquetaConcepto(string? naturaleza)
    {
        if (string.IsNullOrWhiteSpace(naturaleza)) return "";
        return naturaleza.Trim().Equals("C", StringComparison.OrdinalIgnoreCase) ? "CR" : "DB";
    }

    /// <summary>
    /// Obtiene la tasa de compra USD (si aplica). Si aún no tienes
    /// la tabla/fuente, devuelve 0 sin detener el proceso.
    /// </summary>
    private decimal ObtenerTasaCompraUsd()
    {
        try
        {
            // Si tienes tabla de tasas, reemplaza este bloque por tu SELECT real.
            // Ejemplo ilustrativo (ajusta nombres reales):
            // var q = QueryBuilder.Core.QueryBuilder
            //     .From("TASAS", "BNKPRD01")
            //     .Select("TASA_COMPRA_USD")
            //     .OrderBy("FECHA", QueryBuilder.Enums.SortDirection.Desc)
            //     .FetchNext(1)
            //     .Build();
            //
            // using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
            // var obj = cmd.ExecuteScalar();
            // return obj is null || obj is DBNull ? 0m : Convert.ToDecimal(obj, CultureInfo.InvariantCulture);

            return 0m; // fallback seguro
        }
        catch
        {
            return 0m; // nunca rompas el flujo por tasa
        }
    }


    /// <summary>
    /// Lee de CFP801 si el perfil genera asiento de balance (CFTSGE=1) y obtiene sus cuentas/CC.
    /// </summary>
    private (bool enabled, string glDebito, int ccDebito, string glCredito, int ccCredito) TryGetAutoBalance(string perfil)
    {
        // SELECT CFTSGE, CFTSGD, CFTCCD, CFTSGC, CFTCCC FROM BNKPRD01.CFP801 WHERE CFTSBK=1 AND CFTSKY=:perfil
        var q = QueryBuilder.Core.QueryBuilder
            .From("CFP801", "BNKPRD01")
            .Select("CFTSGE", "CFTSGD", "CFTCCD", "CFTSGC", "CFTCCC")
            .Where<Cfp801>(x => x.CFTSBK == 1)
            .Where<Cfp801>(x => x.CFTSKY == perfil)
            .FetchNext(1)
            .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return (false, "", 0, "", 0);

        int sge = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0));
        string glDb = rd.IsDBNull(1) ? "" : rd.GetValue(1).ToString()!.Trim();
        int ccDb = rd.IsDBNull(2) ? 0 : Convert.ToInt32(rd.GetValue(2));
        string glCr = rd.IsDBNull(3) ? "" : rd.GetValue(3).ToString()!.Trim();
        int ccCr = rd.IsDBNull(4) ? 0 : Convert.ToInt32(rd.GetValue(4));

        return (sge == 1, glDb, ccDb, glCr, ccCr);
    }

    /// <summary>
    /// Busca en ADQECTL (control e-commerce) la GL/CC cuyo T-code (ADQECTR1..15) coincida con <paramref name="tcodeBuscado"/>.
    /// </summary>
    private bool TryGetGLFromAdqEctl(string comercio, string tcodeBuscado, out string gl, out int cc)
    {
        gl = ""; cc = 0;
        var q = QueryBuilder.Core.QueryBuilder
            .From("ADQECTL", "BCAH96DTA")
            .Select("*")
            .WhereRaw("ADQECONT = 'EC'")
            .WhereRaw($"ADQENUM = {comercio}")   // si ADQENUM es numérico
            .FetchNext(1)
            .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return false;

        // Sin bucles, chequeamos 1..15 explícitos (tcode→cuenta/costo del mismo ordinal).
        string tr;
        // 1
        tr = rd["ADQECTR1"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT1"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO1"]); return !string.IsNullOrEmpty(gl); }
        // 2
        tr = rd["ADQECTR2"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT2"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO2"]); return !string.IsNullOrEmpty(gl); }
        // 3
        tr = rd["ADQECTR3"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT3"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO3"]); return !string.IsNullOrEmpty(gl); }
        // 4
        tr = rd["ADQECTR4"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT4"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO4"]); return !string.IsNullOrEmpty(gl); }
        // 5
        tr = rd["ADQECTR5"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT5"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO5"]); return !string.IsNullOrEmpty(gl); }
        // 6
        tr = rd["ADQECTR6"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT6"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO6"]); return !string.IsNullOrEmpty(gl); }
        // 7
        tr = rd["ADQECTR7"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT7"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO7"]); return !string.IsNullOrEmpty(gl); }
        // 8
        tr = rd["ADQECTR8"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT8"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO8"]); return !string.IsNullOrEmpty(gl); }
        // 9
        tr = rd["ADQECTR9"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT9"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECCO9"]); return !string.IsNullOrEmpty(gl); }
        // 10
        tr = rd["ADQECTR10"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT10"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC10"]); return !string.IsNullOrEmpty(gl); }
        // 11
        tr = rd["ADQECTR11"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT11"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC11"]); return !string.IsNullOrEmpty(gl); }
        // 12
        tr = rd["ADQECTR12"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT12"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC12"]); return !string.IsNullOrEmpty(gl); }
        // 13
        tr = rd["ADQECTR13"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT13"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC13"]); return !string.IsNullOrEmpty(gl); }
        // 14
        tr = rd["ADQECTR14"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT14"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC14"]); return !string.IsNullOrEmpty(gl); }
        // 15
        tr = rd["ADQECTR15"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQECNT15"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQECC15"]); return !string.IsNullOrEmpty(gl); }

        return false;
    }

    /// <summary>
    /// Busca en ADQCTL (control/secuencia) la GL/CC cuyo T-code (ADQCTR1..15) coincida con <paramref name="tcodeBuscado"/>.
    /// </summary>
    private bool TryGetGLFromAdqCtl(string control, int secuencia, string tcodeBuscado, out string gl, out int cc)
    {
        gl = ""; cc = 0;
        var q = QueryBuilder.Core.QueryBuilder
            .From("ADQCTL", "BCAH96DTA")
            .Select("*")
            .WhereRaw($"ADQCONT = '{control}'")
            .WhereRaw($"ADQNUM = {secuencia}")
            .FetchNext(1)
            .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return false;

        string tr;
        // mismo patrón que ADQECTL, sin bucles
        tr = rd["ADQCTR1"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT1"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO1"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR2"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT2"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO2"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR3"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT3"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO3"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR4"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT4"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO4"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR5"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT5"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO5"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR6"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT6"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO6"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR7"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT7"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO7"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR8"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT8"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO8"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR9"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT9"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCCO9"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR10"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT10"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC10"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR11"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT11"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC11"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR12"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT12"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC12"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR13"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT13"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC13"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR14"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT14"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC14"]); return !string.IsNullOrEmpty(gl); }

        tr = rd["ADQCTR15"]?.ToString()?.Trim() ?? "";
        if (tr == tcodeBuscado) { gl = rd["ADQCNT15"]?.ToString()?.Trim() ?? ""; cc = SafeToInt(rd["ADQCC15"]); return !string.IsNullOrEmpty(gl); }

        return false;
    }

    private static int SafeToInt(object? o)
        => o is null || o is DBNull ? 0 : Convert.ToInt32(o, CultureInfo.InvariantCulture);


    /// <summary>
    /// Ejecuta un programa RPG INT_LOTES con los 35 parámetros exactos.
    /// </summary>
    /// <param name="parametros">Parametros</param>
    ///<param name="tipoCuenta">Tipo de cuenta (1-ahorros/6-cheques/40-Contable).</param>
    ///<param name="numeroCuenta">Número de Cuenta a Debitar/Acredita.r</param>
    ///<param name="monto">Monto a Debitar/Acreditar.</param>
    ///<param name="naturalezaContable">Naturaleza Contable Debito o Credito  D ó C.</param>
    ///<param name="centroCosto">Centro de costo (162 para POS).</param>
    /// <param name="perfil">Perfil transerver.</param>
    /// <param name="moneda">Código de moneda.</param>
    /// <param name="descripcion1">Leyenda 1.</param>
    /// <param name="descripcion2">Leyenda 2.</param>
    /// <param name="descripcion3">Leyenda 3.</param>
    /// <returns>(CodigoError, DescripcionError)</returns>
    public async Task<(int CodigoErrorPosteo, string? DescripcionErrorPosteo, string? nomArc)> PosteoLoteAsync(
    IntLotesParamsDto parametros,
    decimal tipoCuenta,          // CUENTA DEL CLIENTE
    decimal numeroCuenta,        // CUENTA DEL CLIENTE
    decimal monto,
    string naturalezaContable,   // "D" o "C"
    decimal centroCosto,         // por defecto (p.ej., 162)
    decimal moneda,              // por defecto
    string perfil,
    string descripcion1,         // texto base cliente
    string descripcion2,
    string descripcion3
)
    {
        try
        {
            bool isDebCliente = naturalezaContable?.Trim().Equals("D", StringComparison.OrdinalIgnoreCase) == true;

            // ---- Datos lado interno (con defaults si no vienen) ----
            decimal tpoInterno = parametros.TipoCuentaInterna ?? 40m;  // 40=contable por defecto
            decimal ctaInterna = Convert.ToDecimal( parametros.TipoCuentaInterna);   // DEBE VENIR para postear
            decimal ccosInterno = Convert.ToDecimal(parametros.CentroCostoInterno);
            decimal monInterno = parametros.MonedaInterna ?? moneda;

            // ---- Descripciones (permite overrides desde el DTO) ----
            string cli1 = parametros.Des001 ?? descripcion1;//cliente por defecto
            string cli2 = parametros.Des002 ?? descripcion2;
            string cli3 = parametros.Des003 ?? descripcion3;

            string int1 = parametros.Des001 ?? descripcion1; //internas por defecto iguales a cliente
            string int2 = parametros.Des002 ?? descripcion2;
            string int3 = parametros.Des003 ?? descripcion3;

            var builder = ProgramCallBuilder.For(_connection, "BCAH96", "INT_LOTES")
                                            .UseSqlNaming()
                                            .WrapCallWithBraces();

            if (isDebCliente)
            {
                // =========================================================
                // NATURALEZA = "D"  → Cliente DEBITO / Interno CREDITO
                //  Mov.1 = Cliente (D)
                //  Mov.2 = Interno (C)
                //  DESDBx = Cliente   (debito)
                //  DESCRx = Internas  (crédito)
                // =========================================================

                // ---------- Movimiento 1 (Cliente - Débito) ----------
                builder.InDecimal("PMTIPO01", tipoCuenta, precision: 2, scale: 0);
                builder.InDecimal("PMCTAA01", numeroCuenta, precision: 13, scale: 0);
                builder.InDecimal("PMVALR01", monto, precision: 13, scale: 2);
                builder.InChar("PMDECR01", "D", 1);
                builder.InDecimal("PMCCOS01", 0m, precision: 5, scale: 0);
                builder.InDecimal("PMMONE01", moneda, precision: 3, scale: 0);

                // ---------- Movimiento 2 (Interno - Crédito) ----------
                builder.InDecimal("PMTIPO02", tpoInterno, precision: 2, scale: 0);
                builder.InDecimal("PMCTAA02", ctaInterna, precision: 13, scale: 0);
                builder.InDecimal("PMVALR02", monto, precision: 13, scale: 2);
                builder.InChar("PMDECR02", "C", 1);
                builder.InDecimal("PMCCOS02", ccosInterno, precision: 5, scale: 0);
                builder.InDecimal("PMMONE02", monInterno, precision: 3, scale: 0);

                // ===================== Movimiento 3 =====================
                builder.InDecimal("PMTIPO03", 0m, precision: 2, scale: 0);
                builder.InDecimal("PMCTAA03", 0m, precision: 13, scale: 0);
                builder.InDecimal("PMVALR03", 0m, precision: 13, scale: 2);
                builder.InChar("PMDECR03", "", 1);
                builder.InDecimal("PMCCOS03", 0m, precision: 5, scale: 0);
                builder.InDecimal("PMMONE03", 0m, precision: 3, scale: 0); //Moneda del movimiento

                // ===================== Movimiento 4 =====================
                builder.InDecimal("PMTIPO04", 0m, precision: 2, scale: 0);
                builder.InDecimal("PMCTAA04", 0m, precision: 13, scale: 0);
                builder.InDecimal("PMVALR04", 0m, precision: 13, scale: 2);
                builder.InChar("PMDECR04", "", 1);
                builder.InDecimal("PMCCOS04", 0m, precision: 5, scale: 0);
                builder.InDecimal("PMMONE04", 0m, precision: 3, scale: 0); //Moneda del movimiento

                // ===================== Generales =====================
                builder.InChar("PMPERFIL", perfil, 13); //Perfil transerver
                builder.InDecimal("MONEDA", moneda, precision: 3, scale: 0);

                // ---------- Descripciones ----------
                // Nuevas (DESDBx) = las del débito → cliente
                builder.InChar("DESDB1", cli1, 40);
                builder.InChar("DESDB2", cli2, 40);
                builder.InChar("DESDB3", cli3, 40);

                // Originales (DESCRx) = las del crédito → interno
                builder.InChar("DESCR1", int1, 40);
                builder.InChar("DESCR2", int2, 40);
                builder.InChar("DESCR3", int3, 40);
            }
            else
            {
                // =========================================================
                // NATURALEZA = "C"  → Cliente CREDITO / Interno DEBITO
                //  Mov.1 = Interno (D)
                //  Mov.2 = Cliente (C)
                //  DESDBx = Internas (debito)
                //  DESCRx = Cliente  (crédito)
                // =========================================================

                // ---------- Movimiento 1 (Interno - Débito) ----------
                builder.InDecimal("PMTIPO01", tpoInterno, precision: 2, scale: 0);
                builder.InDecimal("PMCTAA01", ctaInterna, precision: 13, scale: 0);
                builder.InDecimal("PMVALR01", monto, precision: 13, scale: 2);
                builder.InChar("PMDECR01", "D", 1);
                builder.InDecimal("PMCCOS01", ccosInterno, precision: 5, scale: 0);
                builder.InDecimal("PMMONE01", monInterno, precision: 3, scale: 0);

                // ---------- Movimiento 2 (Cliente - Crédito) ----------
                builder.InDecimal("PMTIPO02", tipoCuenta, precision: 2, scale: 0);
                builder.InDecimal("PMCTAA02", numeroCuenta, precision: 13, scale: 0);
                builder.InDecimal("PMVALR02", monto, precision: 13, scale: 2);
                builder.InChar("PMDECR02", "C", 1);
                builder.InDecimal("PMCCOS02", centroCosto, precision: 5, scale: 0);
                builder.InDecimal("PMMONE02", moneda, precision: 3, scale: 0);


                // ===================== Movimiento 3 =====================
                builder.InDecimal("PMTIPO03", 0m, precision: 2, scale: 0);
                builder.InDecimal("PMCTAA03", 0m, precision: 13, scale: 0);
                builder.InDecimal("PMVALR03", 0m, precision: 13, scale: 2);
                builder.InChar("PMDECR03", "", 1);
                builder.InDecimal("PMCCOS03", 0m, precision: 5, scale: 0);
                builder.InDecimal("PMMONE03", 0m, precision: 3, scale: 0); //Moneda del movimiento

                // ===================== Movimiento 4 =====================
                builder.InDecimal("PMTIPO04", 0m, precision: 2, scale: 0);
                builder.InDecimal("PMCTAA04", 0m, precision: 13, scale: 0);
                builder.InDecimal("PMVALR04", 0m, precision: 13, scale: 2);
                builder.InChar("PMDECR04", "", 1);
                builder.InDecimal("PMCCOS04", 0m, precision: 5, scale: 0);
                builder.InDecimal("PMMONE04", 0m, precision: 3, scale: 0); //Moneda del movimiento

                // ===================== Generales =====================
                builder.InChar("PMPERFIL", perfil, 13); //Perfil transerver
                builder.InDecimal("MONEDA", moneda, precision: 3, scale: 0);

                // ---------- Descripciones ----------
                // Nuevas (DESDBx) = las del débito → internas
                builder.InChar("DESDB1", int1, 40);
                builder.InChar("DESDB2", int2, 40);
                builder.InChar("DESDB3", int3, 40);

                // Originales (DESCRx) = las del crédito → cliente
                builder.InChar("DESCR1", cli1, 40);
                builder.InChar("DESCR2", cli2, 40);
                builder.InChar("DESCR3", cli3, 40);
            }

            builder.OutDecimal("CODER", 2, 0);
            builder.OutChar("DESERR", 70);
            builder.OutChar("NomArc", 10);

            var result = await builder.CallAsync(_contextAccessor.HttpContext);

            result.TryGet("CODER", out int codigoError);
            result.TryGet("DESERR", out string? descripcionError);
            result.TryGet("NomArc", out string? nomArc);

            return (codigoError, descripcionError, nomArc);
        }
        catch (Exception ex)
        {
            return (-1, "Error general en PosteoLoteAsync: " + ex.Message, "");
        }
    }

    /// <summary>
    /// Método para agregar librerías a la LIBL de la conexión actual.
    /// </summary>
    /// <returns>(bool agregoLibrerias, string descripcionErrorLibrerias)</returns>
    private (bool agregoLibrerias, string descripcionErrorLibrerias) CargaLibrerias()
    {
        try
        {
            // Lista completa que quieres dejar en LIBL (ajusta el orden a tu necesidad)
            var libl = "QTEMP ICBS BCAH96 BCAH96DTA BNKPRD01 QGPL GX COVENPGMV4";

            // Comando CL en un SOLO statement
            var clCmd = $"CHGLIBL LIBL({libl})";

            // Longitud para QCMDEXC = número de caracteres del comando, con escala 5
            static decimal QcmdexcLen(string s) => Convert.ToDecimal(s.Length.ToString() + ".00000", CultureInfo.InvariantCulture);

            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = "CALL QSYS2.QCMDEXC(?, ?)";
            var p1 = cmd.CreateParameter(); p1.DbType = System.Data.DbType.String; p1.Value = clCmd; cmd.Parameters.Add(p1);
            var p2 = cmd.CreateParameter(); p2.DbType = System.Data.DbType.Decimal; p2.Precision = 15; p2.Scale = 5; p2.Value = QcmdexcLen(clCmd); cmd.Parameters.Add(p2);

            cmd.ExecuteNonQuery();

            return (true, "Se agregaron las librias");
        }
        catch (DbException ex)
        {
            return (false, $"Error al agregar librerías a LIBL. : {ex.Message}");
        }
    }

    /// <summary>
    /// Crea un DTO de respuesta de error con metadatos consistentes.
    /// </summary>
    private static RespuestaGuardarTransaccionesDto BuildError(string code, string message)
        => new()
        {
            CodigoError = code,
            DescripcionError = message
        };
}





