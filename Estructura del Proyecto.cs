Este es mi código actualmente:

using Connections.Abstractions;
using Connections.Helpers;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BCAH96DTA;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BNKPRD01;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Utils;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
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

    /// <inheritdoc/>
    public async Task<RespuestaGuardarTransaccionesDto> GuardarTransaccionesAsync(GuardarTransaccionesDto guardarTransaccionesDto)
    {
        await Task.Yield(); // Simula asincronía para cumplir con la firma async.
        //Procesos Previos

        _connection.Open(); //Abrimos la conexión a la base de datos

        // Validamos que la conexión esté activa, si no, retornamos error y no continuamos con el proceso.
        if (!_connection.IsConnected) return BuildError(BizCodes.ConexionDbFallida, "No fue posible establecer conexión con la base de datos.");

        //============================Validaciones Previas============================//

        // Normalización de importes: tolera "." o "," y espacios
        var deb = Utilities.ParseMonto(guardarTransaccionesDto.MontoDebitado);
        var cre = Utilities.ParseMonto(guardarTransaccionesDto.MontoAcreditado);

        //Validamos que al menos uno de los montos sea mayor a 0, no se puede postear ambos en 0.
        if (deb <= 0m && cre <= 0m) return BuildError(BizCodes.NoImportes, "No hay importes a postear (ambos montos son 0).");

        //Validamos que no se posteen ambos montos mayores a 0 a la vez.
        if (deb > 0m && cre > 0m) return BuildError(BizCodes.MontosIgualesMayores, "No se puede postear ambos montos (débito y crédito) mayores a cero a la vez.");

        //Obtenemos perfil transerver de la configuración global
        string perfilTranserver = GlobalConnection.Current.PerfilTranserver;

        //Validamos, si no hay perfil transerver, retornamos error porque el proceso no puede continuar.
        if (perfilTranserver.IsNullOrEmpty()) return BuildError(BizCodes.PerfilNoExiste, "No se ha configurado el perfil transerver a buscar en JSON.");

        //Validamos si existe el comercio en la tabla BCAH96DTA/IADQCOM
        var (existeComercio, codigoError, mensajeComercio) = BuscarComercio(guardarTransaccionesDto.NumeroCuenta, int.Parse(guardarTransaccionesDto.CodigoComercio));
        if (!existeComercio) return BuildError(codigoError, mensajeComercio);

        //Validamos si existe la terminal en la tabla BCAH96DTA/ADQ03TER
        var (existeTerminal, esTerminalVirtual, codigoErrorTerminal, mensajeTerminal) = BuscarTerminal(guardarTransaccionesDto.Terminal, int.Parse(guardarTransaccionesDto.CodigoComercio));
        if (!existeTerminal) return BuildError(codigoErrorTerminal, mensajeTerminal);

        //============================Fin Validaciones Previas============================//

        //============================Inicia Proceso Principal============================//

        // 1. Obtenemos si la terminal es virtual (e-commerce) o física.
        var respuestaPerfil = VerPerfil(perfilTranserver);

        // Si no existe el perfil, retornar error y no continuar con el proceso.
        if (!respuestaPerfil.existePerfil) return BuildError(respuestaPerfil.codigoError, respuestaPerfil.descripcionError);

        //2. Cargamos las librerías necesarias en la LIBL de la conexión actual.
        var (agregoLibrerias, codigoErrorLibrerias, descripcionErrorLibrerias) = CargaLibrerias(); // Asegura que las librerías necesarias estén en el entorno de ejecución
        //Validamos si se agregaron las librerías, si no, retornamos error y no continuamos con el proceso.
        if (!agregoLibrerias) return BuildError(codigoErrorLibrerias, descripcionErrorLibrerias);

        // 3. Resolvemos los parámetros necesarios para llamar a Int_lotes.
        var p = ResolverParametrosIntLotes(
            esEcommerce: esTerminalVirtual, // true si es e-commerce (terminal virtual)
            perfil: perfilTranserver, // Perfil transerver (CFTSKY)
            naturalezaCliente: guardarTransaccionesDto.NaturalezaContable,                 // 'C' o 'D'
            numeroCuenta: guardarTransaccionesDto.NumeroCuenta, //Del cliente/comercio
            codigoComercio: guardarTransaccionesDto.CodigoComercio, //Código del cliente/comercio
            monedaIsoNum: 0,                         // si tu RPG lo espera, envía el ISO num correspondiente
            monto: guardarTransaccionesDto.NaturalezaContable.Equals("D") ? deb : cre                         // monto a postear (mismo en ambos lados)
        );

        // 4. Ejecutamos el programa INT_LOTES con los parámetros necesarios.
        Task<(string CodigoErrorPosteo, string DescripcionErrorPosteo, string? nomArc)> respuesta = PosteoLoteAsync(p);

        _connection.Close(); //Cerramos la conexión a la base de datos
        //============================Fin Proceso Principal============================//
        return BuildError(respuesta.Result.CodigoErrorPosteo, respuesta.Result.DescripcionErrorPosteo);
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
                return (true, BizCodes.Ok, "Existe Comercio."); // Coemrcio existe
            }
            return (false, BizCodes.ComercioNoExiste, "No existe Comercio."); // Comercio no existe
        }
        catch (SqlException ex)
        {
            return (false, BizCodes.ErrorSql, ex.Message); // Indica error al consultar comercio
        }
        catch (Exception ex)
        {
            return (false, BizCodes.ErrorDesconocido, ex.Message); // Indica error desconocido al consultar comercio
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
                return (true, EsTerminalVirtual(terminal), BizCodes.Ok, "Existe terminal."); // Terminal existe
            }
            return (false, false, BizCodes.TerminalNoExiste, "No existe terminal."); // Terminal no existe
        }
        catch (SqlException ex)
        {
            return (false, false, BizCodes.ErrorSql, ex.Message); //Indica error en SQL al Consultar Terminal
        }
        catch (Exception ex)
        {
            return (false, false, BizCodes.ErrorDesconocido, ex.Message); // Indica error desconocido al consultar terminal
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
                return (true, BizCodes.Ok, "Existe Perfil Transerver."); // Perfil existe
            }
            return (false, BizCodes.PerfilNoExiste, "No existe Perfil Transerver."); // Perfil no existe
        }
        catch (SqlException ex)
        {
            // Manejo de errores SQL
            return (false, BizCodes.ErrorSql, "Error SQL: " + ex.Message); //Indica error sql al consultar perfil
        }
        catch (Exception ex)
        {
            // Manejo de errores en la consulta
            return (false, BizCodes.ErrorDesconocido, "Error general: " + ex.Message); // Indica error desconocido al consultar perfil
        }
    }

    /// <summary>
    /// Resuelve todos los parámetros necesarios para INT_LOTES:
    /// - Calcula t-codes, obtiene ADQNUM y, si aplica, GL/CC en UNA sola consulta (ADQECTL o ADQCTL).
    /// - Aplica auto-balance (CFP801) si está activo para el perfil.
    /// - Arma las descripciones EXACTAS como en el RPG (40 chars).
    /// - Construye ambos movimientos: Mov1 = Débito, Mov2 = Crédito, según la naturaleza del cliente.
    ///<param name="esEcommerce">Indica si la transacción es de e-commerce (terminal virtual).</param>
    ///<param name="perfil">Perfil transerver (CFTSKY).</param>"
    ///<param name="naturalezaCliente">Naturaleza Contable a aplicar.</param>
    ///<param name="numeroCuenta">Número de cuenta del cliente.</param>
    ///<param name="codigoComercio">Código del comercio.</param>
    ///<param name="monedaIsoNum">Moneda de la cuenta.</param>
    ///<param name="monto">Valor monto acreditar o debitar.</param>
    /// </summary>
    private IntLotesParamsDto ResolverParametrosIntLotes(
        bool esEcommerce,
        string perfil,
        string naturalezaCliente, // 'C' (acreditamos cliente) o 'D' (debitamos cliente)
        string numeroCuenta,      // cuenta del cliente
        string codigoComercio,
        int monedaIsoNum,                     // 0 si no aplica ISO; de lo contrario ej. 340/840
        decimal monto                         // monto a postear (mismo en ambos lados)
    )
    {
        var dto = new IntLotesParamsDto
        {
            Perfil = perfil,
            Moneda = monedaIsoNum,
            TasaTm = 0m,
            ErrorMetodo = 0
        };

        // ---------------------------------------------------------
        // 1) Determinar t-codes (cliente vs GL) según naturaleza
        // ---------------------------------------------------------
        string tcodeCliente = naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase) ? "0783" : "0784";
        string tcodeGL = naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase) ? "0784" : "0783";

        // ---------------------------------------------------------
        // 2) Obtener ADQNUM + (GL/CC) en UNA consulta (control EC/GL)
        // ---------------------------------------------------------
        var (adqNumCtl, glCtl, ccCtl) = ObtenerAdqNumYGL(esEcommerce, tcodeGL);

        // ---------------------------------------------------------
        // 3) Auto-balance (CFP801) o control (ADQECTL/ADQCTL)
        // ---------------------------------------------------------
        var (enabled, glDeb, ccDeb, glCre, ccCre) = TryGetAutoBalance(perfil);
        dto.EsAutoBalance = enabled;

        string? glCuenta;
        int glCC;
        if (enabled)
        {
            // Cliente 'C' → interno debe ir a Débito; Cliente 'D' → interno a Crédito
            if (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase))
            {
                // Débito interno
                glCuenta = glDeb; glCC = ccDeb;
            }
            else
            {
                // Crédito interno
                glCuenta = glCre; glCC = ccCre;
            }
            dto.FuenteGL = "CFP801";
        }
        else
        {
            // Usar control EC/GL
            glCuenta = glCtl; glCC = ccCtl; dto.FuenteGL = esEcommerce ? "ADQECTL" : "ADQCTL";
        }

        // ---------------------------------------------------------
        // 4) Tipo de cuenta del cliente (Ahorros/Cheques) vía Ver_cta
        // ---------------------------------------------------------
        var infoCta = VerCta(numeroCuenta);

        decimal tipoClienteDec;

        // Validar que la cuenta exista
        if (infoCta.EsAhorro)//Es cuenta de ahorros
        {
            tipoClienteDec = 1; //Cuentas de Ahorros son tipo 1
        }
        else if (infoCta.EsCheques) //Es cuenta de cheques
        {
            tipoClienteDec = 6; // Cuentas de Cheques son tipo 6
        }
        else // Otros tipos (p. ej., contable)
        {
            tipoClienteDec = 40; // Contable (otros casos)
        }

        // ---------------------------------------------------------
        // 5) Tasa (si tu RPG la usa). De lo contrario, queda 0.
        // ---------------------------------------------------------
        try { dto.TasaTm = ObtenerTasaCompraUsd(); } catch { dto.TasaTm = 0m; }

        // ---------------------------------------------------------
        // 6) Descripciones EXACTAS del RPG (40 chars; usa ADQNUM)
        // ---------------------------------------------------------
        string concepto = "VTA";
        string fechaFormateada = DateTime.Now.ToString("yyyyMMdd");
        string ochoEspacios = "        ";
        string adqNumPadded = (adqNumCtl ?? "0").PadRight(10); // anchura usada por el RPG

        string desDb1 = Trunc("Total Neto Db liquidacion come", 40);
        string desCr1 = Trunc("Total Neto Cr liquidacion come", 40);
        string descDb2 = Trunc($"{codigoComercio}{ochoEspacios}-{fechaFormateada}-{tipoClienteDec}-{numeroCuenta}", 40);
        string descCr2 = Trunc($"{codigoComercio}{ochoEspacios}-{fechaFormateada}", 40);
        string descDb3 = Trunc($"&{concepto}&{adqNumPadded}Db Net.Liq1  ||", 40);
        string descCr3 = Trunc($"&{concepto}&{adqNumPadded}Cr Net.Liq2  ||", 40);

        // ---------------------------------------------------------
        // 7) Construir ambos movimientos (Mov1 = Débito, Mov2 = Crédito)
        //    Reglas:
        //    - Si naturalezaCliente = 'C' (acreditamos cliente):
        //        Mov1: Débito interno (GL)
        //        Mov2: Crédito cliente
        //    - Si naturalezaCliente = 'D' (debitamos cliente):
        //        Mov1: Débito cliente
        //        Mov2: Crédito interno (GL)
        // ---------------------------------------------------------
        static decimal ToDecOrZero(string? s)
            => decimal.TryParse((s ?? "").Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

        dto.Perfil = perfil;
        dto.Moneda = monedaIsoNum;
        dto.TasaTm = 0m;

        if (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase))
        {
            // Mov1 = DB interno (GL)
            dto.TipoMov1 = 40;
            dto.CuentaMov1 = ToDecOrZero(glCuenta);
            dto.DeCr1 = "D";
            dto.CentroCosto1 = glCC;
            dto.MontoMov1 = monto;
            dto.MonedaMov1 = monedaIsoNum;

            // Mov2 = CR cliente
            dto.TipoMov2 = tipoClienteDec;
            dto.CuentaMov2 = ToDecOrZero(numeroCuenta);
            dto.DeCr2 = "C";
            dto.CentroCosto2 = 0m;
            dto.MontoMov2 = monto;
            dto.MonedaMov2 = monedaIsoNum;

            // Descripciones: DESDB* (lado debitado = interno), DESCR* (lado acreditado = cliente)
            dto.DesDB1 = desDb1; dto.DesDB2 = descDb2; dto.DesDB3 = descDb3;
            dto.DesCR1 = desCr1; dto.DesCR2 = descCr2; dto.DesCR3 = descCr3;

            // Trazabilidad
            dto.NaturalezaCliente = naturalezaCliente;
            dto.NaturalezaGL = naturalezaCliente == "C" ? 'D' : 'C';
            dto.TcodeCliente = tcodeCliente;
            dto.TcodeGL = tcodeGL;
            dto.CuentaClienteOriginal = numeroCuenta;
            dto.CuentaGLResuelta = glCuenta ?? string.Empty;
        }
        else
        {
            // Mov1 = DB cliente
            dto.TipoMov1 = tipoClienteDec;
            dto.CuentaMov1 = ToDecOrZero(numeroCuenta);
            dto.DeCr1 = "D";
            dto.CentroCosto1 = 0;
            dto.MontoMov1 = monto;
            dto.MonedaMov1 = monedaIsoNum;

            // Mov2 = CR interno (GL)
            dto.TipoMov2 = 40;
            dto.CuentaMov2 = ToDecOrZero(glCuenta);
            dto.DeCr2 = "C";
            dto.CentroCosto2 = glCC;
            dto.MontoMov2 = monto;
            dto.MonedaMov2 = monedaIsoNum;

            // Descripciones: DESDB* (lado debitado = cliente), DESCR* (lado acreditado = interno)
            dto.DesDB1 = desDb1; dto.DesDB2 = descDb2; dto.DesDB3 = descDb3;
            dto.DesCR1 = desCr1; dto.DesCR2 = descCr2; dto.DesCR3 = descCr3;
            // Trazabilidad
            dto.NaturalezaCliente = naturalezaCliente;
            dto.NaturalezaGL = naturalezaCliente == "C" ? 'D' : 'C';
            dto.TcodeCliente = tcodeCliente;
            dto.TcodeGL = tcodeGL;
            dto.CuentaClienteOriginal = numeroCuenta;
            dto.CuentaGLResuelta = glCuenta ?? string.Empty;
        }

        // ---------------------------------------------------------
        // 8) Validación mínima: GL obligatoria para el lado interno
        // ---------------------------------------------------------
        bool glFaltante =
            (naturalezaCliente.Equals("C", StringComparison.OrdinalIgnoreCase) && dto.CuentaMov1 == 0m)
         || (naturalezaCliente.Equals("D", StringComparison.OrdinalIgnoreCase) && dto.CuentaMov2 == 0m);

        if (glFaltante)
        {
            dto.ErrorMetodo = 1;
            string errorbase = $"No se encontró cuenta interna (GL) para tcode {tcodeGL} en ";

            if (dto.FuenteGL == "CFP801")
            {
                dto.DescripcionError = errorbase + "CFP801.";
                if (esEcommerce)
                {
                    dto.DescripcionError = errorbase + "ADQECTL";
                }
                else
                {
                    dto.DescripcionError = errorbase + "ADQCTL";
                }
            }
        }
        return dto;
    }

    /// <summary>
    /// Obtiene en UNA SOLA CONSULTA el ADQNUM/ADQENUM y, si corresponde,
    /// la cuenta GL (CNTk) y el centro de costo (CCOk) cuyo T-CODE (CTRk) coincide con <paramref name="tcodeGL"/>.
    /// - Si <paramref name="esEcommerce"/> = true, consulta ADQECTL (CONTROL='EC').
    /// - Si es false, consulta ADQCTL (CONTROL='GL').
    /// Devuelve: (AdqNum, GlCuenta, GlCC).
    /// <param name="esEcommerce">Indica si es Comercio virtual o tradicional.</param>
    /// <param name="tcodeGL">T-code a buscar (ej. '0784' o '0783').</param>"
    /// </summary>
    /// <returns>Tupla (AdqNum, GlCuenta, GlCC)</returns>
    private (string AdqNum, string? GlCuenta, int GlCC) ObtenerAdqNumYGL(bool esEcommerce, string tcodeGL)
    {
        // --- 1) Armar SELECT único con alias estándar (CTRn, CNTn, CCOn, ADQNUM) ---
        var q = esEcommerce
            ? QueryBuilder.Core.QueryBuilder
                .From("ADQECTL", "BCAH96DTA")
                .Select(
                    "ADQENUM AS ADQNUM",
                    "ADQECTR1 AS CTR1", "ADQECNT1 AS CNT1", "ADQECCO1 AS CCO1",
                    "ADQECTR2 AS CTR2", "ADQECNT2 AS CNT2", "ADQECCO2 AS CCO2",
                    "ADQECTR3 AS CTR3", "ADQECNT3 AS CNT3", "ADQECCO3 AS CCO3",
                    "ADQECTR4 AS CTR4", "ADQECNT4 AS CNT4", "ADQECCO4 AS CCO4",
                    "ADQECTR5 AS CTR5", "ADQECNT5 AS CNT5", "ADQECCO5 AS CCO5",
                    "ADQECTR6 AS CTR6", "ADQECNT6 AS CNT6", "ADQECCO6 AS CCO6",
                    "ADQECTR7 AS CTR7", "ADQECNT7 AS CNT7", "ADQECCO7 AS CCO7",
                    "ADQECTR8 AS CTR8", "ADQECNT8 AS CNT8", "ADQECCO8 AS CCO8",
                    "ADQECTR9 AS CTR9", "ADQECNT9 AS CNT9", "ADQECCO9 AS CCO9",
                    "ADQECTR10 AS CTR10", "ADQECNT10 AS CNT10", "ADQECC10 AS CCO10",
                    "ADQECTR11 AS CTR11", "ADQECNT11 AS CNT11", "ADQECC11 AS CCO11",
                    "ADQECTR12 AS CTR12", "ADQECNT12 AS CNT12", "ADQECC12 AS CCO12",
                    "ADQECTR13 AS CTR13", "ADQECNT13 AS CNT13", "ADQECC13 AS CCO13",
                    "ADQECTR14 AS CTR14", "ADQECNT14 AS CNT14", "ADQECC14 AS CCO14",
                    "ADQECTR15 AS CTR15", "ADQECNT15 AS CNT15", "ADQECC15 AS CCO15"
                )
                .WhereRaw(
                    "ADQECONT = 'EC' AND (" +
                    "ADQECTR1 = @T OR ADQECTR2 = @T OR ADQECTR3 = @T OR ADQECTR4 = @T OR ADQECTR5 = @T OR " +
                    "ADQECTR6 = @T OR ADQECTR7 = @T OR ADQECTR8 = @T OR ADQECTR9 = @T OR ADQECTR10 = @T OR " +
                    "ADQECTR11 = @T OR ADQECTR12 = @T OR ADQECTR13 = @T OR ADQECTR14 = @T OR ADQECTR15 = @T)"
                )
                .FetchNext(1)
                .Build()
            : QueryBuilder.Core.QueryBuilder
                .From("ADQCTL", "BCAH96DTA")
                .Select(
                    "ADQNUM AS ADQNUM",
                    "ADQCTR1 AS CTR1", "ADQCNT1 AS CNT1", "ADQCCO1 AS CCO1",
                    "ADQCTR2 AS CTR2", "ADQCNT2 AS CNT2", "ADQCCO2 AS CCO2",
                    "ADQCTR3 AS CTR3", "ADQCNT3 AS CNT3", "ADQCCO3 AS CCO3",
                    "ADQCTR4 AS CTR4", "ADQCNT4 AS CNT4", "ADQCCO4 AS CCO4",
                    "ADQCTR5 AS CTR5", "ADQCNT5 AS CNT5", "ADQCCO5 AS CCO5",
                    "ADQCTR6 AS CTR6", "ADQCNT6 AS CNT6", "ADQCCO6 AS CCO6",
                    "ADQCTR7 AS CTR7", "ADQCNT7 AS CNT7", "ADQCCO7 AS CCO7",
                    "ADQCTR8 AS CTR8", "ADQCNT8 AS CNT8", "ADQCCO8 AS CCO8",
                    "ADQCTR9 AS CTR9", "ADQCNT9 AS CNT9", "ADQCCO9 AS CCO9",
                    "ADQCTR10 AS CTR10", "ADQCNT10 AS CNT10", "ADQCC10 AS CCO10",
                    "ADQCTR11 AS CTR11", "ADQCNT11 AS CNT11", "ADQCC11 AS CCO11",
                    "ADQCTR12 AS CTR12", "ADQCNT12 AS CNT12", "ADQCC12 AS CCO12",
                    "ADQCTR13 AS CTR13", "ADQCNT13 AS CNT13", "ADQCC13 AS CCO13",
                    "ADQCTR14 AS CTR14", "ADQCNT14 AS CNT14", "ADQCC14 AS CCO14",
                    "ADQCTR15 AS CTR15", "ADQCNT15 AS CNT15", "ADQCC15 AS CCO15"
                )
                .WhereRaw(
                    " (" +
                    "ADQCTR1 = @T OR ADQCTR2 = @T OR ADQCTR3 = @T OR ADQCTR4 = @T OR ADQCTR5 = @T OR " +
                    "ADQCTR6 = @T OR ADQCTR7 = @T OR ADQCTR8 = @T OR ADQCTR9 = @T OR ADQCTR10 = @T OR " +
                    "ADQCTR11 = @T OR ADQCTR12 = @T OR ADQCTR13 = @T OR ADQCTR14 = @T OR ADQCTR15 = @T)"
                )
                .FetchNext(1)
                .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        // Si tu QueryBuilder no soporta parámetros, inyecta el valor literal una sola vez:
        cmd.CommandText = cmd.CommandText.Replace("@T", $"'{tcodeGL}'");

        using var rd = cmd.ExecuteReader();
        if (!rd.Read())
            return ("0", null, 0);

        // --- 2) ADQNUM común ---
        var adqNum = rd.IsDBNull(rd.GetOrdinal("ADQNUM"))
            ? "0"
            : Convert.ToDecimal(rd.GetValue(rd.GetOrdinal("ADQNUM"))).ToString("0");

        // --- 3) Localizar el índice k (1..15) cuyo CTRk == tcodeGL y devolver CNTk/CCOk ---
        string? gl = null; int cc = 0;
        for (int k = 1; k <= 15; k++)
        {
            var ctr = rd.GetString(rd.GetOrdinal($"CTR{k}")).Trim();
            if (string.Equals(ctr, tcodeGL, StringComparison.OrdinalIgnoreCase))
            {
                // CNTk puede ser DECIMAL en DB2; lo leemos como string "plano"
                var cntOrdinal = rd.GetOrdinal($"CNT{k}");
                gl = rd.IsDBNull(cntOrdinal) ? null : Convert.ToString(rd.GetValue(cntOrdinal))?.Trim();

                var ccoOrdinal = rd.GetOrdinal($"CCO{k}");
                cc = rd.IsDBNull(ccoOrdinal) ? 0 : Convert.ToInt32(rd.GetValue(ccoOrdinal));
                break;
            }
        }
        return (adqNum, gl, cc);
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
    /// <returns>Tupla (TipoCuenta, DescCorta, Descripcion, EsAhorro, EsCheques)</returns>
    private VerCtaResult VerCta(string numeroCuenta)
    {
        if (string.IsNullOrWhiteSpace(numeroCuenta))
            return new VerCtaResult { DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };

        //Consulta a TAP002 para obtener DMTYP (tipo de cuenta)
        var qverCtaQuery = new SelectQueryBuilder("TAP00201", "BNKPRD01")
            .Select("DMTYP")
            .WhereRaw($"DMACCT = {numeroCuenta}")
            .FetchNext(1)
            .Build();

        var cmd = _connection.GetDbCommand(qverCtaQuery, _contextAccessor.HttpContext!);
        var rd = cmd.ExecuteReader();

        if (!rd.Read()) return new VerCtaResult { TipoCuenta = 0m, DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };

        int tipoCuenta = rd.IsDBNull(0) ? 0 : Convert.ToInt32(rd.GetValue(0));

        if (tipoCuenta == 1)
            return new VerCtaResult { TipoCuenta = 1m, DescCorta = "AHO", Descripcion = "Ahorros", EsAhorro = true, EsCheques = false };

        if (tipoCuenta == 6)
            return new VerCtaResult { TipoCuenta = 6m, DescCorta = "CHE", Descripcion = "Cheques", EsAhorro = false, EsCheques = true };

        return new VerCtaResult { TipoCuenta = 0m, DescCorta = "", Descripcion = "", EsAhorro = false, EsCheques = false };
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
    /// Obtiene la tasa de compra USD (si aplica). Si aún no tienes
    /// la tabla/fuente, devuelve 0 sin detener el proceso.
    /// </summary>
    static private decimal ObtenerTasaCompraUsd()
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
            //     .Build()
            //
            // using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!)
            // var obj = cmd.ExecuteScalar()
            // return obj is null || obj is DBNull ? 0m : Convert.ToDecimal(obj, CultureInfo.InvariantCulture)

            return 0m; // fallback seguro
        }
        catch
        {
            return 0m; // nunca rompas el flujo por tasa
        }
    }

    /// <summary>
    /// Lee de CFP801 si el perfil genera asiento de balance (CFTSGE=1) y obtiene sus cuentas/CC.
    /// <param name="perfil">Perfil transerver.</param>
    /// </summary>
    /// <returns>Tupla (enabled, glDebito, ccDebito, glCredito, ccCredito)</returns>
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
    /// Ejecuta INT_LOTES con dos movimientos completos (Débito y Crédito) usando un DTO ya resuelto.
    /// - Mov1 = lado DEBITADO (PMTIPO01..PMMONE01 + DESDB1..3)
    /// - Mov2 = lado ACREDITADO (PMTIPO02..PMMONE02 + DESCR1..3)
    /// OUT: CODER, DESERR, NomArc
    /// </summary>
    /// <param name="p">Parámetros ya armados para INT_LOTES (ambos movimientos, descripciones y generales).</param>
    /// <returns>Tupla (CodigoErrorPosteo, DescripcionErrorPosteo, NomArc)</returns>
    public async Task<(string CodigoErrorPosteo, string DescripcionErrorPosteo, string? NomArc)> PosteoLoteAsync(IntLotesParamsDto p)
    {
        try
        {
            var builder = ProgramCallBuilder.For(_connection, "BCAH96", "INT_LOTES")
                .UseSqlNaming()
                .WrapCallWithBraces();

            // ===================== Movimiento 1 (Débito) =====================
            builder.InDecimal("PMTIPO01", p.TipoMov1, precision: 2, scale: 0);
            builder.InDecimal("PMCTAA01", p.CuentaMov1, precision: 13, scale: 0);
            builder.InDecimal("PMVALR01", p.MontoMov1, precision: 13, scale: 2);
            builder.InChar("PMDECR01", string.IsNullOrEmpty(p.DeCr1) ? "D" : p.DeCr1, 1);
            builder.InDecimal("PMCCOS01", p.CentroCosto1, precision: 5, scale: 0);
            builder.InDecimal("PMMONE01", p.MonedaMov1 > 0 ? p.MonedaMov1 : p.Moneda, precision: 3, scale: 0);

            // ===================== Movimiento 2 (Crédito) ====================
            builder.InDecimal("PMTIPO02", p.TipoMov2, precision: 2, scale: 0);
            builder.InDecimal("PMCTAA02", p.CuentaMov2, precision: 13, scale: 0);
            builder.InDecimal("PMVALR02", p.MontoMov2, precision: 13, scale: 2);
            builder.InChar("PMDECR02", string.IsNullOrEmpty(p.DeCr2) ? "C" : p.DeCr2, 1);
            builder.InDecimal("PMCCOS02", p.CentroCosto2, precision: 5, scale: 0);
            builder.InDecimal("PMMONE02", p.MonedaMov2 > 0 ? p.MonedaMov2 : p.Moneda, precision: 3, scale: 0);

            // ===================== Movimiento 3 (vacío) ======================
            builder.InDecimal("PMTIPO03", 0m, precision: 2, scale: 0);
            builder.InDecimal("PMCTAA03", 0m, precision: 13, scale: 0);
            builder.InDecimal("PMVALR03", 0m, precision: 13, scale: 2);
            builder.InChar("PMDECR03", "", 1);
            builder.InDecimal("PMCCOS03", 0m, precision: 5, scale: 0);
            builder.InDecimal("PMMONE03", 0m, precision: 3, scale: 0);

            // ===================== Movimiento 4 (vacío) ======================
            builder.InDecimal("PMTIPO04", 0m, precision: 2, scale: 0);
            builder.InDecimal("PMCTAA04", 0m, precision: 13, scale: 0);
            builder.InDecimal("PMVALR04", 0m, precision: 13, scale: 2);
            builder.InChar("PMDECR04", "", 1);
            builder.InDecimal("PMCCOS04", 0m, precision: 5, scale: 0);
            builder.InDecimal("PMMONE04", 0m, precision: 3, scale: 0);

            // ===================== Generales/Descripciones ===================
            builder.InChar("PMPERFIL", p.Perfil, 13);
            builder.InDecimal("MONEDA", p.Moneda, precision: 3, scale: 0);

            // Lado DEBITADO → DESDBx
            builder.InChar("DESDB1", p.DesDB1 ?? "", 40);
            builder.InChar("DESDB2", p.DesDB2 ?? "", 40);
            builder.InChar("DESDB3", p.DesDB3 ?? "", 40);

            // Lado ACREDITADO → DESCRx
            builder.InChar("DESCR1", p.DesCR1 ?? "", 40);
            builder.InChar("DESCR2", p.DesCR2 ?? "", 40);
            builder.InChar("DESCR3", p.DesCR3 ?? "", 40);

            // ===================== OUT =====================
            builder.OutDecimal("CODER", 2, 0);
            builder.OutChar("DESERR", 70);
            builder.OutChar("NomArc", 10);

            var result = await builder.CallAsync(_contextAccessor.HttpContext);

            result.TryGet("CODER", out int codigoError);
            result.TryGet("DESERR", out string? descripcionError);
            result.TryGet("NomArc", out string? nomArc);

            if (codigoError != 0)
            {
                return (BizCodes.IntLotesFail, $"Error en INT_LOTES: {descripcionError}", nomArc);
            }
            else
            {
                return (BizCodes.Ok, "Posteo de Lote Exitoso", nomArc);
            }
        }
        catch (SqlException ex)
        {
            return (BizCodes.ErrorSql, "Error SQL en PosteoLoteAsync: " + ex.Message, "");
        }
        catch (Exception ex)
        {
            return (BizCodes.ErrorDesconocido, "Error general en PosteoLoteAsync: " + ex.Message, "");
        }
    }

    /// <summary>
    /// Método para agregar librerías a la LIBL de la conexión actual.
    /// </summary>
    /// <returns>Tupla (agregoLibrerias, codigoErrorLibrerias, descripcionErrorLibrerias)</returns>
    private (bool agregoLibrerias, string codigoErrorLibrerias, string descripcionErrorLibrerias) CargaLibrerias()
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

            return (true, BizCodes.Ok, "Se agregaron las librias");
        }
        catch (SqlException ex)
        {
            return (false, BizCodes.ErrorSql, $"Error SQL al agregar librerías a LIBL. : {ex.Message}");
        }
        catch (Exception ex)
        {
            return (false, BizCodes.ErrorDesconocido, $"Error al agregar librerías a LIBL. : {ex.Message}");
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



Dime como se integra y usa la parte de obtener Tasa de cambio, respecto al código antiguo:


      *                                                                       
      /Title Genera Lote de ventas y devoluciones
     H*   ======================================================
     H*                   HCBS                                     *
     H*             ©opyright ®egister                               *
     H* AUTOR......:   Cesar Welchez    13/09/2007                  *
     H*                  Lester Ortega     2024                        *
     H* =============Objetivo=================================
      *====================================================================== *
      * Definiciones
      *====================================================================== *
     FIADQCTL   UF   E           K DISK    Extfile('BCAH96DTA/IADQCTL')
     FADQ00ARC  IF   E           K DISK    Extfile('BCAH96DTA/ADQ00ARC')
     FADQ01ENT  IF   E           K DISK    Extfile('BCAH96DTA/ADQ01ENT')
     FADQ02COM  IF   E           K DISK    Extfile('BCAH96DTA/ADQ02COM')
     FADQ03TER  IF   E           K DISK    Extfile('BCAH96DTA/ADQ03TER')
     FADQ04LOT  IF   E           K DISK    Extfile('BCAH96DTA/ADQ04LOT')
     FADQ10DET  UF   E           K DISK    Extfile('BCAH96DTA/ADQ10DET')
     FADQ13IMP  IF   E           K DISK    Extfile('BCAH96DTA/ADQ13IMP')
     FADQ90LOT  IF   E           K DISK    Extfile('BCAH96DTA/ADQ90LOT')
     FADQ91TER  IF   E           K DISK    Extfile('BCAH96DTA/ADQ91TER')
     FADQ92COM  IF   E           K DISK    Extfile('BCAH96DTA/ADQ92COM')
     FADQ93ENT  IF   E           K DISK    Extfile('BCAH96DTA/ADQ93ENT')
     FADQ94ARC  IF   E           K DISK    Extfile('BCAH96DTA/ADQ94ARC')
     FCFP801    IF   E           K DISK    Extfile('BNKPRD01/CFP801')
     FPOP801    UF A E           K DISK    Extfile('BNKPRD01/POP801')
     FPOP802    IF A E           K DISK    Extfile('BNKPRD01/POP802')
     FCFP753    IF   E           K DISK    Extfile('BNKPRD01/CFP753')
     FTAP001    IF   E           K DISK    Extfile('BNKPRD01/TAP001')
     FCFP102    IF   E           K DISK    Extfile('BNKPRD01/CFP102')
     FTAP002CT  IF   E           K DISK    Extfile('BNKPRD01/TAP002CT')
     FIADQCOM   IF   E           K DISK    Extfile('BCAH96DTA/IADQCOM')
     FBINACE    IF   E           K DISK    Extfile('HONDUNEW/BINACE')
      *//*AGREGADO EL 21/07/2025 MEJORA POR ECOMMERCE
     FADQCOBRO  UF A E           K DISK    Extfile('BCAH96DTA/ADQCOBRO')
     FADQECTL   UF A E           K DISK    Extfile('BCAH96DTA/ADQECTL')
     FADQMEMH   UF A E           K DISK    Extfile('BCAH96DTA/ADQMEMH')
     FGLC002    IF   E           K DISK    Extfile('BNKPRD01/GLC002')
     FADQERRLOG UF A E           K DISK    Extfile('BCAH96DTA/ADQERRLOG')
     F********************************************
     F*
     D FECTIM          C                   CONST('ICBSUSER/FECTIM')
      *
     d @00FILE         s                   LIKE(A00FILE)
     D*
     d W00FILE         s                   LIKE(A00FILE)
     d*
     d W01FILE         s                   LIKE(A01FILE)
     d W01FIID         s                   LIKE(A01FIID)
     d
     d W02FILE         s                   LIKE(A02FILE)
     d W02FIID         s                   LIKE(A02FIID)
     d W02COME         s                   LIKE(A02COME)
     d
     d W03FILE         s                   LIKE(A03FILE)
     d W03FIID         s                   LIKE(A03FIID)
     d W03COME         s                   LIKE(A03COME)
     d W03TERM         s                   LIKE(A03TERM)
     d
     d W02NACO         s                   LIKE(A02NACO)
     d
     d W04FILE         s                   LIKE(A04FILE)
     d W04FIID         s                   LIKE(A04FIID)
     d W04COME         s                   LIKE(A04COME)
     d W04TERM         s                   LIKE(A04TERM)
     d W04LOTE         s                   LIKE(A04LOTE)
     d*
     d W10FILE         s                   LIKE(A10FILE)
     d W10FIID         s                   LIKE(A10FIID)
     d W10COME         s                   LIKE(A10COME)
     d W10TERM         s                   LIKE(A10TERM)
     d W10LOTE         s                   LIKE(A10LOTE)
     d*
     d W92FILE         s                   LIKE(A92FILE)
     d W92FIID         s                   LIKE(A92FIID)
     d W92COME         s                   LIKE(A92COME)
     d W_A10CTPO       s                   LIKE(A10CTPO)
     D*
     D*neto trn
     d W_A10NETD       s                   LIKE(A10NETD)
     D*mto trn
     d W_A10IMTR       s                   LIKE(A10IMTR)
     d* importe de afiliacion
     d W_A10ICDE       s                   LIKE(A10ICDE)
     d* impuesto sibre venta retenido
     d W_A10IVCD       s                   LIKE(A10IVCD)
     d* mto de devolucion
     d W_A10IGCI       s                   LIKE(A10IGCI)
     d* mto de Liquidacion
     d W_A13MOTO       s                   LIKE(A13MOTO)
     d*
     D SSSSS           DS
     D  FECHA                  1      6  0
      *-
     D fecffd          DS
     d fectt                   1      6  0
     D  mestt                  1      2  0
     D  diatt                  3      4  0
     D  anntt                  5      6  0
     D fecgg           DS
     D  anngg                  1      2  0
     D  mesgg                  3      4  0
     D  diagg                  5      6  0
      *-Campos y constantes
     D CondCtaOri      s              1A
     D fecsys          S              8
     D horasys         S              7
     D MsgNoPerfil     s              1
     D wTSWSEQ         s              5  0
     D WTSWSCC         s              5  0
     D codtrn          s              4
     D TIPO            s              2
     D CANTdb          s              5s 0
     D TotalDB         s             12s 2
     D CANTcr          s              5s 0
     D TotalCR         s             12s 2
     D Monto_Trn       s             12s 2
     D ErrorFlag       s              1A   inz('0')
     D Dispo           s             12s 2
     D W3ADQCODP       s              3a   inz('999')
     D HayConta        s              5s 0
     D wCEPNEM         s              3
     D wCEPDSC         s             18
     D wdmbrch         s              3s 0
     D bco             S              3s 0 inz
     D cif             S             10A   inz
     D t_debito        S             12s 2 inz(0.00)
     D t_credito       S             12s 2 inz(0.00)
     D wwdis           S             12s 2 inz(0.00)
     D Cc_comis10      S             10s 0 inz(198)
     D Cc_comis3       S              3s 0 inz(198)
     D X               s              2  0 inz(0)
     D NOTRACh6        S              6A   INZ('000000')
     D NOTRACn6        S              6S 0 INZ(0)
     D LUTCZch         S              1A   INZ(*BLANKS)
     D LUCAZch         S             10A   INZ(*BLANKS)
     D WCOME           S             15A   INZ(*BLANKS)
     D wdmacctch10     s             10A   inz(*blanks)
     D wdmacct         s             10s 0 inz(*zeros)
     D Total_Comercio  s             15s 2 inz(*zeros)
     D W_ADQNUM        s                   like(ADQNUM)
     D W_ADQNUM_ch     s             10A   inz(*Blanks)
     D W_ADQCNT1       s                   like(ADQCNT1)
     D W_ADQCCO1       s                   LIKE(ADQCCO1)
     D W_ADQCNT2       s                   like(ADQCNT2)
     D W_ADQCCO2       s                   LIKE(ADQCCO2)
     D W_ADQCNT3       s                   like(ADQCNT3)
     D W_ADQCCO3       s                   LIKE(ADQCCO3)
     D W_ADQCNT4       s                   like(ADQCNT4)
     D W_ADQCCO4       s                   LIKE(ADQCCO4)
     D W_ADQCNT5       s                   like(ADQCNT5)
     D W_ADQCCO5       s                   LIKE(ADQCCO5)
     D W_ADQCNT6       s                   like(ADQCNT6)
     D W_ADQCCO6       s                   LIKE(ADQCCO6)
     D W_ADQCNT7       s                   like(ADQCNT7)
     D W_ADQCCO7       s                   LIKE(ADQCCO7)
     D W_ADQCNT8       s                   like(ADQCNT8)
     D W_ADQCCO8       s                   LIKE(ADQCCO8)
     D W_ADQCNT9       s                   like(ADQCNT9)
     D W_ADQCCO9       s                   LIKE(ADQCCO9)
     D W_ADQCNT10      s                   like(ADQCNT10)
     D W_ADQCC10       S                   LIKE(ADQCC10)
     D W_ADQCNT11      s                   like(ADQCNT11)
     D W_ADQCC11       S                   LIKE(ADQCC11)
     D W_ADQCNT12      s                   like(ADQCNT12)
     D W_ADQCC12       S                   LIKE(ADQCC12)
     D W_ADQCNT13      s                   like(ADQCNT13)
     D W_ADQCC13       S                   LIKE(ADQCC13)
     D W_ADQCNT14      s                   like(ADQCNT14)
     D W_ADQCC14       S                   LIKE(ADQCC14)
     D W_ADQCNT15      s                   like(ADQCNT15)
     D W_ADQCC15       S                   LIKE(ADQCC15)
     D* CODIGOS DE TRN
     D W_ADQCTR1       S                   LIKE(ADQCTR1)
     D W_ADQCTR2       S                   LIKE(ADQCTR2)
     D W_ADQCTR3       S                   LIKE(ADQCTR3)
     D W_ADQCTR4       S                   LIKE(ADQCTR4)
     D W_ADQCTR5       S                   LIKE(ADQCTR5)
     D W_ADQCTR6       S                   LIKE(ADQCTR6)
     D W_ADQCTR7       S                   LIKE(ADQCTR7)
     D W_ADQCTR8       S                   LIKE(ADQCTR8)
     D W_ADQCTR9       S                   LIKE(ADQCTR9)
     D W_ADQCTR10      S                   LIKE(ADQCTR10)
     D W_ADQCTR11      S                   LIKE(ADQCTR11)
     D W_ADQCTR12      S                   LIKE(ADQCTR12)
     D W_ADQCTR13      S                   LIKE(ADQCTR13)
     D W_ADQCTR14      S                   LIKE(ADQCTR14)
     D W_ADQCTR15      S                   LIKE(ADQCTR15)

     D W_TSTCC         s                   LIKE(TSTCC)
     D Sum_Net_Trn     s             12S 2 INZ(0.00)
     D Sum_Mto_Trn     s             12S 2 INZ(0.00)
     D Sum_Afi_Trn     s             12S 2 INZ(0.00)
     D T_Sum_Afi_Trn   s             12S 2 INZ(0.00)
     D T_Sum_Afi_Trn2  s             12S 2 INZ(0.00)
     D Sum_Isr_Trn     s             12S 2 INZ(0.00)
     D T_Sum_Isr_Trn   s             12S 2 INZ(0.00)
     D Sum_Dev_8Po     s             12S 2 INZ(0.00)
     D T_Sum_Dev_8Po   s             12S 2 INZ(0.00)
     D Sum_Devolucion  s             12S 2 INZ(0.00)
     D Sum_Avance_Efe  s             12S 2 INZ(0.00)
     D Sum_Liq_Trn     s             12S 2 INZ(0.00)
     D T_Sum_Liq_Trn   s             12S 2 INZ(0.00)
     D Total_calculad  s             12S 2 INZ(0.00)
     D ExisteCtacli    S              1A   INZ(' ')
     D W_TSTACT        S             12S 0 INZ(0)
     D W_TSWSCC        S              5S 0 INZ(0)
     D Concepto        S              3A   INZ(*blanks)
     D DesConcepto     S             11A   INZ(*blanks)
     d Non_Num_Posn    s              2  0  Inz(*zeros)
     d tamaCODP        s              2  0
IPD  d Numbers         S             10    inz('0123456789')
IPD  d BinCha          S              6a   inz(*blanks)
     D                 DS
     D WFEDIAL                 1      6    INZ
     D FAÑOS                   1      2
     D FMESS                   3      4
     D FDIAS                   5      6
     D                 DS
     D wFTSBT                  1      3S 0 INZ(000)
     D wFTSBTcha               1      3
     d*
     D                 DS
     D eDSCDT                  1      9s 0
     D edia                    2      3
     D emes                    4      5
     D eann                    6      9
     D                 DS
     D cYYYYMMDD               1      8
     D NYYYYMMDD               1      8s 0
     D                 DS
     D CFDRCR                  1      1
     D wCFDRCR                 1      1s 0
     D                 DS
     D LUFDL                   1      4s 0
     D wLUFDL                  1      4
     D                 DS
     D LUFPT                   1      4s 0
     D wLUFPT                  1      4
     D                 DS
     D CFTSBR                  1      3s 0
     D wCFTSBR                 1      3
     D                 DS
     D CFTSTE                  1      4s 0
     D wCFTSTE                 1      4
     D                 DS
     D  wCUX1AC                1     12 00
     D wtip                    1      2 00
     D wcta                    3     12 00
      *//* AGREGADO EL 21/07/2025 MEJORA POR ECOMMERCE
     D esEcommerce     S              5S 0 inz(0)
     D W_ADQENUM       s                   like(ADQENUM)
     D W_ADQENUM_ch    s             10A   inz(*Blanks)
     D W_ADQECNT1      s                   like(ADQECNT1)
     D W_ADQECNT5      s                   like(ADQECNT5)
     D W_ADQECCO1      s                   LIKE(ADQECCO1)
     D W_ADQECCO5      s                   LIKE(ADQECCO5)
     D* CODIGOS DE TRN
     D W_ADQECTR1      S                   LIKE(ADQECTR1)
     D W_ADQECTR5      S                   LIKE(ADQECTR5)

      *//*COBROS A LOS COMERCIOS POR ECOMMERCE
     D DESCFUNC        S                   LIKE(ADQCOBRO03)
     D C_TERMINAL      S             16A   INZ('')
     D C_MEMBRESIA     S                   LIKE(ADQCOBRO04)
     D C_STS           S                   LIKE(ADQCOBRO18)
     D $TasaCompra     S                   LIKE(GBBKXR)
     D MonedaUSD       S              3S 0 inz(001)
     D flagCobro       S                   LIKE(ADQMEMH012)
     D descripFlag     S                   LIKE(ADQMEMH017)
     D StsCtaComercio  S              1A   INZ('I')
      *Nuevo
     D ProcesoBanet    S              1    INZ('N')
     D PgmDs         ESDS                  extname(RPG4DS) qualified

     DCoreteNum        S              3S 0
     DNUMCoreteNum     S              3S 0 INZ(23)
     C*
     C     *ENTRY        PLIST
     C                   PARM                    @00FILE          17
     C                   PARM                    PERFIL           13
     C                   PARM                    Nolote            3
     C                   PARM                    Usuario          10
     C                   PARM                    MARCA1            4
     C                   PARM                    MARCA2            4
     C                   PARM                    TIPO              2
     c*
      *                                                                       
      * Rutina principal del programa                                         *
      *                                                                       
      /free

         exsr FecReal;
         exsr VerFecha;
         exsr VerPerfil;
         If   MsgNoPerfil = '0';

           eval Sum_Liq_Trn= 0.00;
           eval T_Sum_Liq_Trn= 0.00;
           eval T_Sum_Isr_Trn = 0.00;
           eval T_Sum_Afi_Trn= 0.00;
           eval T_Sum_Afi_Trn2= 0.00;
           eval Sum_Dev_8Po  = 0.00;
           eval T_Sum_Dev_8Po  = 0.00;
           W00FILE = @00FILE;
           Setll (W00FILE) ADQ00ARC ;
           Reade (W00FILE) ADQ00ARC ;
           Dow not %Eof(ADQ00ARC );

             //  ©Cabecera de archivo
             W01FILE = A00FILE;
             Setll (W01FILE ) ADQ01ENT ;
             Reade (W01FILE ) ADQ01ENT ;
             Dow not %Eof(ADQ01ENT );

             //  ©Encabezado de Comercio
               W02FILE = A01FILE;
               W02FIID = A01FIID;
               Setll ( W02FILE: W02FIID  ) ADQ02COM ;
               Reade ( W02FILE: W02FIID  ) ADQ02COM ;
               Dow not %Eof(ADQ02COM );
               W02NACO =  A02NACO;

                 If A02FIID <> 'H021';
                     eval ProcesoBanet = 'S';
                 Endif;

               Exsr Controles;

            //* AGREGADO EL 21/07/2025 MEJORA POR ECOMMERCE
               eval C_MEMBRESIA = 0.00;
            //***
               eval Sum_Net_Trn= 0.00;
               eval Sum_Mto_Trn= 0.00;
               eval Sum_Afi_Trn= 0.00;
               eval Sum_Isr_Trn= 0.00;
               eval Sum_Devolucion = 0.00;
               eval Sum_Avance_Efe = 0.00;
               eval Sum_Liq_Trn = 0.00;
               eval Sum_Dev_8Po  = 0.00;

               exsr Reg_Total_Comercio;

             //  ©Encabezado de Terminal
                 W03FILE = A02FILE;
                 W03FIID = A02FIID;
                 W03COME = A02COME;
                 Setll (W03FILE: W03FIID: W03COME ) ADQ03TER ;
                 Reade (W03FILE: W03FIID: W03COME ) ADQ03TER ;
                 Dow not %Eof(ADQ03TER);

            //* AGREGADO EL 21/07/2025 MEJORA POR ECOMMERCE
            //Aqui hacemos validancion si es una terminal virtual
                  exsr ValidaTerminal;
            //***

             //  ©Encabezado de lotes
                   W04FILE = A03FILE;
                   W04FIID = A03FIID;
                   W04COME = A03COME;
                   W04TERM = A03TERM;
                   Setll (W04FILE: W04FIID: W04COME: W04TERM ) ADQ04LOT ;
                   Reade (W04FILE: W04FIID: W04COME: W04TERM ) ADQ04LOT ;
                   Dow not %Eof(ADQ04LOT) ;

             //  ©Registro de Detalle General
                     W10FILE = A04FILE;
                     W10FIID = A04FIID;
                     W10COME = A04COME;
                     W10TERM = A04TERM;
                     W10LOTE = A04LOTE;

                     Setll (W10FILE:W10FIID:W10COME:W10TERM:W10LOTE) ADQ10DET ;
                     Reade (W10FILE:W10FIID:W10COME:W10TERM:W10LOTE) ADQ10DET ;
                     Dow not %Eof(ADQ10DET);
                      If (A10FIDT = MARCA1 OR A10FIDT = MARCA2);
                         A10NUMU = W_ADQNUM;

                         A13FILE = A10FILE; //ARCHIVO
                         A13FIID = A10FIID; //ENTIDAD PROSA EMISOR
                         A13COME = A10COME; //CLAVE COMERCIO
                         A13TERM = A10TERM; //TERMINAL
                         A13LOTE = A10LOTE; //CLAVE DE LOTE
                         A13NUMR = A10NUMR; //NUMERO REFERENCIA
                         chain (A13FILE: A13FIID: A13COME: A13TERM:
                                A13LOTE: A13NUMR) ADQ13IMP;

                         Exsr Totalize;
                         A10CTPO = W_A10CTPO; //CTA POSTEO
                         A10CATC = A02CATC; //FAMILIA DEL COMERCIO
                         A10GICO = A02GICO; //GIRO DEL COMERCIO
                         A10CTRE = 0; //CTA RECHAZO
                         A10STAT = 'Genera_Lot'; //ESTATUS TRANSACCION
                         UPDATE R10DETA;

                         If ProcesoBanet = 'S';


                           CoreteNum = A10IMCT;
                        //  ©contabilida Debito Neto de liquidacion comerci
                        //   wdmacctch10 = %subst(A02CTDE: 1: 10);
                           LUTCZch = ' ';
                           W_TSTACT = W_ADQCNT1;
                           W_TSWSCC = W_ADQCCO1;
                           Concepto = 'VTA';
                           DesConcepto = 'Db Net.Liq1' + %Trim(%char(A10IMCT));
                           W_TSTCC  = T_Sum_Liq_Trn ;
                           Codtrn   = W_ADQCTR1;  //'0784'   0783=Cr 0784=Db
                           W02NACO = 'Total Neto Db' + %Trim(%char(A10IMCT)) +
                                    'liquidacion come' ;
                           Exsr Debito;
                        //  ©contabilida Credito Neto de liquidacion comerci
                           LUTCZch = ' ';
                           W_TSTACT = W_ADQCNT2;
                           W_TSWSCC = W_ADQCCO2;
                           Concepto = 'VTA';
                           DesConcepto = 'Cr Net.Liq2'+ %Trim(%char(A10IMCT));
                           W_TSTCC  = T_Sum_Liq_Trn;
                           Codtrn =  W_ADQCTR2;  // '0783';  0783=Cr 0784=Db
                           W02NACO = 'Total Neto Cr'+ %Trim(%char(A10IMCT)) +
                                    'liquidacion come';
                           Exsr Credito;

                           Iter;

                         EndIf;
                      EndIf;
                      Reade (W10FILE:W10FIID:W10COME:W10TERM:W10LOTE) ADQ10DET ;
                     Enddo;

                   Reade (W04FILE: W04FIID: W04COME: W04TERM ) ADQ04LOT ;
                   Enddo;

                 Reade ( W03FILE: W03FIID: W03COME ) ADQ03TER ;
                 Enddo;


          //  ©comparacion de total de comercio con sumatoria detalle

               Total_calculad  =
               (Sum_Net_Trn + Sum_Avance_Efe - Sum_Devolucion);
                   Exsr Ver_Cta;
                   // If ErrorFlag = '0';

                         //  (2)©contabilida debito afiliacion comercio
                         If ExisteCtacli = 'S';
                            IF DMTYP = 1;
                               LUTCZch = '1';
                               CodTrn ='DHAD';        // DEBITO AHO
                            Else;
                               IF DMTYP = 6;
                                  LUTCZch = '6';
                                  CodTrn ='DCAD';      // DEBITO CHK
                               EndIf;
                            EndIf;
                            W_TSTACT = wdmacct  ;
                            W_TSWSCC = 0;
                            W_A10CTPO = wdmacct;
                            Concepto = 'AFI';
                            DesConcepto = 'Db Afil.Com';
                            W_TSTCC = Sum_Afi_trn;
                   //       Exsr DbCliVta ;
                         Else;
                            //  ©contablidad cr Neto Trn contable
                            LUTCZch = ' ';
                            W_TSTACT = W_ADQCNT11;
                            W_TSWSCC = W_ADQCC11 ;
                            W_A10CTPO = W_ADQCNT11;
                            W_TSTCC  = Sum_Afi_trn;
                            Codtrn   =  W_ADQCTR11;    // 0784=Db
                            Concepto = 'AFI';
                            DesConcepto = 'Db Afil.Com';
                            Exsr Debito;
                         Endif;
                      //CREDITOS A CUENTA DE RECHAZO SI CUENTA COMERCIO INACTIVA
                         exsr Credito_comercio;
                         exsr Credito_8_Porciento;

       //  ©contabilida Avance de efectivo por COMERCIO
                     //  ©contabilida Debito por avance de efectivo
                         W_TSTACT = W_ADQCNT3;
                         W_TSWSCC = W_ADQCCO3;
                         Concepto = 'AVA';
                         DesConcepto = 'Db Ava.Efec';
                         W_TSTCC  = Sum_Avance_Efe;
                         Codtrn   =  W_ADQCTR3;      // 0784=Db
                         Exsr Debito;
                     //  ©contabilida credito por avance de efectivo
                         W_TSTACT = W_ADQCNT10;
                         W_TSWSCC = W_ADQCC10;
                         Concepto = 'AVA';
                         DesConcepto = 'Cr Ava.Efec';
                         W_TSTCC = Sum_Avance_Efe;
                         // CodTrn ='CCAD';   // CCAD=Cr antes wel 08/12/15
                         Codtrn    = W_ADQCTR10; // 0783=Cr Despues
                         Exsr Credito;
                   // Endif;

       //* CONTABILIDAD COMISIÓN POR MEMEBRESÍA ÚNICA ECOMMERCE
                if (C_MEMBRESIA <> 0);
                  If ( ExisteCtacli = 'S' AND StsCtaComercio = 'A' );

                    W_ADQNUM_ch = W_ADQENUM_ch;

                    IF DMTYP = 1;
                        LUTCZch = '1'; //Debito a Cta AHO
                        CodTrn ='DHAD';        // DEP.AHO
                    Else;
                        IF DMTYP = 6;
                          LUTCZch = '6'; //Debito a Cta CHK
                          CodTrn ='DCAD';      // DEP CHK
                        EndIf;
                    EndIf;

                    //* Debito por MEMEBRESÍA ÚNICA a COMERCIO
                    W_TSTACT = wdmacct;
                    W_TSWSCC = 0;
                    W_A10CTPO = wdmacct;
                    Concepto = 'MEM';
                    DesConcepto = 'Db MemUnica';
                    W_TSTCC = C_MEMBRESIA;
                    Exsr DbCliVta ;

                  else;

                    W_ADQNUM_ch = W_ADQENUM_ch;

                    //* Debito por MEMEBRESÍA ÚNICA a CTA RECHAZO
                    LUTCZch = '6'; //Debito a Cta Contable:
                    W_TSTACT = W_ADQECNT5;
                    W_TSWSCC = W_ADQECCO5;
                    Concepto = 'MEM';
                    DesConcepto = 'Db MemUnica';
                    W_TSTCC  = C_MEMBRESIA;
                    Codtrn   = W_ADQECTR5;  //'0784'
                    W02NACO = 'MEMBRESÍA ECOM ADQ';
                    Exsr Debito2;
                  Endif;

                  //* Crédito por MEMEBRESÍA ÚNICA a CTA CONTABLE
                    LUTCZch = ' ';
                    W_TSTACT = W_ADQECNT1; //CUENTA CONTABLE MEM UNICA
                    W_TSWSCC = W_ADQECCO1; //CENTRO COSTO CTA MEM UNICA
                    Concepto = 'MEM';
                    DesConcepto = 'Cr MemUnica';
                    W_TSTCC = C_MEMBRESIA; // MONTO DEL LOTE
                    Codtrn    = W_ADQECTR1; // COD TRN 0783=CR MEM UNICA
                    W02NACO = 'MEMBRESÍA ECOM ADQ';
                    Exsr Credito;
                    Exsr CambiaStsCobroMemUnica;
                    Exsr HistoricoCobroEcommerce;
                    W_ADQNUM_ch = %char(W_ADQNUM);
                ENDIF;
               Reade ( W02FILE: W02FIID ) ADQ02COM ;
               Enddo;

             Reade (W01FILE) ADQ01ENT ;
             Enddo;

           Reade (W00FILE) ADQ00ARC ;
           Enddo;


                        CoreteNum = A10IMCT;
                     //  ©contabilida Debito Neto de liquidacion comerci
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT1;
                         W_TSWSCC = W_ADQCCO1;
                         Concepto = 'VTA';
                         DesConcepto = 'Db Net.Liq1' + %Trim(%char(A10IMCT));
                         W_TSTCC  = T_Sum_Liq_Trn ;
                         Codtrn   = W_ADQCTR1;  //'0784'   0783=Cr 0784=Db
                         W02NACO = 'Total Neto Db' + %Trim(%char(A10IMCT)) +
                                   'liquidacion come' ;
                         Exsr Debito;
                     //  ©contabilida Credito Neto de liquidacion comerci
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT2;
                         W_TSWSCC = W_ADQCCO2;
                         Concepto = 'VTA';
                         DesConcepto = 'Cr Net.Liq2'+ %Trim(%char(A10IMCT));
                         W_TSTCC  = T_Sum_Liq_Trn;
                         Codtrn =  W_ADQCTR2;  // '0783';  0783=Cr 0784=Db
                         W02NACO = 'Total Neto Cr'+ %Trim(%char(A10IMCT)) +
                                   'liquidacion come';
                         Exsr Credito;

                     //  ©contabilida Debito Neto de liquidacion come rci
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT7;
                         W_TSWSCC = W_ADQCCO7;
                         Concepto = 'VTA';
                         DesConcepto = 'Db Net.Liq3';
                         W_TSTCC  = T_Sum_Liq_Trn ;
                         Codtrn   = W_ADQCTR7;      //     0784=Db
                         W02NACO = 'Total Neto Db liquidacion come';
                         Exsr Debito ;
        // 27/02/2017  //  ©contabilida total ISR
                         W_TSTACT = W_ADQCNT5;
                         W_TSWSCC = W_ADQCCO5;
                         W_TSTCC  = T_Sum_Isr_Trn;
                         Codtrn   =  W_ADQCTR5;  //  0783=cr
                         Concepto = 'ISR';
                         DesConcepto = 'Cr Net.ISR ';
                         W02NACO = 'Total CR Imp. Sobre la Renta ';
                         Exsr Credito  ;
                     //  ©contabilida debito 8 porciento impuesto
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT6;
                         W_TSWSCC = W_ADQCCO6;
                         Concepto = 'VTA';
                         DesConcepto = 'Dev.8%.Come';
                         W_TSTCC  = T_Sum_Dev_8Po;
                         Codtrn   =  W_ADQCTR6;   //  0784=Db
                         W02NACO = 'Total Neto Db 8% Sobre Impuest';
                         Exsr Debito ;
                         //  ©contablidad cr afiliacion trn nuestra
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT9;
                         Exsr VerCcCome;
                         W_TSWSCC = Cc_comis3;
                         W_TSTCC  = T_Sum_Afi_trn;
                         Codtrn = W_ADQCTR9;        // 0783=Cr
                         Concepto = 'AFI';
                         DesConcepto = 'Cr Net.Afil';
                         W02NACO = 'Total CR Afiliacion          ';
                         Exsr Credito;
                         //  ©contablidad cr afiliacion trn ot bancos
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT4;
                         W_TSWSCC = W_ADQCCO4;
                         W_TSTCC  = T_Sum_Afi_trn2;
                         Codtrn   = W_ADQCTR4;    // 0783=Cr
                         Concepto = 'AFI';
                         DesConcepto = 'Cr Net.Afil';
                         W02NACO = 'Total CR Afiliacion          ';
                         Exsr Credito;

           exsr UpdHdrLote;

         Endif;
         *inlr=*on;

       BEGSR VerFileTrn;
             CFBK=001;
             CFTCNO=CodTrn;
             chain (CFBK:  CFTCNO) CFP753;
             If %Found(CFP753);
                TSTSAP =   CFAPPL;
                TSWDRC =   CFDRCR;
                exsr acumula;
             endif;
       ENDSR;

       BEGSR Acumula;
          If wCFDRCR > 5;   // Process Debit
               CANTdb = CANTdb + 1;
               TotalDB = TotalDB + TSTCC;
               t_debito= t_debito + TSTCC;
          Else;
            If wCFDRCR <= 5;   // Process Credit
               CANTcr = CANTcr + 1;
               TotalCR = TotalCR + TSTCC;
               t_credito=t_credito + TSTCC;
            Endif;
          Endif;
       ENDSR;
       Begsr VerPerfil;
           MsgNoPerfil = '1';
           CFTSBK=001;
           CFTSKY=PERFIL;
           Chain (CFTSBK:CFTSKY) CFP801;
           If %Found(CFP801);
              MsgNoPerfil = '0';
              exsr VerUltlote;
           ENDIF;
           If MsgNoPerfil = '1';
              Dsply 'No existe perfil';
           Endif;
         EndSr;

         Begsr VerUltlote;
             wFTSBT = 000;
             FTTSBK=001;
             FTTSKY=PERFIL;
             Setll ( FTTSBK: FTTSKY ) pop801;
             Reade ( FTTSBK: FTTSKY  ) pop801;
             Dow not %Eof(pop801);
                      wFTSBT = FTSBT ;
             Reade ( FTTSBK: FTTSKY  ) pop801;
             Enddo;
             Exsr NuevoLote;
         EndSr;


         Begsr NuevoLote;
             clear POP8011;
             wFTSBT = wFTSBT + 1;
             Nolote =wFTSBTcha;
             FTTSBK=001;
             FTTSKY=PERFIL;
             FTSBT = wFTSBT;
             FTTSST = 02;
             FTTSOR =Usuario;
             FTTSDT =  DSDT;
             write Pop8011;
             wTSWSEQ = 0;
         EndSr;

         Begsr UpdHdrLote;
             FTTSBK=001;
             FTTSKY=PERFIL;
             FTSBT = wFTSBT;
             Chain (FTTSBK: FTTSKY: FTSBT ) pop801;
             If %Found(pop801);
                FTTSDI = CANTdb;
                FTTSID = totalDB;
                FTTSCI = CANTcr;
                FTTSIC = totalCR;
                Update pop8011;
             Endif;
         EndSr;

         Begsr VerFecha;
           DSBK =001;
           Chain (DSBK) TAP001;
           If %Found(TAP001);
              eDSCDT = DSCDT;
              cYYYYMMDD = eann + emes +  edia ;
           ENDIF;
         EndSr;

      *-Postear credito a cuenta del cliente
         BEGSR CrCliVta ;
           clear pop8021;
           TSBK    = 001;
           TSTSKY  = PERFIL ;
           TSBTCH  =  wFTSBT;
           wTSWSEQ = wTSWSEQ + 1;
           TSWSEQ = wTSWSEQ;
           TSTOVR =   'S';
           TSTTDT =  nYYYYMMDD;             // fecha efectiva
           TSTACT = W_TSTACT;
           TSWSCC = W_TSWSCC;
           TSWTCD = CODTRN;
           TSTCC =  W_TSTCC;
           EXSR VerFileTrn;
           NOTRACn6=%dec(NOTRACh6:6:0);
           TSTCN1 = NOTRACn6;
           TSTAL1 = ' ';
           TSTAL2 = ' ';
           TSTAL3 = ' ';
           TSTAL1 = W02NACO;
           WCOME  = %CHAR(A02COME);
           TSTAL2 = WCOME  + '-' + A02FEDA;
           %subst(TSTAL3:1:1) = '&' ;
           %subst(TSTAL3:2:3) = Concepto;
           %subst(TSTAL3:5:1) = '&' ;
           %subst(TSTAL3:6:10) = W_ADQNUM_ch;
           %subst(TSTAL3:16:11) = DesConcepto;
           %subst(TSTAL3:29:2) = '||';
           hayconta=hayconta + 1;
             IF TSTCC > 0;
              write pop8021;
             Endif;
         ENDSR;
      *-Postear debito a cuenta del cliente
         BEGSR DbCliVta ;
           clear pop8021;
           TSBK    = 001;
           TSTSKY  = PERFIL ;
           TSBTCH  =  wFTSBT;
           wTSWSEQ = wTSWSEQ + 1;
           TSWSEQ = wTSWSEQ;
           TSTOVR =   'S';
           TSTTDT =  nYYYYMMDD;             // fecha efectiva
           TSTACT = W_TSTACT;
           TSWSCC = W_TSWSCC;
           TSWTCD = CODTRN;
           TSTCC =  W_TSTCC;
           EXSR VerFileTrn;
           TSTCN1 = 0;
           TSTAL1 = ' ';
           TSTAL2 = ' ';
           TSTAL3 = ' ';
           TSTAL1 = W02NACO;
           WCOME  = %CHAR(A02COME);
           TSTAL2 = WCOME  + '-' + A02FEDA;
           %subst(TSTAL3:1:1) = '&' ;
           %subst(TSTAL3:2:3) = Concepto;
           %subst(TSTAL3:5:1) = '&' ;
           %subst(TSTAL3:6:10) = W_ADQNUM_ch;
           %subst(TSTAL3:16:11) = DesConcepto;
           %subst(TSTAL3:29:2) = '||';
           hayconta=hayconta + 1;
             IF TSTCC > 0;
              Write pop8021;
             ENDIF;
         ENDSR;

         BEGSR Credito ;
           clear pop8021;
           TSBK    = 001;
           TSTSKY  = PERFIL ;
           TSBTCH  =  wFTSBT;
           wTSWSEQ = wTSWSEQ + 1;
           TSWSEQ  = wTSWSEQ;
           TSTOVR  =   'S';
           TSTTDT  =  nYYYYMMDD;             // fecha efectiva
           TSTACT  =  W_TSTACT;
           TSWSCC  =  W_TSWSCC;
           TSWTCD  = CODTRN;
           TSTCC   = W_TSTCC;
           EXSR VerFileTrn;
           TSTCN1 = 0;
           TSTAL1 = ' ';
           TSTAL2 = ' ';
           TSTAL3 = ' ';
           TSTAL1 = W02NACO;
           WCOME  = %CHAR(A02COME);
           TSTAL2 = WCOME  + '-' + A02FEDA;
           %subst(TSTAL3:1:1) = '&' ;
           %subst(TSTAL3:2:3) = Concepto;
           %subst(TSTAL3:5:1) = '&' ;
           %subst(TSTAL3:6:10) = W_ADQNUM_ch;
           %subst(TSTAL3:16:11) =  DesConcepto;
           %subst(TSTAL3:29:2) = '||';
           hayconta=hayconta + 1;
              IF TSTCC > 0;
              write pop8021;
              EndIf;
         ENDSR;
         BEGSR Debito;
           clear pop8021;
           TSBK    = 001;
           TSTSKY  = PERFIL ;
           TSBTCH  =  wFTSBT;
           wTSWSEQ = wTSWSEQ + 1;
           TSWSEQ  = wTSWSEQ;
           TSTOVR  =   'S';
           TSTTDT  =  nYYYYMMDD;              // fecha efectiva
           TSTACT  = W_TSTACT;
           TSWSCC  = W_TSWSCC;
           LUTCZch = '6';
           TSWTCD  = CODTRN;
           TSTCC   =  W_TSTCC;
           LUCAZch = wdmacctch10;
           EXSR VerFileTrn;
           NOTRACn6=%dec(NOTRACh6:6:0);
           TSTCN1  = NOTRACn6;
           TSTAL1  = ' ';
           TSTAL2  = ' ';
           TSTAL3  = ' ';
           TSTAL1 = W02NACO;
           WCOME  = %CHAR(A02COME);
           TSTAL2 = WCOME  + '-' + A02FEDA + '-'+ LUTCZch + '-' + LUCAZch;
           %subst(TSTAL3:1:1) = '&' ;
           %subst(TSTAL3:2:3) = Concepto;
           %subst(TSTAL3:5:1) = '&' ;
           %subst(TSTAL3:6:10) = W_ADQNUM_ch;
           %subst(TSTAL3:16:11) =  DesConcepto;
           %subst(TSTAL3:29:2) = '||';
           hayconta=hayconta + 1;
              IF TSTCC > 0;
              Write pop8021;
              EndIf;
         ENDSR;

         BEGSR Debito2;
           clear pop8021;
           TSBK    = 001;
           TSTSKY  = PERFIL ;
           TSBTCH  =  wFTSBT;
           wTSWSEQ = wTSWSEQ + 1;
           TSWSEQ  = wTSWSEQ;
           TSTOVR  =   'S';
           TSTTDT  =  nYYYYMMDD;             // fecha efectiva
           TSTACT  = W_TSTACT;
           TSWSCC  = W_TSWSCC;
           //LUTCZch = '6';
           TSWTCD  = CODTRN;
           TSTCC   =  W_TSTCC;
           LUCAZch = wdmacctch10;
           EXSR VerFileTrn;
           NOTRACn6=%dec(NOTRACh6:6:0);
           TSTCN1  = NOTRACn6;
           TSTAL1  = ' ';
           TSTAL2  = ' ';
           TSTAL3  = ' ';
           TSTAL1 = W02NACO;
           WCOME  = %CHAR(A02COME);
           if (LUTCZch <> *BLANKS AND LUCAZch <> *BLANKS);
            TSTAL2 = WCOME  + '-' + A02FEDA + '-'+ LUTCZch + '-' + LUCAZch;
           else;
            TSTAL2 = WCOME  + '-' + A02FEDA;
           endif;
           %subst(TSTAL3:1:1) = '&' ;
           %subst(TSTAL3:2:3) = Concepto;
           %subst(TSTAL3:5:1) = '&' ;
           %subst(TSTAL3:6:10) = W_ADQNUM_ch;
           %subst(TSTAL3:16:11) =  DesConcepto;
           %subst(TSTAL3:29:2) = '||';
           hayconta=hayconta + 1;
              IF TSTCC > 0;
              Write pop8021;
              EndIf;
         ENDSR;

         Begsr Reg_Total_Comercio;
           Total_Comercio = 0.00;
           W92FILE = A02FILE;
           W92FIID = A02FIID;
           W92COME = A02COME;
           chain (W92FILE: W92FIID: W92COME) ADQ92COM;
           IF %FOUND(ADQ92COM);
              Total_Comercio = A92TODE;
           ELSE;
              DSPLY 'NO HAY TOTALES DEL COMERCIO';
              ErrorFlag = '1';
              Total_Comercio = 0.00;
           ENDIF;
         ENDSR;

         Begsr Ver_Cta;
           wdmacctch10 = %subst(A02CTDE: 1: 10);
           wdmacct    = %DEC(wdmacctch10 :10:0);
           chain (wdmacct) tap002CT;
           IF %FOUND(TAP002CT);
              ExisteCtacli = 'S';
           ELSE;
              ExisteCtacli = 'N';
           ENDIF;

           IF (DMSTAT <>  '1' and DMSTAT <>  '6');
              //*Estatus de la cuenta invalido
              StsCtaComercio = 'I';
            ELSE;
              StsCtaComercio = 'A';
              LUCAZch = wdmacctch10;
              IF DMTYP = 1;
                LUTCZch = '1';
                CodTrn ='DHAD';        // DEBITO AHO
              Elseif (DMTYP = 6);
                LUTCZch = '6';
                CodTrn ='DCAD';      // DEBITO CHK
              EndIf;
            ENDIF;
         ENDSR;

         BEGSR Controles;
           chain (TIPO) IADQCTL;
           if %found(IADQCTL);
              ADQNUM = ADQNUM + 1;
              W_ADQNUM = ADQNUM;
              W_ADQNUM_ch = %char(W_ADQNUM);
              W_ADQCNT1 = ADQCNT1;
              W_ADQCCO1 = ADQCCO1;
              W_ADQCNT2 = ADQCNT2;
              W_ADQCCO2 = ADQCCO2;
              W_ADQCNT3 = ADQCNT3;
              W_ADQCCO3 = ADQCCO3;
              W_ADQCNT4 = ADQCNT4;
              W_ADQCCO4 = ADQCCO4;
              W_ADQCNT5 = ADQCNT5;
              W_ADQCCO5 = ADQCCO5;
              W_ADQCNT6 = ADQCNT6;
              W_ADQCCO6 = ADQCCO6;
              W_ADQCNT7 = ADQCNT7;
              W_ADQCCO7 = ADQCCO7;
              W_ADQCNT8 = ADQCNT8;
              W_ADQCCO8 = ADQCCO8;
              W_ADQCNT9 = ADQCNT9;
              W_ADQCCO9 = ADQCCO9;
              W_ADQCNT10= ADQCNT10;
              W_ADQCC10= ADQCC10;
              W_ADQCNT11= ADQCNT11;
              W_ADQCC11= ADQCC11;
              W_ADQCNT12= ADQCNT12; //Cta rechazo Cre
              W_ADQCC12= ADQCC12; //C.C rechazo Cre
              W_ADQCNT13= ADQCNT13; //Cta rechazo Deb
              W_ADQCC13 = ADQCC13; //C.C rechazo Deb
              W_ADQCNT14= ADQCNT14;
              W_ADQCC14= ADQCC14;
              W_ADQCNT15= ADQCNT15;
              W_ADQCC15= ADQCC15;
            // CODIGOS DE TRANSACCIO
              W_ADQCTR1 = ADQCTR1;
              W_ADQCTR2 = ADQCTR2;
              W_ADQCTR3 = ADQCTR3;
              W_ADQCTR4 = ADQCTR4;
              W_ADQCTR5 = ADQCTR5;
              W_ADQCTR6 = ADQCTR6;
              W_ADQCTR7 = ADQCTR7;
              W_ADQCTR8 = ADQCTR8;
              W_ADQCTR9 = ADQCTR9;
              W_ADQCTR10= ADQCTR10;
              W_ADQCTR11= ADQCTR11;
              W_ADQCTR12= ADQCTR12; //TRN rechazo Cre
              W_ADQCTR13= ADQCTR13; //TRN rechazo Deb
              W_ADQCTR14= ADQCTR14;
              W_ADQCTR15= ADQCTR15;
              Update GXADQCTL;
           else;
             IF not %found(IADQCTL);
                DSPLY 'No hay reg control contable';
                return;
             ENDIF;
           Endif;
         EndSr;

         begsr Totalize;
             //         © 01 Ventas
             //         © 02 Disposiciones en Efectiv
             //         © 20 Pagos
             //         © 21 Devoluciones            ?
             //         © 22 Prepopina y Pospropina
             //         © 23 Check In And Check Out
                       If A10NETD < 0;    // neto deposito
                          W_A10NETD = A10NETD * -1;
                       Else;
                          W_A10NETD = A10NETD;
                       Endif;
                       If A10IVCD < 0;    // Impuestos
                          W_A10IVCD = A10IVCD * -1;
                       Else;
                          W_A10IVCD = A10IVCD;
                       Endif;
                       If A10IMTR < 0;    // mto de transaccion
                          W_A10IMTR = A10IMTR * -1;
                       Else;
                          W_A10IMTR = A10IMTR;
                       Endif;
                       If A10ICDE < 0;    // mto de afiliacion
                          W_A10ICDE = A10ICDE * -1;
                       Else;
                          W_A10ICDE = A10ICDE;
                       Endif;
                       If A10IGCI < 0;    // mto de afiliacion
                          W_A10IGCI = A10IGCI * -1;
                       Else;
                          W_A10IGCI = A10IGCI;
                       Endif;
                       If A13MOTO < 0;    // mto de Liquidacion
                          W_A13MOTO = A13MOTO * -1;
                       Else;
                          W_A13MOTO = A13MOTO;
                       Endif;
                       If A10CLAT = 01; // 
       // 2017/01/27                    //  ©sumarizacion Neto Trn
                         eval Sum_Net_Trn = Sum_Net_Trn + W_A10NETD ;
                                        //  ©sumarizacion de Monto Trn
                         eval Sum_Mto_Trn = Sum_Mto_Trn + W_A10IMTR ;
                                        //  ©sumarizacion Mto afiliacion
                         eval Sum_Afi_Trn= Sum_Afi_trn + W_A10ICDE ;
                         BinCha = %subst(A10NCTA:1:6);
                         ABINCO = %dec(BinCha:6:0);
                         chain ( ABINCO ) BINACE;
                         If %Found(BINACE);
                            // comision por tasa de intercambio nuetra
                            Eval T_Sum_Afi_Trn= T_Sum_Afi_trn + W_A10ICDE;
                         Else;
                            // comision por tasa de intercambio ot bancos
                            Eval T_Sum_Afi_Trn2= T_Sum_Afi_trn2 + W_A10ICDE;
                         EndIf;
                                        //  ©sumarizacion de ISR
                         eval Sum_Isr_Trn= Sum_Isr_Trn + W_A10IVCD ;
                         eval T_Sum_Isr_Trn= T_Sum_Isr_Trn + W_A10IVCD ;
                                        //  ©sumarizacion de 8% Dev
                         eval Sum_Dev_8Po  = Sum_Dev_8Po   + W_A10IGCI ;
                         eval T_Sum_Dev_8Po  = T_Sum_Dev_8Po   + W_A10IGCI ;
                         //  ©sumarizacion Total Liquidacion POR COMERCIO
                         eval Sum_Liq_Trn= Sum_Liq_Trn + W_A13MOTO ;
                                        //  ©sumarizacion Total Liquidacion
                         eval T_Sum_Liq_Trn= T_Sum_Liq_Trn + W_A13MOTO ;

                       Else;
                       If A10CLAT = 02; //  ©Avance de efectivo
                         eval Sum_Avance_Efe = Sum_Avance_Efe + W_A10IMTR ;
                       Else;
                       If A10CLAT = 21; //  ©sumarizacion devoluciones
                         eval Sum_Devolucion = Sum_Devolucion + W_A10NETD;
                       Endif;
                       Endif;
                       Endif;
         EndSr;

         Begsr Credito_comercio;
                        //(2)©contabilida Credito a comercio
                         If ( ExisteCtacli = 'S' AND StsCtaComercio = 'A' );
                            IF DMTYP = 1;
                               LUTCZch = '1';
                               CodTrn ='CHAD';        // DEP.AHO
                            Else;
                               IF DMTYP = 6;
                                  LUTCZch = '6';
                                  CodTrn ='CCAD';      // DEP CHK
                               EndIf;
                            EndIf;
                            W_TSTACT = wdmacct  ;
                            W_TSWSCC = 0;
                            Concepto = 'VTA';
                            DesConcepto = 'Cr Comercio';
                            //weldelewl
                            W_TSTCC  = Sum_Net_Trn;
                            Exsr CrCliVta ;
                         Else;
                            //  ©contablidad Credito a comercio
                            LUTCZch = ' ';
                            W_TSTACT = W_ADQCNT8;
                            W_TSWSCC = W_ADQCCO8;
                            W_TSTCC  = Sum_Net_Trn;
                            Codtrn   = W_ADQCTR8;      // 0783=Cr no cta cli
                            Concepto = 'VTA';
                            DesConcepto = 'Cr Comercio';
                            Exsr Credito;
                         Endif;
         EndSr;


          Begsr Credito_8_Porciento;
                        //(2)©contabilida Credito a comercio
                         If ( ExisteCtacli = 'S' AND StsCtaComercio = 'A' );
                            IF DMTYP = 1;
                               LUTCZch = '1';
                               CodTrn ='CHAD';        // DEP.AHO
                            Else;
                               IF DMTYP = 6;
                                  LUTCZch = '6';
                                  CodTrn ='CCAD';      // DEP CHK
                               EndIf;
                            EndIf;
                            W_TSTACT = wdmacct  ;
                            W_TSWSCC = 0;
                            Concepto = 'VTA';
                            DesConcepto = 'Dev.8%.Come';
                            //weldelewl
                            W_TSTCC  = Sum_Dev_8Po;
                            Exsr CrCliVta ;
                         Else;
                            //  ©contablidad Credito a comercio
                            LUTCZch = ' ';
                            W_TSTACT = W_ADQCNT8;
                            W_TSWSCC = W_ADQCCO8;
                            W_TSTCC  = Sum_Dev_8Po;
                            Codtrn = W_ADQCTR8;       // 0783=Cr no cta cli
                            Concepto = 'VTA';
                            DesConcepto = 'Dev.8%.Come';
                            Exsr Credito;
                         Endif;
         EndSr;
         Begsr VerCcCome;
           ADQCOME = A02COME;
           chain ( ADQCOME ) IADQCOM;
           If %found(IADQCOM);
             w3ADQCODP = %subst(ADQCODP:1:3);
             Non_Num_Posn = %check (Numbers : w3ADQCODP);
              If Non_Num_Posn > *Zeros;
                 Cc_comis3  = 999;
              Else;
                 Cc_comis3  = %dec(w3ADQCODP: 3:0);
              EndIf;
           else;
                 Cc_comis3  = 999;
           Endif;
         EndSr;
      //* AGREGADO EL 21/07/2025 MEJORA POR ECOMMERCE **************************
         Begsr ValidaTerminal;
            clear esEcommerce;

            if (C_MEMBRESIA = 0);
               monitor;
                  esEcommerce = %scan('E' :A03TERM :1 :1 );
               on-error;
                  esEcommerce = 0;
                  Exsr GuardaLogDeErrores;
               endmon;

               if (esEcommerce > 0); //Es una terminal virtual
                  C_TERMINAL = A03TERM;
                  //Obtener cobros para terminal virtual por comercio
                  Exsr CobrosTerminalVrt;
                  //Si tiene pendiente cobro se obtienen las cuentas para lote
                  if (C_MEMBRESIA <> 0);
                  //Obtiene cuentas donde acreditará cobros por terminal virtual
                     Exsr CuentasControlEcommerce;
                  endif;
               endif;
            endif;
        EndSr;
        Begsr CobrosTerminalVrt;
         chain ( A02COME ) ADQCOBRO;
         if %FOUND(ADQCOBRO);
            if ( ADQCOBRO18 = 'PENDIENTE');
               Exsr ObtieneValorDolar;
               C_MEMBRESIA = %DECH(ADQCOBRO04 * $TasaCompra :8 :2);
               C_STS = ADQCOBRO18;
            endif;
         endif;
        EndSr;
        Begsr CuentasControlEcommerce;
            chain ('EC') ADQECTL;
            if %found(ADQECTL);
               ADQENUM = ADQENUM + 1;
               W_ADQENUM = ADQENUM;
               W_ADQENUM_ch = %char(W_ADQENUM);
               W_ADQECNT1 = ADQECNT1; //MEM UNICA CUENTA CONTABLE
               W_ADQECNT5 = ADQECNT5; //CTA RECHAZO
               W_ADQECCO1 = ADQECCO1; //MEM UNICA C.C
               W_ADQECCO5 = ADQECCO5; //CTA RECHAZO C.C
               // CODIGOS DE TRANSACCIO
               W_ADQECTR1 = ADQECTR1; //MEM UNICA COD TRN 0783=CR
               W_ADQECTR5 = ADQECTR5; //CTA RECHAZO COD TRN 0784=DR
               Update $ADQECTL;
            endif;
        EndSr;
        Begsr CambiaStsCobroMemUnica;
            chain ( A02COME ) ADQCOBRO;
            if %FOUND(ADQCOBRO);
               ADQCOBRO18 = 'PAGADO';
               flagCobro = 'P';
               descripFlag = 'PAGO POSTEADO EN LOTE';
               C_STS = ADQCOBRO18;
               monitor;
                  UPDATE $ADQCOBRO;
               on-error;
                  C_STS = 'ERROR STS';
                  Exsr GuardaLogDeErrores;
               endmon;
            endif;
        EndSr;
        Begsr ObtieneValorDolar;
            Setgt ( 1  : MonedaUSD : *Hival : *Hival ) GLC002;
            Readp GLC002;
            Dow Not %Eof(GLC002);
              If GBCODE = MonedaUSD;
                $TasaCompra = GBBKXR;
                Leave;
              EndIf;
              Readp GLC002;
            Enddo;
        EndSr;
        Begsr HistoricoCobroEcommerce;
            ADQMEMH001 = A02FILE; //ARCHIVO
            monitor;
              ADQMEMH002 = %CHAR(A02ENPR); //ENTIDAD
              ADQMEMH004 = %dec(A02CTDE: 10: 0); //CTA DEPOSITO COMERCIO
            on-error;
              ADQMEMH002 = *blanks;
              ADQMEMH004 = *zeros;
            endmon;
            ADQMEMH003 = A02COME; //CLAVE COMERCIO
            ADQMEMH005 = C_TERMINAL; //TERMINAL VIRTUAL
            ADQMEMH006 = A10LOTE; //CLAVE LOTE
            ADQMEMH007 = A10NUMR; //NUM REFERENCIA
            ADQMEMH008 = 'COMISIÓN MEMEBRESÍA ÚNICA'; //DESCRIPCIÓN
            ADQMEMH009 = A02NACO; //NOMBRE DE COMERCIO
            ADQMEMH010 = C_MEMBRESIA; //COBRO MEMBRESIA UNICA
            ADQMEMH011 = C_STS; //ESTADO COBRO MEMBRESÍA
            ADQMEMH012 = flagCobro; //FLAG COBRO REALIZADO
            //Auditoría
            ADQMEMH013 = %DEC( %DATE() ); //FECHA COBRO
            ADQMEMH014 = %DEC( %TIME() ); //HORA COBRO
            ADQMEMH015 = Usuario; //USUARIO COBRO
            ADQMEMH016 = $TasaCompra; //VALOR DOLAR USD
            ADQMEMH017 = descripFlag; //DESCRIPCIÓN RESULTADO DEL FLAG
            monitor;
               WRITE $ADQMEMH;
            on-error;
              Exsr GuardaLogDeErrores;
            endmon;
        EndSr;
        Begsr GuardaLogDeErrores;
            PROCNME    = PgmDs.PROCNME;
            STSCDE     = PgmDs.STSCDE;
            PRVSTSCDE  = PgmDs.PRVSTSCDE;
            SRCLINNBR  = PgmDs.SRCLINNBR;
            EXCPTSUBR  = PgmDs.EXCPTSUBR;
            NBRPARMS   = PgmDs.NBRPARMS;
            EXCPTTYP   = PgmDs.EXCPTTYP;
            EXCPTNBR   = PgmDs.EXCPTNBR;
            MSGWRKAREA = PgmDs.MSGWRKAREA;
            LIB        = PgmDs.LIB;
            RTVEXCPTDT = PgmDs.RTVEXCPTDT;
            EXCPTID    = PgmDs.EXCPTID;
            DTEJOBENTR = PgmDs.DTEJOBENTR;
            CNTJOBENTR = PgmDs.CNTJOBENTR;
            LASTFILEOP = PgmDs.LASTFILEOP;
            FILESTS    = PgmDs.FILESTS;
            JOBNME     = PgmDs.JOBNME;
            USER       = PgmDs.USER;
            JOBNMBR    = PgmDs.JOBNMBR;
            DTEJOBENT2 = PgmDs.DTEJOBENT2;
            DTEPGMRUN  = PgmDs.DTEPGMRUN;
            TMEPGMRUN  = PgmDs.TMEPGMRUN;
            COMPILEDTE = PgmDs.COMPILEDTE;
            COMPILETME = PgmDs.COMPILETME;
            COMPILELVL = PgmDs.COMPILELVL;
            SRCFILE    = PgmDs.SRCFILE   ;
            SRCLIB     = PgmDs.SRCLIB    ;
            SRCMBR     = PgmDs.SRCMBR    ;
            MODULEPGM  = PgmDs.MODULEPGM ;
            MODULEPROC = PgmDs.MODULEPROC;
            WRITE $ADQERRLOG;
        EndSr;
      //***************************************************************
      /End-free
     c     FecReal       begsr
     C                   call      FECTIM
     c                   parm                    Fecsys
     c                   parm                    Horasys
     c                   endsr
