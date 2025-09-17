Este es el código que tengo actualmente y que he construido con tu apoyo:
using Connections.Abstractions;
using Microsoft.IdentityModel.Tokens;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BCAH96DTA;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BNKPRD01;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Utils;
using QueryBuilder.Builders;
using QueryBuilder.Enums;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;

/// <summary>
/// Clase de servicio para el procesamiento de transacciones POS.
/// </summary>
/// <param name="_connection">Inyección de clase IDatabaseConnection.</param>
/// <param name="_contextAccessor">Inyección de clase IHttpContextAccessor.</param>
public class TransaccionesServices(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : ITransaccionesServices
{
    private string perfilTranserver = string.Empty; // Este valor debería ser dinámico o configurado según el contexto real.

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

        //LLamada a método FecReal, reemplaza llamado a CLLE fecha Real (FECTIM) ICBSUSER/FECTIM
        //  var (error, fecsys, horasys) = FecReal()

        //Llamada a método VerFecha, reemplaza llamado a CLLE VerFecha (DSCDT) BNKPRD01/TAP001
        var (found, yyyyMMdd) = VerFecha();
        if (!found) return BuildError("400", "No se pudo obtener la fecha del sistema.");

        _connection.Open(); //Abrimos la conexión a la base de datos

        //============================Validaciones Previas============================//

        // Normalización de importes: tolera "." o "," y espacios
        var deb = Utilities.ParseMonto(guardarTransaccionesDto.MontoDebitado);
        var cre = Utilities.ParseMonto(guardarTransaccionesDto.MontoAcreditado);

        //Obtenemos perfil transerver de la configuración global
        perfilTranserver = GlobalConnection.GetPerfilTranserver.PerfilTranserver;

        //Validamos, si no hay perfil transerver, retornamos error porque el proceso no puede continuar.
        if (perfilTranserver.IsNullOrEmpty()) return BuildError("400", "No se ha configurado el perfil transerver.");

        //Validamos que al menos uno de los montos sea mayor a 0, no se puede postear ambos en 0.
        if (deb <= 0m && cre <= 0m) return BuildError("400", "No hay importes a postear (ambos montos son 0).");

        //Validamos si existe el comercio en la tabla BCAH96DTA/IADQCOM
        var (existeComercio, codigoError, mensajeComercio) = BuscarComercio(guardarTransaccionesDto.NumeroCuenta, int.Parse(guardarTransaccionesDto.CodigoComercio));
        if (!existeComercio) return BuildError(codigoError, mensajeComercio);

        //Validación de Terminal
        var (existeTerminal, codigoErrorTerminal, mensajeTerminal) = BuscarTerminal(guardarTransaccionesDto.Terminal, int.Parse(guardarTransaccionesDto.CodigoComercio));
        if (!existeTerminal) return BuildError(codigoErrorTerminal, mensajeTerminal);

        //============================Fin Validaciones Previas============================//

        //============================Inicia Proceso Principal============================//

        // 1. Obtenemos el Perfil Transerver del cliente.
        var respuestaPerfil = VerPerfil(perfilTranserver);

        // Si no existe el perfil, retornar error y no continuar con el proceso.
        if (!respuestaPerfil.existePerfil) return BuildError(respuestaPerfil.codigoError, respuestaPerfil.descripcionError);

        // 2. Obtenemos el último lote de la tabla POP801 para el perfil transerver.
        //    Si no existe, se asume 0.
        //    Esto reemplaza la lógica de VerUltLote en RPGLE.
        var (ultimoLote, descripcionUltimoLote) = VerUltLote(perfilTranserver);

        // 3. Llamamos al método NuevoLote con el valor obtenido.
        //    Esto reemplaza la lógica de NuevoLote en RPGLE.
        var (numeroLote, existeLote) = NuevoLote(perfilTranserver, "usuario", ultimoLote, ultimoLote);
        if (!existeLote) return BuildError("400", "No se pudo crear un nuevo lote. " + descripcionUltimoLote);

        //Validar 
        int secuencia = 0;

        //Validación de naturaleza contable
        switch (guardarTransaccionesDto.NaturalezaContable)
        {
            case "C":
                //Proceso de crédito
                secuencia += 1;
                InsertPop802(
                    perfil: perfilTranserver,
                    lote: numeroLote,
                    seq: secuencia,
                    fechaYyyyMmDd: Convert.ToInt32(yyyyMMdd),
                    cuenta: guardarTransaccionesDto.NumeroCuenta,      // TSTACT: cuenta objetivo (cliente/comercio)
                    centroCosto: 0,                // TSWSCC: si requieres C.C., cámbialo aquí
                    codTrn: "0783",                // 0783 = Crédito (convención del core)
                    monto: cre,
                    al1: guardarTransaccionesDto.NombreComercio,       // leyenda 1
                    al2: $"{guardarTransaccionesDto.CodigoComercio}-{guardarTransaccionesDto.Terminal}", // leyenda 2
                    al3: $"&{EtiquetaConcepto(guardarTransaccionesDto.NaturalezaContable)}&{guardarTransaccionesDto.IdTransaccionUnico}&Cr Tot." // leyenda 3
                );
                break;
            case "D":
                //Proceso de débito
                secuencia += 1;
                InsertPop802(
                    perfil: perfilTranserver,
                    lote: numeroLote,
                    seq: secuencia,
                    fechaYyyyMmDd: Convert.ToInt32(yyyyMMdd),
                    cuenta: guardarTransaccionesDto.NumeroCuenta,
                    centroCosto: 0,
                    codTrn: "0784",                // 0784 = Débito (convención del core)
                    monto: deb,
                    al1: guardarTransaccionesDto.NombreComercio,
                    al2: $"{guardarTransaccionesDto.CodigoComercio}-{guardarTransaccionesDto.Terminal}",
                    al3: $"&{EtiquetaConcepto(guardarTransaccionesDto.NaturalezaContable)}&{guardarTransaccionesDto.IdTransaccionUnico}&Db Tot."
                );
                break;
            default:
                return BuildError("00001", "Naturaleza contable inválida.");
        }

        return BuildError(code: "200", message: "Transacción procesada correctamente.");
    }

    // ============================ Utilidades ============================

    /// <summary>
    /// Equivalente a: 
    /// <c>CALL FECTIM PARM(FAAAAMMDD HORA)</c>.
    /// </summary>
    /// <returns>
    /// (respuesta: true/false, fecsys: "yyyyMMdd" (8), horasys: "HHmmss" (7))
    /// </returns>
    public (bool respuesta, string fecsys, string horasys) FecReal()
    {
        // Variables de salida: simulan los PARM de CLLE.
        string fecsys = string.Empty;   // &FAAAAMMDD (8)
        string horasys = string.Empty;  // &HORA      (7)

        try
        {
            // ================== SQL generado ==================
            // SELECT
            //   CURRENT_DATE AS FECHA,
            //   CURRENT_TIME AS HORA
            // FROM SYSIBM.SYSDUMMY1
            // ==================================================
            var query = QueryBuilder.Core.QueryBuilder
                .From("SYSDUMMY1", "SYSIBM")
                .Select(
                    "CURRENT_DATE AS FECHA",
                    "CURRENT_TIME AS HORA"
                )
                .FetchNext(1)
                .Build();

            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = query.Sql;

            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
                return (false, fecsys, horasys);

            // Lectura directa por índice para máximo rendimiento
            fecsys = rd.GetString(0).Replace("-", "");                 // "yyyyMMdd" (8)
            horasys = rd.GetString(1).PadRight(7).Replace(".", "");     // "HHmmss" -> ajustado a LEN(7)

            return (true, fecsys, horasys);
        }
        catch
        {
            // Si hay error, mantenemos contrato similar al PGM (bandera false)
            return (false, fecsys, horasys);
        }
    }

    /// <summary>
    /// Lee DSCDT desde BNKPRD01.TAP001 (DSBK=001) y retorna:
    /// - el valor bruto DSCDT (CYYMMDD)
    /// - la fecha formateada YYYYMMDD
    /// </summary>
    /// <returns>seObtuvoFecha, dscdtCyyMmDd, yyyyMMdd</returns>
    private (bool seObtuvoFecha,string yyyyMMdd) VerFecha()
    {
        // Valores de salida predeterminados para conservar contrato estable.
        var dscdt = 0;            // valor crudo CYYMMDD
        var yyyyMMdd = string.Empty;

        try
        {
            // SELECT DSCDT FROM BNKPRD01.TAP001 WHERE DSBK = 1 FETCH FIRST 1 ROW ONLY
            // - Se usa DTO para habilitar lambdas tipadas y evitar cadenas mágicas.
            var query = QueryBuilder.Core.QueryBuilder
                .From("TAP001", "BNKPRD01")
                .Select("DSCDT")             // solo la columna necesaria
                .Where<Tap001>(x => x.DSBK == 1)          // DSBK = 001 en RPGLE
                .FetchNext(1)                              // equivalente a CHAIN + %FOUND
                .Build();

            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = query.Sql;

            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
                return (false, yyyyMMdd);

            // Lectura directa: índice 0 porque solo seleccionamos DSCDT.
            dscdt = rd.GetInt32(0);

            // Conversión de CYYMMDD → YYYYMMDD para uso homogéneo en .NET/SQL.
            yyyyMMdd = ConvertCyyMmDdToYyyyMmDd(dscdt);

            return (true, yyyyMMdd);
        }
        catch
        {             // En caso de error, retornamos valores predeterminados.
            return (false, yyyyMMdd);
        }
    }

    /// <summary>
    /// Convierte un entero en formato IBM i CYYMMDD (p. ej. 1240912) a "YYYYMMDD".
    /// </summary>
    /// <remarks>
    /// - C: siglo relativo a 1900 (0=>1900, 1=>2000, etc.).  
    /// - YY: año dentro del siglo.  
    /// - MM: mes, DD: día.
    /// </remarks>
    private static string ConvertCyyMmDdToYyyyMmDd(int cyymmdd)
    {
        // Separación de C, YY, MM, DD usando división/módulo para evitar parseos de string.
        var c = cyymmdd / 1000000;                 // dígito del siglo
        var yy = (cyymmdd / 10000) % 100;            // dos dígitos de año
        var mm = (cyymmdd / 100) % 100;            // mes
        var dd = cyymmdd % 100;            // día

        // Año absoluto: 1900 + (C * 100) + YY. Para C=1 => 2000+YY.
        var yyyy = 1900 + (c * 100) + yy;

        // Composición sin separadores para uso en sistemas que requieren 8 caracteres.
        return $"{yyyy:0000}{mm:00}{dd:00}";
    }


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
                .OrderBy("ADQCOME", QueryBuilder.Enums.SortDirection.Asc)
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
    private (bool existeTerminal, string codigoError, string mensajeTerminal) BuscarTerminal(string terminalRecibida, int codigoComercioRecibido)
    {
        try
        {
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
            if (reader.Read())
            {
                return (true, "00001", "Existe terminal."); // Terminal existe
            }
            return (false, "00002", "No existe terminal."); // Terminal no existe
        }
        catch (Exception ex)
        {
            // Manejo de errores en la consulta
            return (false, "0003", ex.Message); // Indica error al consultar comercio
        }
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
                .From("CFP801", "BCAH96DTA")
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
    /// Obtiene el último valor de FTSBT para un perfil dado (equivalente al VerUltlote en RPGLE).
    /// </summary>
    /// <param name="perfil">Clave de perfil que corresponde a FTTSKY.</param>
    /// <returns>El último valor de FTSBT encontrado o 0 si no existe.</returns>
    private (int ultimoLote, string descripcionUltimoLote) VerUltLote(string perfil)
    {
        // Variable resultado (equivalente a wFTSBT en RPGLE)
        int ultimoFTSBT = 0;

        try
        {

            // Construimos el query con QueryBuilder
            var ultimoLoteQuery = QueryBuilder.Core.QueryBuilder
                .From("POP801", "BCAH96DTA")   // Tabla POP801 en librería AS400
                .Select("FTSBT")               // Campo que queremos traer
                .WhereRaw("FTTSBK = 001")         // Condición fija de RPGLE
                .Where<Pop801>(x => x.FTTSKY == perfil) // Filtro dinámico por PERFIL
                .OrderBy("FTSBT DESC")         // Simula leer hasta el último FTSBT
                .FetchNext(1)                  // Solo el último
                .Build();

            using var command = _connection.GetDbCommand(ultimoLoteQuery, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                ultimoFTSBT = reader.GetInt32(0);
            }
            return (ultimoFTSBT, ultimoFTSBT > 0 ? "Se encontro Último." : "No se encontro último lote.");
        }
        catch (Exception ex)
        {
            return (0, ex.Message); // Retorna 0 en caso de error
        }
    }

    /// <summary>
    /// Inserta un nuevo lote en <c>BNKPRD01.POP801</c>.
    /// </summary>
    /// <param name="perfil">Valor para FTTSKY.</param>
    /// <param name="usuario">Valor para FTTSOR.</param>
    /// <param name="dsdt">Fecha operativa (CYYMMDD) para FTTSDT.</param>
    /// <param name="ultimoFtsbt">Último FTSBT existente (para calcular el siguiente).</param>
    /// <returns>El número de lote generado (FTSBT) y si se persistió correctamente.</returns>
    private (int numeroLote, bool existeLote) NuevoLote(string perfil, string usuario, int dsdt, int ultimoFtsbt)
    {
        // ► En RPG: wFTSBT = wFTSBT + 1; FTTSBK = 001; FTTSKY = PERFIL; FTSBT = wFTSBT; FTSST = 02; FTTSOR = Usuario; FTTSDT = DSDT; write Pop8011

        var siguienteFtsbt = ultimoFtsbt + 1; // número de lote que se insertará

        try
        {
            // IntoColumns define el orden de columnas; Row especifica los valores respetando ese orden.
            var insertNuevoLote = new InsertQueryBuilder("POP801", "BNKPRD01")
                .IntoColumns("FTTSBK", "FTTSKY", "FTTSBT", "FTTSST", "FTTSOR", "FTTSDT")
                .Row([1, perfil, siguienteFtsbt, 2, usuario, dsdt])
                .Build();

            using var cmd = _connection.GetDbCommand(insertNuevoLote, _contextAccessor.HttpContext!);

            var affected = cmd.ExecuteNonQuery(); // write Pop8011

            return (siguienteFtsbt, affected > 0);
        }
        catch
        {
            return (0, false); // En caso de error, retornamos 0 y false.}
        }
    }

    /// <summary>
    /// Inserta una fila en POP802 (detalle de posteo) con campos esenciales.
    /// </summary>
    private void InsertPop802(
        string perfil,
        int lote,
        int seq,
        int fechaYyyyMmDd,
        string cuenta,
        int centroCosto,
        string codTrn,
        decimal monto,
        string al1,
        string al2,
        string al3)
    {
        // Nota funcional: POP802 requiere varias columnas obligatorias del core.
        // Aquí posteamos lo esencial (override, fecha, cuenta, tcode, monto y leyendas).
        var pop802Sql = new InsertQueryBuilder("POP802", "BNKPRD01")
            .IntoColumns(
                "TSBK",    // Bank
                "TSTSKY",  // Perfil
                "TSBTCH",  // Lote
                "TSWSEQ",  // Secuencia
                "TSTOVR",  // Override
                "TSTTDT",  // Fecha efectiva (YYYYMMDD)
                "TSTACT",  // Cuenta
                "TSWSCC",  // Centro de costo
                "TSWTCD",  // Código de transacción
                "TSTCC",   // Monto
                "TSTAL1",  // Leyenda 1
                "TSTAL2",  // Leyenda 2
                "TSTAL3"   // Leyenda 3
            )
            .Row([
                1,
                perfil,
                lote,
                seq,
                "S",
                fechaYyyyMmDd,
                cuenta,
                centroCosto,
                codTrn,
                monto,
                Trunc(al1, 30),
                Trunc(al2, 30),
                Trunc(al3, 30)
            ])
            .Build();

        using var cmd = _connection.GetDbCommand(pop802Sql, _contextAccessor.HttpContext!);

        var aff = cmd.ExecuteNonQuery();

        if (aff <= 0) throw new InvalidOperationException("No se pudo insertar el detalle POP802.");
    }

    /// <summary>
    /// Método auxiliar para truncar cadenas a una longitud máxima.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    private static string Trunc(string? s, int max)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;
        if (s.Length <= max)
            return s;
        return s[..max];
    }

    /// <summary>Convierte "C"/"D" a etiqueta corta funcional.</summary>
    private static string EtiquetaConcepto(string nat) => (nat ?? "C").Equals("D", StringComparison.InvariantCultureIgnoreCase) ? "DB" : "CR";

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






Y este es el código que generaste, en que difieren ambos.
    
using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BCAH96DTA;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BNKPRD01;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using QueryBuilder.Builders;
using QueryBuilder.Enums;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;

/// <summary>
/// Servicio principal de procesamiento de transacciones por lote para comercios.
/// </summary>
/// <remarks>
/// - Encapsula el flujo contable de débito/crédito a nivel de comercio.
/// - Incluye validación de terminal virtual y cobros de e-commerce (membresía única). 2
/// - Mantiene compatibilidad lógica con el RPG original, pero usando QueryBuilder.
/// </remarks>
/// <param name="_connection">Conexión IDatabaseConnection (AS/400).</param>
/// <param name="_contextAccessor">Acceso a HttpContext para trazabilidad.</param>
public class TransaccionesServices(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : ITransaccionesServices
{
    private string _perfilTranserver = string.Empty; // Perfil transserver (TS). Puede provenir de DTAARA/ADQDTA si está disponible.

    /// <summary>
    /// Punto de entrada: guarda/postea un lote de transacciones de comercio.
    /// </summary>
    /// <remarks>
    /// - Normaliza importes y recupera fecha de proceso desde TAP001 (DSCDT/YYYMMDD). 3
    /// - Obtiene perfil TS (DTAARA/ADQDTA o fallback configurable).
    /// - Ejecuta asientos contables por comercio: neto liquidación, avance efectivo y e-commerce. 4
    /// </remarks>
    public async Task<RespuestaGuardarTransaccionesDto> GuardarTransaccionesAsync(GuardarTransaccionesDto dto)
    {
        await Task.Yield();

        // =================== Preparación ===================

        // -- Normalización básica de montos (entrada API sólo cliente; aquí operamos a nivel comercio)
        var montoDeb = ParseMonto(dto.montoDebitado);
        var montoCre = ParseMonto(dto.montoAcreditado);

        // -- Fecha de proceso (YYYYMMDD) desde TAP001.DSCDT
        var (okFecha, yyyymmdd) = VerFecha();
        if (!okFecha) return BuildError("400", "No se pudo obtener la fecha del sistema (TAP001)."); // TAP001/DSCDT

        // -- Perfil TransServer: se intenta leer desde DTAARA BCAH96DTA/ADQDTA; si falla, queda vacío (se puede inyectar por config)
        _perfilTranserver = TryGetPerfilFromDataArea() ?? _perfilTranserver;

        _connection.Open();

        // =================== Datos de negocio (comercio) ===================
        // -- Comercio por cuenta depósito (ADQ02COM.A02CTDE); de ahí se deriva nombre, giro, etc.
        var comercio = GetComercioByCuentaDeposito(dto.numeroCuenta);
        if (comercio is null)
            return BuildError("404", "No se encontró comercio por la cuenta de depósito (ADQ02COM).");

        // =================== Asientos contables por lote (empresa) ===================
        // Nota: la naturaleza contable del API es 'C' (crédito) o 'D' (débito).
        //       El flujo por comercio replica lo del RPG:
        //       - Asiento neto liquidación (Db y Cr)           5
        //       - Avance de efectivo (Db y Cr)                  6
        //       - Membresía única e-commerce (Db/Cr + cambio de estado)  7

        // -- Neto liquidación: se simula con los totales recibidos (montos ya agregados a nivel comercio).
        if (montoDeb > 0m || montoCre > 0m)
            PostearNetoLiquidacion(comercio, yyyymmdd, montoDeb, montoCre);

        // -- Avance de efectivo (si aplica): ejemplo usa campos del comercio/códigos contables del perfil.
        if (TieneAvanceEfectivo(dto))
            PostearAvanceEfectivo(comercio, yyyymmdd, ImporteAvance(dto));

        // -- E-commerce: si la terminal es virtual, calcula cobro de membresía y genera Db/Cr + actualización de estado.
        //    Basado en ValidaTerminal/CuentasControl/CambiaStsCobro del RPG. 8
        if (EsTerminalVirtual(dto.terminal))
            PostearEcommerce(comercio, yyyymmdd, dto);

        return new()
        {
            CodigoError = "0",
            DescripcionError = "OK",
            PerfilUsado = _perfilTranserver,
            FechaProceso = yyyymmdd,
            Comercio = new()
            {
                Codigo = comercio.A02COME.ToString(),
                Nombre = comercio.A02NACO,
                CuentaDeposito = comercio.A02CTDE?.Trim() ?? string.Empty
            }
        };
    }

    // =================== Auxiliares de dominio ===================

    /// <summary>Obtiene la fecha de proceso (YYYYMMDD) desde TAP001.DSCDT.</summary>
    /// <returns>Tupla (found, fecha).</returns>
    private (bool found, string yyyyMMdd) VerFecha()
    {
        // SELECT DSCDT FROM BNKPRD01/TAP001 WHERE DSBK = 001
        var sql = new SelectBuilder()
            .From("BNKPRD01", "TAP001")
            .Select("DSCDT")
            .Where("DSBK", Operador.Igual, 1)
            .Build();

        var fecha = _connection.ExecuteScalar<decimal?>(sql);
        if (fecha is null) return (false, "");

        // DSCDT viene como 9,0 (YYYYMMDD)
        var yyyyMMdd = ((decimal)fecha).ToString("00000000");
        return (true, yyyyMMdd);
    }

    /// <summary>Intenta obtener el perfil TS desde la data area ADQDTA (BCAH96DTA/ADQDTA).</summary>
    /// <remarks>
    /// Usa la tabla función <c>QSYS2.DATA_AREA_VALUE</c> si está disponible. En entornos sin PTFs, devolverá null.
    /// </remarks>
    private string? TryGetPerfilFromDataArea()
    {
        try
        {
            var sql = new SelectBuilder()
                .From("QSYS2", "DATA_AREA_VALUE")
                .Select("DATA_AREA_VALUE")
                .Where("SYSTEM_SCHEMA_NAME", Operador.Igual, "BCAH96DTA")
                .And("DATA_AREA_NAME", Operador.Igual, "ADQDTA")
                .Build();

            var value = _connection.ExecuteScalar<string?>(sql);
            return value?.Trim();
        }
        catch
        {
            // Ambientes donde DATA_AREA_VALUE no está disponible lanzan SQL0204/0206 (como viste).
            // En ese caso, se puede leer por programa o dejar perfil por configuración.
            return null;
        }
    }

    /// <summary>Busca comercio por cuenta de depósito (ADQ02COM.A02CTDE).</summary>
    private Adq02Com? GetComercioByCuentaDeposito(string cuentaDeposito)
    {
        var sql = new SelectBuilder()
            .From("BCAH96DTA", "ADQ02COM")
            .Select("A02COME", "A02NACO", "A02GICO", "A02CTDE")
            .Where("A02CTDE", Operador.Igual, cuentaDeposito.Trim())
            .Build();

        return _connection.QuerySingleOrDefault<Adq02Com>(sql);
    }

    /// <summary>Postea la pareja Db/Cr del neto de liquidación del comercio.</summary>
    private void PostearNetoLiquidacion(Adq02Com com, string fechaProc, decimal totalDb, decimal totalCr)
    {
        // Db (Net.Liq1) → cuenta contable 1 del perfil; Cr (Net.Liq2) → cuenta 2.
        // Esto replica el bloque Debito/Credito neto del RPG. 9
        if (totalDb > 0m)
            InsertPOP801Entry(new()
            {
                FTTSBK = 1,
                FTTSKY = _perfilTranserver,
                FTTSDT = ToPacked7(fechaProc),
                FTSBT = SiguienteNumeroLote(),
                FTTSOR = ContextUser(),
                FTTSSC = 0,
                FTTSDI = 1,
                FTTSCI = 0,
                FTTSID = totalDb,
                FTTSIC = 0m,
                FTTSDP = 0,
                FTTSCP = 0,
                FTTSPD = 0m,
                FTTSPC = 0m,
                FTTSBD = 0m,
                FTSLD = 0m,
                FTSBC = 0m,
                FTSLC = 0m,
            }, concepto: "VTA", descripcion: "Db Net.Liq1", naturaleza: 'D');

        if (totalCr > 0m)
            InsertPOP801Entry(new()
            {
                FTTSBK = 1,
                FTTSKY = _perfilTranserver,
                FTTSDT = ToPacked7(fechaProc),
                FTSBT = SiguienteNumeroLote(),
                FTTSOR = ContextUser(),
                FTTSSC = 0,
                FTTSDI = 0,
                FTTSCI = 1,
                FTTSID = 0m,
                FTTSIC = totalCr,
                FTTSDP = 0,
                FTTSCP = 0,
                FTTSPD = 0m,
                FTTSPC = 0m,
                FTTSBD = 0m,
                FTSLD = 0m,
                FTSBC = 0m,
                FTSLC = 0m,
            }, concepto: "VTA", descripcion: "Cr Net.Liq2", naturaleza: 'C');
    }

    /// <summary>Postea avance de efectivo (par Db/Cr) a nivel de comercio.</summary>
    private void PostearAvanceEfectivo(Adq02Com com, string fechaProc, decimal importe)
    {
        if (importe <= 0m) return;

        // Db Avance (cuenta 3) y Cr Avance (cuenta 10) siguiendo el RPG. 10
        InsertPOP801Entry(BuildDb(fechaProc, "AVA", "Db Ava.Efec", importe), naturaleza: 'D');
        InsertPOP801Entry(BuildCr(fechaProc, "AVA", "Cr Ava.Efec", importe), naturaleza: 'C');
    }

    /// <summary>Postea membresía única e-commerce (Db/Cr) y cambia estatus de cobro.</summary>
    private void PostearEcommerce(Adq02Com com, string fechaProc, GuardarTransaccionesDto dto)
    {
        // 1) Calcular/leer monto de membresía y cuentas de control (ADQECTL). 2) Db a rechazo o a cta comercio; 3) Cr a cta contable; 4) Cambiar estatus cobro.
        // Basado en CobrosTerminalVrt/CuentasControl/CambiaStsCobroMemUnica. 11

        var membership = ObtenerCobroMembresia(com.A02COME);
        if (membership <= 0m) return;

        // Db a cuenta rechazo o a cta depósito (según estado/validación) y luego Cr a cuenta contable de membresía
        InsertPOP801Entry(BuildDb(fechaProc, "MEM", "Db MemUnica", membership), naturaleza: 'D');
        InsertPOP801Entry(BuildCr(fechaProc, "MEM", "Cr MemUnica", membership), naturaleza: 'C');

        // Cambio de estado de cobro (PENDIENTE → PAGADO)
        CambiarEstatusCobro(com.A02COME);
    }

    // =================== Builders de POP801 (lote) ===================

    /// <summary>Crea entrada Db de POP801 con metadatos estándar.</summary>
    private Pop801 BuildDb(string yyyyMMdd, string concepto, string descripcion, decimal importe)
        => new()
        {
            FTTSBK = 1,
            FTTSKY = _perfilTranserver,
            FTTSDT = ToPacked7(yyyyMMdd),
            FTSBT = SiguienteNumeroLote(),
            FTTSOR = ContextUser(),
            FTTSSC = 0,
            FTTSDI = 1,
            FTTSCI = 0,
            FTTSID = importe,
            FTTSIC = 0m,
        };

    /// <summary>Crea entrada Cr de POP801 con metadatos estándar.</summary>
    private Pop801 BuildCr(string yyyyMMdd, string concepto, string descripcion, decimal importe)
        => new()
        {
            FTTSBK = 1,
            FTTSKY = _perfilTranserver,
            FTTSDT = ToPacked7(yyyyMMdd),
            FTSBT = SiguienteNumeroLote(),
            FTTSOR = ContextUser(),
            FTTSSC = 0,
            FTTSDI = 0,
            FTTSCI = 1,
            FTTSID = 0m,
            FTTSIC = importe,
        };

    /// <summary>Inserta una fila POP801 (tabla de lotes) usando QueryBuilder.</summary>
    /// <param name="row">Entidad lista para insertar.</param>
    /// <param name="concepto">Etiqueta funcional de negocio.</param>
    /// <param name="descripcion">Descripción legible.</param>
    /// <param name="naturaleza">'D' débito o 'C' crédito (sólo metadato, no cambia importes).</param>
    private void InsertPOP801Entry(Pop801 row, string concepto = "", string descripcion = "", char naturaleza = 'D')
    {
        // Insert directo. Se mantienen columnas de conteos e importes coherentes a la naturaleza.
        var insert = new InsertBuilder()
            .Into("BNKPRD01", "POP801")
            .Columns(
                "FTTSBK","FTTSKY","FTTSDT","FTSBT","FTTSOR","FTTSSC",
                "FTTSDI","FTTSCI","FTTSID","FTTSIC")
            .Values(row.FTTSBK, row.FTTSKY, row.FTTSDT, row.FTSBT, row.FTTSOR, 0,
                    row.FTTSDI, row.FTTSCI, row.FTTSID, row.FTTSIC)
            .Build();

        _connection.ExecuteNonQuery(insert);

        // Comentario interno: aquí podrías escribir POP802 (detalle) si lo necesitas a futuro.
    }

    // =================== Persistencia de “config” e-commerce ===================

    private decimal ObtenerCobroMembresia(decimal comercioId)
    {
        // SELECT ADQCOBRO04 (monto) FROM ADQCOBRO WHERE A02COME = :comercio AND ADQCOBRO18='PENDIENTE'  (simplificado)
        var sql = new SelectBuilder()
            .From("BCAH96DTA", "ADQCOBRO")
            .Select("ADQCOBRO04")
            .Where("A02COME", Operador.Igual, comercioId)
            .And("ADQCOBRO18", Operador.Igual, "PENDIENTE")
            .Build();

        var monto = _connection.ExecuteScalar<decimal?>(sql) ?? 0m;

        // Multiplicador por tasa USD si corresponde (GLC002), tal como en el RPG. 12
        var tasa = ObtenerTasaCompraUsd();
        return Decimal.Round(monto * tasa, 2);
    }

    private void CambiarEstatusCobro(decimal comercioId)
    {
        // UPDATE ADQCOBRO SET ADQCOBRO18='PAGADO' WHERE A02COME=:comercio (simplificado)
        var update = new UpdateBuilder()
            .Table("BCAH96DTA", "ADQCOBRO")
            .Set(("ADQCOBRO18", "PAGADO"))
            .Where("A02COME", Operador.Igual, comercioId)
            .Build();

        _connection.ExecuteNonQuery(update);
    }

    private decimal ObtenerTasaCompraUsd()
    {
        // SELECT GBBKXR FROM BNKPRD01/GLC002 WHERE GBBKCD=001 AND GBCRCD='USD' (setgt/readp del RPG) 13
        var sql = new SelectBuilder()
            .From("BNKPRD01", "GLC002")
            .Select("GBBKXR")
            .Where("GBBKCD", Operador.Igual, 1)
            .And("GBCRCD", Operador.Igual, "USD")
            .Build();

        return _connection.ExecuteScalar<decimal?>(sql) ?? 1m;
    }

    // =================== Utilitarios ===================

    private static decimal ParseMonto(string? s)
    {
        if (s.IsNullOrEmpty()) return 0m;
        _ = decimal.TryParse(s.Replace(",", "").Trim(), out var v);
        return v;
    }

    private static int ToPacked7(string yyyymmdd)
        => int.Parse(yyyymmdd); // POP801 almacena FTTSDT(7,0) juliano/efectivo; ajusta si tu formato es distinto.

    private static int SiguienteNumeroLote() => 1; // Placeholder: reemplazar por generador real (POP801.FTSBT 001–999).

    private string ContextUser() => _contextAccessor.HttpContext?.User?.Identity?.Name ?? "API";

    private static RespuestaGuardarTransaccionesDto BuildError(string code, string message)
        => new() { CodigoError = code, DescripcionError = message };
}

// =================== Modelos mínimos usados por el servicio ===================

/// <summary>Entidad ADQ02COM (campos usados por el servicio).</summary>
public class Adq02Com
{
    /// <summary>Clave del comercio para identificar el titular del lote.</summary>
    public decimal A02COME { get; set; }
    /// <summary>Nombre de comercio, informativo para reportes y auditoría.</summary>
    public string A02NACO { get; set; } = string.Empty;
    /// <summary>Cuenta de depósito del comercio (relación por número de cuenta de la petición).</summary>
    public string? A02CTDE { get; set; }
}

/// <summary>Entidad POP801 (tabla de encabezados de lote).</summary>
/// <remarks>Mapa directo de las columnas necesarias para el flujo de Db/Cr.</remarks>
public class Pop801
{
    public int FTTSBK { get; set; }                 // Bank Number
    public string FTTSKY { get; set; } = "";        // Transaction Server Profile
    public int FTTSDT { get; set; }                 // Processing Date - Effective (packed 7)
    public int FTSBT { get; set; }                  // Batch Number (001–999)
    public string FTTSOR { get; set; } = "";        // Originated By
    public int FTTSSC { get; set; }                 // File Status (no usado en inserción)
    public int FTTSDI { get; set; }                 // Total Debit Items Count
    public int FTTSCI { get; set; }                 // Total Credit Items Count
    public decimal FTTSID { get; set; }             // Total Debit Amount - LCYE
    public decimal FTTSIC { get; set; }             // Total Credit Amount - LCYE
} 

// Nota: La interfaz ITransaccionesServices ya existe en tu proyecto y expone GuardarTransaccionesAsync. 14

