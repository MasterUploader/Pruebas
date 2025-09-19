Este es el código como lo tengo actualmente, a partir de el aplica las mejoras, no alteres lo que ya existe a menos que sea necesario:

using Connections.Abstractions;
using Microsoft.IdentityModel.Tokens;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BCAH96DTA;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BNKPRD01;
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
public class TransaccionesServices(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : ITransaccionesServices
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

        //LLamada a método FecReal, reemplaza llamado a CLLE fecha Real (FECTIM) ICBSUSER/FECTIM
        //  var (error, fecsys, horasys) = FecReal()

        //Llamada a método VerFecha, reemplaza llamado a CLLE VerFecha (DSCDT) BNKPRD01/TAP001
        var (seObtuvoFecha, yyyyMMdd, fechaJuliana) = VerFecha();
        if (!seObtuvoFecha) return BuildError("400", "No se pudo obtener la fecha del sistema.");

        //============================Validaciones Previas============================//

        // Normalización de importes: tolera "." o "," y espacios
        var deb = Utilities.ParseMonto(guardarTransaccionesDto.MontoDebitado);
        var cre = Utilities.ParseMonto(guardarTransaccionesDto.MontoAcreditado);

        //Validamos que al menos uno de los montos sea mayor a 0, no se puede postear ambos en 0.
        if (deb <= 0m && cre <= 0m) return BuildError("400", "No hay importes a postear (ambos montos son 0).");

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

        //    Esto reemplaza la lógica de NuevoLote en RPGLE.
        // (A) Reservar número de lote 
        var (numeroLote, reservado) = ReservarNumeroLote(perfilTranserver, Convert.ToInt32(yyyyMMdd), "TBBANEGA"); //El usuario debe ser YBANET
        if (!reservado) return BuildError("400", "No fue posible reservar un número de lote (POP801).");

        // (B) Preparar reglas y postear desglose
        var nat = guardarTransaccionesDto.NaturalezaContable;             // "C" o "D"
        var montoBruto = nat == "C" ? cre : deb;

        // Si tienes una validación real de terminal virtual, úsala; aquí dejo false por defecto
        var reglas = ObtenerReglasCargos(perfilTranserver, int.Parse(guardarTransaccionesDto.CodigoComercio), esTerminalVirtual);

        // Si quieres continuar numeración de secuencia que ya traías:
        var secuencia = 0;

        var posteoDesglose = PostearDesglose(
            perfil: perfilTranserver,
            numeroLote: numeroLote,
            fechaYyyyMmDd: Convert.ToInt32(yyyyMMdd),
            naturalezaPrincipal: nat,
            cuentaComercio: guardarTransaccionesDto.NumeroCuenta,
            totalBruto: montoBruto,
            reglas: reglas,
            codComercio: guardarTransaccionesDto.CodigoComercio,
            terminal: guardarTransaccionesDto.Terminal,
            nombreComercio: guardarTransaccionesDto.NombreComercio,
            idUnico: guardarTransaccionesDto.IdTransaccionUnico,
            secuenciaInicial: secuencia
        );

        if (!posteoDesglose) return BuildError("400", "No fue posible postear el detalle de la transacción (POP802).");

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
            var query = new SelectQueryBuilder("SYSDUMMY1", "SYSIBM")
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
    private (bool seObtuvoFecha, string yyyyMMdd, string fechaJuliana) VerFecha()
    {
        // Valores de salida predeterminados para conservar contrato estable.
        var dscdt = 0;            // valor crudo CYYMMDD
        var dsdt = 0;            // valor crudo juliano
        var yyyyMMdd = string.Empty;
        var fechaJuliana = string.Empty;

        try
        {
            // ================== SQL generado ==================
            // SELECT DSCDT, DSDT
            // FROM BNKPRD01.TAP001
            // WHERE DSBK = 1 FETCH
            // FIRST 1
            // ROW ONLY
            // ==================================================
            // Construimos consulta SQL con QueryBuilder
            var query = new SelectQueryBuilder("TAP001", "BNKPRD01")
                .Select("DSCDT", "DSDT")             // Campos de fecha Dscdt (CYYMMDD) y Dsdt (juliana)
                .Where<Tap001>(x => x.DSBK == 1)          // DSBK = 001 en RPGLE
                .FetchNext(1)                              // equivalente a CHAIN + %FOUND
                .Build();

            using var cmd = _connection.GetDbCommand(query, _contextAccessor.HttpContext!);

            using var reader = cmd.ExecuteReader();

            // Si hay filas, leemos la primera (solo debe haber una)
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    // -- Mapeo directo desde columnas proyectadas.
                    dscdt = Convert.ToInt32(reader["DSCDT"]);
                    dsdt = Convert.ToInt32(reader["DSDT"]);
                }

                string fechaStr = ((long)dscdt).ToString("D8"); // Asegura 8 dígitos
                string dd = fechaStr[..2];
                string mm = fechaStr.Substring(2, 2);
                string yy = fechaStr.Substring(4, 4);

                yyyyMMdd = yy + mm + dd; // Formato YYYYMMDD

                fechaJuliana = dsdt.ToString("D7"); // Formato juliano con 7 dígitos

                return (true, yyyyMMdd, fechaJuliana);
            }
            return (false, yyyyMMdd, fechaJuliana); // No se encontró registro
        }
        catch
        {             // En caso de error, retornamos valores predeterminados.
            return (false, yyyyMMdd, fechaJuliana);
        }
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

    /// <summary>
    /// Obtiene el siguiente FTSBT para un perfil de forma segura y lo reserva insertando POP801 base.
    /// </summary>
    /// <remarks>
    /// - Usa el propio INSERT como lock optimista; si colisiona, reintenta.
    /// - Si llega a 999, vuelve a 1 (ajústalo si tu negocio requiere otra política).
    /// </remarks>
    private (int ftsbt, bool ok) ReservarNumeroLote(string perfil, int dsdt, string usuario)
    {
        int fttsdt = Convert.ToInt32(Utilities.ToJulian(dsdt.ToString()));
        for (var intento = 0; intento < 5; intento++)
        {
            // 1) MAX(FTSBT) para ese perfil/banco
            var sel = QueryBuilder.Core.QueryBuilder
                .From("POP801", "BNKPRD01")
                .Select("COALESCE(MAX(FTSBT), 0) AS MAXFTSBT")
                .Where<Pop801>(x => x.FTTSBK == 1)
                .Where<Pop801>(x => x.FTTSKY == perfil)
                .Build();

            int max;
            using (var selCmd = _connection.GetDbCommand(sel, _contextAccessor.HttpContext!))
            {
                var obj = selCmd.ExecuteScalar();
                max = obj is null || obj is DBNull ? 0 : Convert.ToInt32(obj, CultureInfo.InvariantCulture);
            }

            // 2) Proponer siguiente (wrap 999→1)
            var next = max >= 999 ? 1 : max + 1;

            // 3) Intentar reservar insertando el encabezado base
            var ins = new InsertQueryBuilder("POP801", "BNKPRD01")
                .IntoColumns("FTTSBK", "FTTSKY", "FTSBT", "FTTSST", "FTTSOR", "FTTSDT",
                             "FTTSDI", "FTTSCI", "FTTSID", "FTTSIC", "FTTSDP", "FTTSCP",
                             "FTTSPD", "FTTSPC", "FTTSBD", "FTTSLD", "FTTSBC", "FTTSLC")
                .Row([
                    1, perfil, next, 2, usuario, fttsdt,
                0, 0, 0m, 0m, 0, 0,
                0m, 0m, 0m, 0m, 0m, 0m
                ])
                .Build();

            try
            {
                using var insCmd = _connection.GetDbCommand(ins, _contextAccessor.HttpContext!);
                var aff = insCmd.ExecuteNonQuery();
                if (aff > 0) return (next, true); // reservado con éxito
            }
            catch
            {
                // Colisión de clave (otro hilo tomó el número): reintentar
            }
        }

        return (0, false);
    }


    /// <summary>
    /// Orquestador: trae reglas base (IADQCTL) y fusiona con e-commerce (ADQECTL) si aplica.
    /// </summary>
    private List<ReglaCargo> ObtenerReglasCargos(string perfil, int comercio, bool esTerminalVirtual)
    {
        var baseRules = ObtenerReglasDesdeIadqctl(perfil, comercio); //Esto no busca por tipo
        return MergeConEcommerce(baseRules, comercio, esTerminalVirtual);
    }

    /// <summary>
    /// Lee reglas base (sólo cuentas GL y metadatos) desde IADQCTL/ADQCTL.
    /// </summary>
    /// <param name="control">Valor de ADQCONT (ej. 'TS').</param>
    /// <param name="numero">Valor de ADQNUM (identificador).</param>
    /// <remarks>
    /// ADQCTL no posee porcentajes ni montos fijos; esos deben venir de otras PF.
    /// </remarks>
    private List<ReglaCargo> ObtenerReglasDesdeIadqctl(string control, decimal numero)
    {
        var reglas = new List<ReglaCargo>();

        var q = QueryBuilder.Core.QueryBuilder
            .From("IADQCTL", "BCAH96DTA")
            .Select(
                // cuentas GL típicas para intereses/comisión/IVA; ajusta si usas otras posiciones
                "ADQCNT1 AS CTA_INT",
                "ADQCNT2 AS CTA_COM",
                "ADQCNT3 AS CTA_IVA",
                // opcional: códigos de trn y naturalezas por si los quieres mapear
                "ADQCTR1 AS TCD_INT",
                "ADQCTR2 AS TCD_COM",
                "ADQCTR3 AS TCD_IVA",
                "ADQDB1  AS NAT_INT",
                "ADQDB2  AS NAT_COM",
                "ADQDB3  AS NAT_IVA"
            )
            .Where<Adqctl>(x => x.ADQCONT == control)
            .Where<Adqctl>(x => x.ADQNUM == numero)
            .FetchNext(1)
            .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return reglas;

        static string Gl(object o) => (o is DBNull) ? "" : Convert.ToString(o)!.Trim();

        // Como ADQCTL no guarda % ni montos fijos, ambos quedan en 0 (se complementan con otras reglas)
        var rInt = new ReglaCargo { Codigo = "INT", CuentaGl = Gl(rd["CTA_INT"]), Porcentaje = 0m, MontoFijo = 0m };
        var rCom = new ReglaCargo { Codigo = "COM", CuentaGl = Gl(rd["CTA_COM"]), Porcentaje = 0m, MontoFijo = 0m };
        var rIva = new ReglaCargo { Codigo = "IVA", CuentaGl = Gl(rd["CTA_IVA"]), Porcentaje = 0m, MontoFijo = 0m };

        if (!rInt.CuentaGl.IsNullOrEmpty()) reglas.Add(rInt);
        if (!rCom.CuentaGl.IsNullOrEmpty()) reglas.Add(rCom);
        if (!rIva.CuentaGl.IsNullOrEmpty()) reglas.Add(rIva);

        return reglas;
    }

    /// <summary>
    /// Funde reglas e-commerce (ADQECTL) con las base si la terminal es virtual.
    /// </summary>
    private List<ReglaCargo> MergeConEcommerce(List<ReglaCargo> baseRules, int comercio, bool esTerminalVirtual)
    {
        if (!esTerminalVirtual) return baseRules;

        var reglas = baseRules.ToDictionary(r => r.Codigo, r => r);

        var q = QueryBuilder.Core.QueryBuilder
            .From("ADQECTL", "BCAH96DTA")
            .Select(
                "ADQECNT1 AS CTA_INT_EC",
                "ADQECNT5 AS CTA_COM_EC",
                "ADQECTR1 AS PCT_INT_EC",
                "ADQECTR5 AS PCT_COM_EC"
            )
            .WhereRaw($"A02COME = {comercio}")
            .FetchNext(1)
            .Build();

        using var cmd = _connection.GetDbCommand(q, _contextAccessor.HttpContext!);
        using var rd = cmd.ExecuteReader();
        if (!rd.Read()) return baseRules;

        static string gl(object? o) => o is DBNull or null ? "" : Convert.ToString(o)!.Trim();
        static decimal pct(object? o) => o is DBNull or null ? 0m : Convert.ToDecimal(o) / 100m;

        var ctaInt = gl(rd["CTA_INT_EC"]);
        var ctaCom = gl(rd["CTA_COM_EC"]);
        var pctInt = pct(rd["PCT_INT_EC"]);
        var pctCom = pct(rd["PCT_COM_EC"]);

        if (!ctaInt.IsNullOrEmpty() || pctInt > 0m)
        {
            if (!reglas.TryGetValue("INT", out var r))
                r = reglas["INT"] = new() { Codigo = "INT" };
            if (!ctaInt.IsNullOrEmpty()) r.CuentaGl = ctaInt;
            r.Porcentaje += pctInt;
        }
        if (!ctaCom.IsNullOrEmpty() || pctCom > 0m)
        {
            if (!reglas.TryGetValue("COM", out var r))
                r = reglas["COM"] = new() { Codigo = "COM" };
            if (!ctaCom.IsNullOrEmpty()) r.CuentaGl = ctaCom;
            r.Porcentaje += pctCom;
        }

        return [.. reglas.Values.Where(r => !r.CuentaGl.IsNullOrEmpty() && (r.Porcentaje > 0m || r.MontoFijo > 0m))];
    }

    /// <summary>
    /// Aplica reglas y postea: 1 línea principal (neto) + N líneas de cargos (opuestas).
    /// También actualiza totales del POP801.
    /// </summary>
    private bool PostearDesglose(
        string perfil,
        int numeroLote,
        int fechaYyyyMmDd,
        string naturalezaPrincipal,
        string cuentaComercio,
        decimal totalBruto,
        List<ReglaCargo> reglas,
        string codComercio,
        string terminal,
        string nombreComercio,
        string idUnico,
        int secuenciaInicial = 0)
    {
        var cargos = CalcularCargos(totalBruto, reglas);
        var totalCargos = cargos.Sum(x => x.Monto);
        var neto = Decimal.Round(totalBruto - totalCargos, 2, MidpointRounding.AwayFromZero);

        var seq = secuenciaInicial + 1;

        // Línea principal (misma naturaleza del request)
        InsertPop802(
            perfil: perfil,
            lote: numeroLote,
            seq: seq,
            fechaYyyyMmDd: fechaYyyyMmDd,
            cuenta: cuentaComercio,
            centroCosto: 0,
            codTrn: naturalezaPrincipal == "C" ? "0783" : "0784",
            monto: neto,
            al1: Trunc(nombreComercio, 30),
            al2: Trunc($"{codComercio}-{terminal}", 30),
            al3: Trunc($"&{EtiquetaConcepto(naturalezaPrincipal)}&{idUnico}&Neto", 30)
        );
        ActualizarTotalesPop801(perfil, numeroLote, naturalezaPrincipal, neto);

        // Cargos (naturaleza opuesta)
        var natCargo = naturalezaPrincipal == "C" ? "D" : "C";
        foreach (var c in cargos.Where(x => x.Monto > 0m))
        {
            seq += 1;

            InsertPop802(
                perfil: perfil,
                lote: numeroLote,
                seq: seq,
                fechaYyyyMmDd: fechaYyyyMmDd,
                cuenta: c.CuentaGl,
                centroCosto: 0,
                codTrn: natCargo == "C" ? "0783" : "0784",
                monto: c.Monto,
                al1: Trunc(nombreComercio, 30),
                al2: Trunc($"{codComercio}-{terminal}", 30),
                al3: Trunc($"&{c.Codigo}&{idUnico}&Cargo", 30)
            );

            ActualizarTotalesPop801(perfil, numeroLote, natCargo, c.Monto);
        }

        return seq > 0;
    }

    /// <summary>
    /// Calcula la lista de cargos aplicando porcentaje y/o monto fijo sobre el total.
    /// </summary>
    private static List<CargoCalculado> CalcularCargos(decimal totalBruto, List<ReglaCargo> reglas)
    {
        var res = new List<CargoCalculado>();
        foreach (var r in reglas)
        {
            var mp = r.Porcentaje > 0m ? Decimal.Round(totalBruto * r.Porcentaje, 2, MidpointRounding.AwayFromZero) : 0m;
            var mf = r.MontoFijo > 0m ? r.MontoFijo : 0m;
            var monto = mp + mf;
            if (monto <= 0m) continue;

            res.Add(new()
            {
                Codigo = r.Codigo,
                CuentaGl = r.CuentaGl,
                Monto = monto
            });
        }
        return res;
    }

    /// <summary>
    /// Incrementa conteos e importes del encabezado del lote (POP801) según naturaleza.
    /// </summary>
    private void ActualizarTotalesPop801(string perfil, int lote, string naturaleza, decimal monto)
    {
        // Forzamos el punto decimal (evita coma por cultura local)
        var m = monto.ToString(CultureInfo.InvariantCulture);

        UpdateQueryBuilder updBuilder;

        if (naturaleza == "C")
        {
            // Crédito: FTTSCI y FTTSIC +=  monto
            updBuilder = new UpdateQueryBuilder("POP801", "BNKPRD01")
                .SetRaw("FTTSCI", "FTTSCI + 1")
                .SetRaw("FTTSIC", $"FTTSIC + {m}")
                .Where<Pop801>(x => x.FTTSBK == 1)
                .Where<Pop801>(x => x.FTTSKY == perfil)
                .Where<Pop801>(x => x.FTSBT == lote);
        }
        else
        {
            // Débito: FTTSDI y FTTSID += monto
            updBuilder = new UpdateQueryBuilder("POP801", "BNKPRD01")
                .SetRaw("FTTSDI", "FTTSDI + 1")
                .SetRaw("FTTSID", $"FTTSID + {m}")
                .Where<Pop801>(x => x.FTTSBK == 1)
                .Where<Pop801>(x => x.FTTSKY == perfil)
                .Where<Pop801>(x => x.FTSBT == lote);
        }

        var upd = updBuilder.Build();

        using var cmd = _connection.GetDbCommand(upd, _contextAccessor.HttpContext!);
        _ = cmd.ExecuteNonQuery();
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


