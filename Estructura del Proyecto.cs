/// <summary>
/// Catálogo centralizado de códigos de resultado/negocio.
/// Todos los códigos son numéricos string de 5 dígitos.
/// Convención general (inspirada en semántica HTTP):
/// - 00000: OK
/// - 1xxxx: Informativos/No-errores (opcionales)
/// - 2xxxx: Idempotencia/Estado benigno (opcionales)
/// - 4xxxx: Errores de validación/negocio/datos (cliente)
/// - 5xxxx: Errores técnicos/sistema/integración (servidor)
/// </summary>
internal static class BizCodes
{
    // ==========================
    // ÉXITO / INFORMATIVO
    // ==========================

    /// <summary>Operación exitosa.</summary>
    public const string Ok = "00000";

    /// <summary>Operación ya aplicada previamente (idempotente).</summary>
    public const string YaProcesado = "20001";

    /// <summary>No hubo cambios que aplicar.</summary>
    public const string SinCambios = "20002";

    // ==========================
    // VALIDACIÓN (4xxxx)
    // ==========================

    // ---- Entradas requeridas / formato ----
    /// <summary>No hay importes a postear (ambos montos = 0).</summary>
    public const string NoImportes = "40001";

    /// <summary>No se configuró perfil transerver.</summary>
    public const string PerfilFaltante = "40002";

    /// <summary>Naturaleza contable inválida (debe ser 'C' o 'D').</summary>
    public const string NaturalezaInvalida = "40003";

    /// <summary>Moneda inválida o no provista.</summary>
    public const string MonedaInvalida = "40004";

    /// <summary>Monto negativo o inválido.</summary>
    public const string MontoInvalido = "40005";

    /// <summary>Cuenta del cliente vacía o inválida.</summary>
    public const string CuentaClienteVacia = "40006";

    /// <summary>Código de comercio vacío o inválido.</summary>
    public const string CodigoComercioVacio = "40007";

    /// <summary>Terminal vacía o inválida.</summary>
    public const string TerminalVacia = "40008";

    /// <summary>Descripciones exceden longitud máxima permitida.</summary>
    public const string DescripcionesLargas = "40009";

    /// <summary>No se pudo obtener fecha del sistema.</summary>
    public const string FechaSistemaNoDisponible = "40010";

    /// <summary>No se recibió identificador único de transacción.</summary>
    public const string IdUnicoFaltante = "40011";

    // ---- Autenticación / Autorización ----
    /// <summary>Usuario no autenticado.</summary>
    public const string UsuarioNoAutenticado = "40101";

    /// <summary>Permisos insuficientes para la operación.</summary>
    public const string PermisosInsuficientes = "40301";

    /// <summary>Operación bloqueada por políticas de negocio.</summary>
    public const string PoliticasBloquean = "40302";

    // ---- No encontrados ----
    /// <summary>Comercio no existe.</summary>
    public const string ComercioNoExiste = "40401";

    /// <summary>Terminal no existe.</summary>
    public const string TerminalNoExiste = "40402";

    /// <summary>Perfil transerver no existe.</summary>
    public const string PerfilNoExiste = "40403";

    /// <summary>Cuenta (cliente) no existe.</summary>
    public const string CuentaNoExiste = "40404";

    /// <summary>Cuenta interna (GL) no existe.</summary>
    public const string CuentaInternaNoExiste = "40405";

    // ---- Conflictos / Estados ----
    /// <summary>Id de transacción duplicado.</summary>
    public const string DuplicadoIdUnico = "40901";

    /// <summary>No fue posible reservar número de lote (conflicto).</summary>
    public const string LoteNoDisponible = "40902";

    /// <summary>Secuencia de lote agotada o inconsistente.</summary>
    public const string SecuenciaAgotada = "40903";

    /// <summary>Cuenta en estado que impide operación (bloqueo, restricción, etc.).</summary>
    public const string EstadoCuentaInvalido = "40904";

    // ---- Semántica / Reglas de negocio (422xx) ----
    /// <summary>No se encontraron reglas/definiciones aplicables.</summary>
    public const string ReglasNoEncontradas = "42200";

    /// <summary>No se encontró contrapartida GL/CC válida para el asiento.</summary>
    public const string ResolverGLFalta = "42201";

    /// <summary>Tipo de cuenta no reconocido (se esperaba 1/6/40).</summary>
    public const string TipoCuentaNoReconocido = "42202";

    /// <summary>Descripciones obligatorias faltantes para la operación.</summary>
    public const string DescripcionesObligatoriasFaltantes = "42203";

    /// <summary>Auto-balance deshabilitado o inconsistente contra el perfil.</summary>
    public const string AutoBalanceDeshabilitado = "42204";

    /// <summary>Reglas de e-commerce inconsistentes o incompletas.</summary>
    public const string ReglasEcommerceInconsistentes = "42205";

    /// <summary>Tasa de cambio faltante para moneda recibida.</summary>
    public const string TasaCambioFaltante = "42206";

    /// <summary>Cuentas de control inconsistentes con el t-code.</summary>
    public const string CuentasControlInconsistentes = "42207";

    /// <summary>Centro de costo inválido o no permitido.</summary>
    public const string CentroCostoNoValido = "42208";

    /// <summary>Error de validación específico de la cuenta.</summary>
    public const string ErrorValidacionCuenta = "42210";

    // ---- Otros (opcionales) ----
    /// <summary>Saldo insuficiente (si aplica en otros flujos).</summary>
    public const string SaldoInsuficiente = "40201";

    // ==========================
    // SISTEMA / INTEGRACIÓN (5xxxx)
    // ==========================

    // ---- Infraestructura / DB ----
    /// <summary>Error al cargar/agregar librerías a la LIBL.</summary>
    public const string LibreriasFail = "50010";

    /// <summary>Fallo de conexión a base de datos.</summary>
    public const string ConexionDbFallida = "50020";

    /// <summary>Timeout ejecutando comando en base de datos.</summary>
    public const string TimeoutDb = "50021";

    /// <summary>Deadlock o contención en base de datos.</summary>
    public const string DeadlockDb = "50022";

    /// <summary>Error SQL genérico (sintaxis/constraint/etc.).</summary>
    public const string ErrorSql = "50023";

    /// <summary>Error desconocido no categorizado.</summary>
    public const string ErrorDesconocido = "50099";

    // ---- Programas RPG/ILE (INT_LOTES y similares) ----
    /// <summary>El programa INT_LOTES devolvió error (CODER != 0).</summary>
    public const string IntLotesFail = "51001";

    /// <summary>Timeout llamando al programa INT_LOTES.</summary>
    public const string IntLotesTimeout = "51002";

    /// <summary>Parámetros inválidos para INT_LOTES (longitud/escala).</summary>
    public const string IntLotesParametroInvalido = "51003";

    /// <summary>Programa RPG/ILE no disponible o no encontrado.</summary>
    public const string ProgramaNoDisponible = "51004";

    /// <summary>Error de conversión de tipos al invocar programa (packed/decimal).</summary>
    public const string ErrorConversionTipos = "51005";

    /// <summary>Tamaño de parámetro fuera de especificación del programa.</summary>
    public const string TamanioParamInvalido = "51006";

    // ---- Entorno IBM i (LIBL/paths) ----
    /// <summary>No se pudo establecer LIBL (CHGLIBL/Addlible falló).</summary>
    public const string CargaLiblFail = "52001";

    /// <summary>No se pudo establecer esquema/corriente de librerías.</summary>
    public const string SetPathLiblFail = "52002";

    // ---- Transaccionalidad ----
    /// <summary>Error al confirmar la transacción (COMMIT).</summary>
    public const string CommitFail = "53001";

    /// <summary>Error al revertir la transacción (ROLLBACK).</summary>
    public const string RollbackFail = "53002";

    /// <summary>Bloqueo de tabla/registro impide la operación.</summary>
    public const string LockTabla = "53003";

    // ---- IO / Sistemas externos ----
    /// <summary>Error de IO o de filesystem.</summary>
    public const string IOError = "54001";

    /// <summary>Servicio externo no disponible.</summary>
    public const string ServicioExternoNoDisponible = "55001";

    /// <summary>Timeout llamando servicio externo.</summary>
    public const string ServicioExternoTimeout = "55002";

    /// <summary>Error devuelto por servicio externo.</summary>
    public const string ServicioExternoError = "55003";
}





using System.Net;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Utils
{
    /// <summary>
    /// Traductor de códigos de negocio (BizCodes) a códigos HTTP.
    /// - Usa mapeos específicos para casos conocidos.
    /// - Aplica reglas por prefijo para el resto.
    /// </summary>
    internal static class BizHttpMapper
    {
        // Mapeos explícitos (casos especiales/conocidos)
        private static readonly Dictionary<string, HttpStatusCode> Specific = new(StringComparer.OrdinalIgnoreCase)
        {
            // Éxitos / estados benignos
            [BizCodes.Ok]           = HttpStatusCode.OK,
            [BizCodes.YaProcesado]  = HttpStatusCode.OK,       // podría ser 208/200
            [BizCodes.SinCambios]   = HttpStatusCode.NoContent,

            // AuthZ/AuthN
            [BizCodes.UsuarioNoAutenticado] = HttpStatusCode.Unauthorized,
            [BizCodes.PermisosInsuficientes] = HttpStatusCode.Forbidden,

            // Not found
            [BizCodes.ComercioNoExiste]     = HttpStatusCode.NotFound,
            [BizCodes.TerminalNoExiste]     = HttpStatusCode.NotFound,
            [BizCodes.PerfilNoExiste]       = HttpStatusCode.NotFound,
            [BizCodes.CuentaNoExiste]       = HttpStatusCode.NotFound,
            [BizCodes.CuentaInternaNoExiste]= HttpStatusCode.NotFound,

            // Conflictos
            [BizCodes.DuplicadoIdUnico]     = HttpStatusCode.Conflict,
            [BizCodes.LoteNoDisponible]     = HttpStatusCode.Conflict,
            [BizCodes.SecuenciaAgotada]     = HttpStatusCode.Conflict,
            [BizCodes.EstadoCuentaInvalido] = HttpStatusCode.Conflict,

            // Semántica (422)
            [BizCodes.ReglasNoEncontradas]  = (HttpStatusCode)422,
            [BizCodes.ResolverGLFalta]      = (HttpStatusCode)422,
            [BizCodes.TipoCuentaNoReconocido] = (HttpStatusCode)422,
            [BizCodes.DescripcionesObligatoriasFaltantes] = (HttpStatusCode)422,
            [BizCodes.AutoBalanceDeshabilitado] = (HttpStatusCode)422,
            [BizCodes.ReglasEcommerceInconsistentes] = (HttpStatusCode)422,
            [BizCodes.TasaCambioFaltante]   = (HttpStatusCode)422,
            [BizCodes.CuentasControlInconsistentes] = (HttpStatusCode)422,
            [BizCodes.CentroCostoNoValido]  = (HttpStatusCode)422,
            [BizCodes.ErrorValidacionCuenta]= (HttpStatusCode)422,
            [BizCodes.SaldoInsuficiente]    = (HttpStatusCode)422, // si lo usas en otros flujos

            // Sistema/infra
            [BizCodes.LibreriasFail]        = HttpStatusCode.InternalServerError,
            [BizCodes.ConexionDbFallida]    = HttpStatusCode.ServiceUnavailable,
            [BizCodes.TimeoutDb]            = HttpStatusCode.GatewayTimeout,
            [BizCodes.DeadlockDb]           = HttpStatusCode.ServiceUnavailable, // transitorio
            [BizCodes.ErrorSql]             = HttpStatusCode.InternalServerError,
            [BizCodes.ErrorDesconocido]     = HttpStatusCode.InternalServerError,

            // Programas RPG/ILE (INT_LOTES)
            [BizCodes.IntLotesFail]             = HttpStatusCode.BadGateway,       // backend/upstream falla
            [BizCodes.IntLotesTimeout]          = HttpStatusCode.GatewayTimeout,
            [BizCodes.IntLotesParametroInvalido]= HttpStatusCode.InternalServerError, // llegó mal al programa
            [BizCodes.ProgramaNoDisponible]     = HttpStatusCode.BadGateway,
            [BizCodes.ErrorConversionTipos]     = HttpStatusCode.InternalServerError,
            [BizCodes.TamanioParamInvalido]     = HttpStatusCode.InternalServerError,

            // Entorno IBM i (LIBL)
            [BizCodes.CargaLiblFail]        = HttpStatusCode.InternalServerError,
            [BizCodes.SetPathLiblFail]      = HttpStatusCode.InternalServerError,

            // Transaccionalidad
            [BizCodes.CommitFail]           = HttpStatusCode.InternalServerError,
            [BizCodes.RollbackFail]         = HttpStatusCode.InternalServerError,
            [BizCodes.LockTabla]            = HttpStatusCode.ServiceUnavailable, // transitorio

            // IO / Externos
            [BizCodes.IOError]                  = HttpStatusCode.InternalServerError,
            [BizCodes.ServicioExternoNoDisponible] = HttpStatusCode.ServiceUnavailable,
            [BizCodes.ServicioExternoTimeout]      = HttpStatusCode.GatewayTimeout,
            [BizCodes.ServicioExternoError]        = HttpStatusCode.BadGateway,
        };

        /// <summary>
        /// Devuelve el <see cref="HttpStatusCode"/> para el <paramref name="bizCode"/>.
        /// Aplica:
        /// 1) Coincidencias exactas (tabla Specific)
        /// 2) Reglas por prefijo (401/403/404/409/422/40/5…)
        /// </summary>
        public static HttpStatusCode ToHttpStatus(string? bizCode)
        {
            if (string.IsNullOrWhiteSpace(bizCode))
                return HttpStatusCode.InternalServerError;

            bizCode = bizCode.Trim();

            // 1) Mapeo específico
            if (Specific.TryGetValue(bizCode, out var exact))
                return exact;

            // 2) Reglas por prefijo (más específicas primero)
            if (bizCode.StartsWith("401", StringComparison.Ordinal)) return HttpStatusCode.Unauthorized;
            if (bizCode.StartsWith("403", StringComparison.Ordinal)) return HttpStatusCode.Forbidden;
            if (bizCode.StartsWith("404", StringComparison.Ordinal)) return HttpStatusCode.NotFound;
            if (bizCode.StartsWith("409", StringComparison.Ordinal)) return HttpStatusCode.Conflict;
            if (bizCode.StartsWith("422", StringComparison.Ordinal)) return (HttpStatusCode)422;

            // Otros 4xx => BadRequest
            if (bizCode.StartsWith("40", StringComparison.Ordinal))  return HttpStatusCode.BadRequest;

            // Timeouts/conectividad (reglas genéricas útiles)
            if (bizCode is "50021" or "51002" or "55002")            return HttpStatusCode.GatewayTimeout;
            if (bizCode is "50020" or "50022" or "53003" or "55001") return HttpStatusCode.ServiceUnavailable;
            if (bizCode is "51004" or "55003")                       return HttpStatusCode.BadGateway;

            // Cualquier 5xx no clasificado => 500
            if (bizCode.StartsWith("5", StringComparison.Ordinal))   return HttpStatusCode.InternalServerError;

            // Fallback
            return HttpStatusCode.InternalServerError;
        }

        /// <summary>Devuelve el status como entero (ej. 422) para usar en <c>StatusCode(...)</c>.</summary>
        public static int ToHttpStatusInt(string? bizCode) => (int)ToHttpStatus(bizCode);
    }
}



var code = resultadoNegocio.Codigo ?? BizCodes.ErrorDesconocido;
var http = BizHttpMapper.ToHttpStatusInt(code);
return StatusCode(http, new {
    code,
    message = resultadoNegocio.Mensaje,
    data = resultadoNegocio.Payload
});
