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
