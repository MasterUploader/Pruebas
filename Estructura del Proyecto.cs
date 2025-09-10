using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.DetalleTarjetaImprimir;
using QueryBuilder.Core; // <- tu namespace de entrada al builder (ajústalo si difiere)
using System.Data.Common;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Repository.IRepository.DetalleTarjetaImprimir;

/// <summary>
/// Repositorio para consulta de tarjetas a imprimir con resolución en una sola consulta SQL.
/// Integra subconsultas correlacionadas para evitar múltiples viajes a BD y suprime el patrón N+1.
/// Compatible con AS400 utilizando <c>FETCH FIRST 1 ROWS ONLY</c> en subconsultas.
/// </summary>
/// <param name="_connection">Proveedor de conexión (IDatabaseConnection) abstraído para AS400.</param>
/// <param name="_contextAccessor">Acceso a <see cref="HttpContext"/> para obtener trazabilidad y logging.</param>
public class DetalleTarjetasImprimirRepository(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor)
{
    /// <summary>
    /// Respuesta acumulada del proceso; se reutiliza durante la ejecución del request.
    /// </summary>
    protected GetDetallesTarjetasImprimirResponseDto _getDetalleTarjetasImprimirResponseDto = new();

    /// <summary>
    /// Obtiene, en una sola consulta, las tarjetas pendientes de impresión según filtros de BIN, agencias y antigüedad.
    /// Incorpora datos relacionados mediante subconsultas: nombre en tarjeta (UNI00L33), número de cuenta (UNI01L19) y nombre de estatus (UNI5500),
    /// así como los nombres de agencia (CFP10201) para agencia que imprime y agencia de apertura.
    /// </summary>
    /// <param name="bin">Código BIN del producto.</param>
    /// <param name="agenciaImprime">Centro de costo que imprime la tarjeta.</param>
    /// <param name="agenciaApertura">Centro de costo de apertura de la tarjeta.</param>
    /// <remarks>
    /// Funcionalidades clave:
    /// <list type="bullet">
    /// <item><description>Elimina N+1 mediante subconsultas correlacionadas en <c>SELECT</c>.</description></item>
    /// <item><description>Respeta filtros de “no impresa” (<c>ST_FECHA_IMPRESION=0</c>, <c>ST_HORA_IMPRESION=0</c>, <c>ST_USUARIO_IMPRESION=''</c>).</description></item>
    /// <item><description>Restringe por antigüedad mínima (<c>ST_FECHA_APERTURA &gt;= fecha_corte</c>).</description></item>
    /// <item><description>Construcción con <c>QueryBuilder</c> para SQL claro, mantenible y tipado cuando aplica.</description></item>
    /// </list>
    /// </remarks>
    public async Task<GetDetallesTarjetasImprimirResponseDto> BusquedaUNI5400(string bin, int agenciaImprime, int agenciaApertura)
    {
        try
        {
            // -- Cálculo de fecha de corte para el filtro de antigüedad (formato entero yyyymmdd).
            int dias = Convert.ToInt32(API_1_TERCEROS_REMESADORAS.Utilities.GlobalConnection.Current.DiasConsultaTarjeta);
            var today = DateTime.Today;
            var daysAgo = today.AddDays(-dias);
            int numericDate = int.Parse(daysAgo.ToString("yyyyMMdd"));

            // -- Aliases para legibilidad dentro del builder (evita errores y facilita el ORDER BY).
            var u54 = "U54";
            var subNombre = "Nombre";                 // Alias para MT_NET_EMBOSE
            var subCuenta = "NumeroCuenta";           // Alias para CA_CTA_COD_CUENTA
            var subEstatus = "Motivo";                // Alias para ES_NOMBRE_ESTATUS
            var agImpCod = "AgenciaImprimeCodigo";
            var agImpNom = "AgenciaImprimeNombre";
            var agApeCod = "AgenciaAperturaCodigo";
            var agApeNom = "AgenciaAperturaNombre";

            // -- Subconsulta: Nombre en tarjeta (UNI00L33) correlacionada por código de tarjeta.
            //    FETCH FIRST asegura 1 fila y mantiene compatibilidad AS400.
            string subSelNombre =
                "(SELECT MTA.MT_NET_EMBOSE FROM S38FILEBA.UNI00L33 MTA " +
                $"WHERE MTA.MT_CTJ_COD_TARJETA = {u54}.ST_CODIGO_TARJETA FETCH FIRST 1 ROWS ONLY)";

            // -- Subconsulta: Cuenta priorizando tipo 10/20 y uso especial 'R'.
            string subSelCuenta =
                "(SELECT CAS.CA_CTA_COD_CUENTA FROM S38FILEBA.UNI01L19 CAS " +
                $"WHERE CAS.CA_CTJ_COD_TARJETA = {u54}.ST_CODIGO_TARJETA " +
                "AND (CAS.CA_TIPO_CUENTA IN (10,20)) AND CAS.CA_IUE_USO_ESPECIAL = 'R' " +
                "FETCH FIRST 1 ROWS ONLY)";

            // -- Subconsulta: Nombre de estatus por código de estatus del proceso de la tarjeta.
            string subSelEstatus =
                "(SELECT ES.ES_NOMBRE_ESTATUS FROM S38FILEBA.UNI5500 ES " +
                $"WHERE ES.ES_ESTATUS_TARJETA = {u54}.ST_ESTATUS_PROCESO_TARJETA " +
                "FETCH FIRST 1 ROWS ONLY)";

            // -- Subconsultas: Nombres de agencia (CFP10201). Se filtra por CFBANK=1 y CFBRCH = centro de costo.
            string subSelAgImpNom =
                "(SELECT C1.CFBRNM FROM BNKPRD01.CFP10201 C1 " +
                $"WHERE C1.CFBANK=1 AND C1.CFBRCH={u54}.ST_CENTRO_COSTO_IMPR_TARJETA " +
                "FETCH FIRST 1 ROWS ONLY)";

            string subSelAgApeNom =
                "(SELECT C2.CFBRNM FROM BNKPRD01.CFP10201 C2 " +
                $"WHERE C2.CFBANK=1 AND C2.CFBRCH={u54}.ST_CENTRO_COSTO_APERTURA " +
                "FETCH FIRST 1 ROWS ONLY)";

            // -- Construcción del SELECT principal con QueryBuilder:
            //    FROM UNI54L07, filtros por BIN y agencias, "no impresa" y fecha de corte.
            //    Se proyectan campos directos y subconsultas como columnas.
            var query = QueryBuilder
                .From("UNI54L07", "S38FILEBA").As(u54)

                // -- Proyección: columnas principales necesarias para armar la respuesta.
                .Select(($"{u54}.ST_CODIGO_TARJETA", "Numero"))
                .Select(($"{u54}.ST_CENTRO_COSTO_IMPR_TARJETA", agImpCod))
                .Select(($"{u54}.ST_CENTRO_COSTO_APERTURA", agApeCod))

                // -- Proyección: subconsultas correlacionadas (traen “Nombre en tarjeta”, “Cuenta” y “Nombre de Estatus”).
                .Select(($"{subSelNombre}", subNombre))
                .Select(($"{subSelCuenta}", subCuenta))
                .Select(($"{subSelEstatus}", subEstatus))

                // -- Proyección: nombres de agencias usando subconsulta por centro de costo (imprime/apertura).
                .Select(($"{subSelAgImpNom}", agImpNom))
                .Select(($"{subSelAgApeNom}", agApeNom))

                // -- Filtros base: BIN, agencias, estado "no impresa" y antigüedad.
                .WhereRaw($"{u54}.ST_BIN_TARJETA = '{bin.Trim()}'")
                .WhereRaw($"{u54}.ST_CENTRO_COSTO_IMPR_TARJETA = {agenciaImprime}")
                .WhereRaw($"{u54}.ST_CENTRO_COSTO_APERTURA = {agenciaApertura}")
                .WhereRaw($"{u54}.ST_FECHA_IMPRESION = 0")
                .WhereRaw($"{u54}.ST_HORA_IMPRESION = 0")
                .WhereRaw($"{u54}.ST_USUARIO_IMPRESION = ''")
                .WhereRaw($"{u54}.ST_FECHA_APERTURA >= {numericDate}")

                // -- Orden: se replica el ordenamiento que ya utilizabas.
                .OrderBy($"{u54}.ST_CODIGO_TARJETA")
                .OrderBy($"{u54}.ST_CENTRO_COSTO_IMPR_TARJETA")
                .OrderBy($"{u54}.ST_CENTRO_COSTO_APERTURA")
                .OrderBy($"{u54}.ST_FECHA_IMPRESION")
                .OrderBy($"{u54}.ST_HORA_IMPRESION")
                .OrderBy($"{u54}.ST_USUARIO_IMPRESION")

                // -- Nota: Si esperas volúmenes enormes, puedes encadenar .FetchFirst(N) y/o .Offset(M) (AS400-compatible).
                .Build();

            using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            command.CommandText = query.Sql;                 // El builder genera el SQL final ya listo para ejecutar.
            command.CommandType = System.Data.CommandType.Text;
            command.CommandTimeout = 0;

            // -- Ejecución en un único viaje a BD.
            using DbDataReader reader = await command.ExecuteReaderAsync();

            // -- Preparación del contenedor de respuesta (reseteo por seguridad).
            _getDetalleTarjetasImprimirResponseDto = new();

            // -- Variables para setear datos de agencias una única vez (como en tu lógica original).
            string? agenciaImpCodVal = null;
            string? agenciaImpNomVal = null;
            string? agenciaApeCodVal = null;
            string? agenciaApeNomVal = null;

            if (reader.HasRows)
            {
                while (await reader.ReadAsync())
                {
                    // -- Mapeo directo desde columnas proyectadas.
                    var numeroTarjeta = reader["Numero"]?.ToString() ?? "";
                    var nombreTarjeta = reader[subNombre]?.ToString() ?? "";
                    var numeroCuenta = reader[subCuenta]?.ToString() ?? "";
                    var motivo = reader[subEstatus]?.ToString() ?? "";

                    var agImpCodRow = reader[agImpCod]?.ToString() ?? "";
                    var agImpNomRow = reader[agImpNom]?.ToString() ?? "";
                    var agApeCodRow = reader[agApeCod]?.ToString() ?? "";
                    var agApeNomRow = reader[agApeNom]?.ToString() ?? "";

                    // -- Primera fila fija los datos de agencia a nivel de cabecera (como hacías con bandera1).
                    if (agenciaImpCodVal is null)
                    {
                        agenciaImpCodVal = agImpCodRow;
                        agenciaImpNomVal = agImpNomRow;
                        agenciaApeCodVal = agApeCodRow;
                        agenciaApeNomVal = agApeNomRow;

                        _getDetalleTarjetasImprimirResponseDto.Agencia.AgenciaImprimeCodigo = agenciaImpCodVal;
                        _getDetalleTarjetasImprimirResponseDto.Agencia.AgenciaImprimeNombre = agenciaImpNomVal;
                        _getDetalleTarjetasImprimirResponseDto.Agencia.AgenciaAperturaCodigo = agenciaApeCodVal;
                        _getDetalleTarjetasImprimirResponseDto.Agencia.AgenciaAperturaNombre = agenciaApeNomVal;
                    }

                    // -- Se agrega la tarjeta a la lista final (una sola pasada; sin más consultas).
                    _getDetalleTarjetasImprimirResponseDto.Tarjetas.Add(new()
                    {
                        Nombre = nombreTarjeta,
                        Numero = numeroTarjeta,
                        FechaEmision = "",     // Mantengo vacíos si no están en UNI54L07; si existen, puedes agregarlos en el SELECT.
                        FechaVencimiento = "",
                        Motivo = motivo,
                        NumeroCuenta = numeroCuenta
                    });
                }
            }

            // -- Bloque de código de estatus final homogéneo.
            _getDetalleTarjetasImprimirResponseDto.Codigo.Message = "Exitoso";
            _getDetalleTarjetasImprimirResponseDto.Codigo.Status = "success";
            _getDetalleTarjetasImprimirResponseDto.Codigo.Error = "200";
            _getDetalleTarjetasImprimirResponseDto.Codigo.TimeStamp = $"{DateTime.Now:HH:mm:ss tt}";

            return _getDetalleTarjetasImprimirResponseDto;
        }
        catch (Exception ex)
        {
            // -- Estandarización de errores en respuesta; mensajes seguros para el cliente.
            var resp = new GetDetallesTarjetasImprimirResponseDto();
            resp.Codigo.Message = ex.Message;
            resp.Codigo.Status = "BadRequest";
            resp.Codigo.Error = "400";
            resp.Codigo.TimeStamp = $"{DateTime.Now:HH:mm:ss tt}";
            return resp;
        }
    }
}
