using Connections.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.CompaniesDtos;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.IServiceReference;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;
using Newtonsoft.Json;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using System.Net;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.REST_UTH.Companies.Companies_Services;

/// <summary>
/// Servicio de Companies que consume el endpoint externo y persiste
/// el resultado en AS/400 (DB2 for i) usando <c>RestUtilities.QueryBuilder</c>.
/// </summary>
/// <param name="_httpClientFactory">Factory de HttpClient con el perfil "GINIH".</param>
/// <param name="_connection">Conexión a base de datos (AS/400).</param>
/// <param name="_contextAccessor">Accessor del contexto HTTP (para logging/decorators de la conexión).</param>
public class CompaniesServices(
    IHttpClientFactory _httpClientFactory,
    IDatabaseConnection _connection,
    IHttpContextAccessor _contextAccessor
) : ICompaniesServices
{
    /// <summary>
    /// Ejecuta el proceso completo: consume el WS y mapea/persiste la respuesta.
    /// </summary>
    /// <param name="getCompaniesDto">Parámetros de la consulta (limit, nextToken y campos obligatorios).</param>
    /// <returns>Respuesta del servicio externo y estado del proceso.</returns>
    public async Task<GetCompaniesResponseDto> DoProcessAsync(GetCompaniesDto getCompaniesDto)
    {
        var response = await ConsumoWebServiceConsultaCompañiasPorCobrar(getCompaniesDto);
        return MapResponse(getCompaniesDto, response);
    }

    /// <summary>
    /// Llama el endpoint externo <c>/companies</c> usando el JWT renovado
    /// y devuelve el DTO deserializado.
    /// </summary>
    private async Task<GetCompaniesResponseDto> ConsumoWebServiceConsultaCompañiasPorCobrar(GetCompaniesDto getCompaniesDto)
    {
        GetCompaniesResponseDto resp = new();
        RefreshToken refreshTokenSvc = new(_connection, _contextAccessor);
        URLsExt urlHelper = new();

        string baseUrl = GlobalConnection.Current.Host ?? string.Empty;

        var refreshResponse = await refreshTokenSvc.DoRefreshToken();
        string jwt = refreshResponse.Data.JWT;
        string limit = getCompaniesDto.Limit ?? string.Empty;
        string nextToken = getCompaniesDto.NextToken ?? string.Empty;

        var endpoint = $"{baseUrl}/companies?limit={limit}&NextToken={nextToken}";

        if (!refreshResponse.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            resp.Status = HttpStatusCode.BadRequest.ToString();
            resp.Message = "¡¡El JWT no se validó Correctamente!!";
            resp.Error = "1";
            resp.Mensaje = "Proceso ejecutado Insatisfactoriamente";
            return resp;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient("GINIH");

            if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.IsWellFormedUriString(baseUrl, UriKind.RelativeOrAbsolute))
            {
                client.BaseAddress = new Uri(urlHelper.QuerySchemeEmptyFilter(endpoint));
            }

            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", jwt);

            using var httpResponse = await client.GetAsync(client.BaseAddress);
            var payload = await httpResponse.Content.ReadAsStringAsync();
            var deserialized = JsonConvert.DeserializeObject<GetCompaniesResponseDto>(payload);

            // Éxito
            if (deserialized is not null &&
                deserialized.Data is not null &&
                deserialized.Data.Count > 0 &&
                (httpResponse.StatusCode == HttpStatusCode.OK ||
                 string.Equals(httpResponse.StatusCode.ToString(), "success", StringComparison.OrdinalIgnoreCase)))
            {
                deserialized.Status = httpResponse.StatusCode.ToString();
                deserialized.Error = "0";
                deserialized.Mensaje = "Proceso ejecutado Satisfactoriamente";
                return deserialized;
            }

            // Sin datos o status != OK
            resp = deserialized ?? new GetCompaniesResponseDto();
            resp.Status = httpResponse.StatusCode.ToString();
            resp.Message = deserialized?.Message ?? "La consulta no devolvió valores";
            resp.Error = "1";
            resp.Mensaje = "Proceso ejecutado Insatisfactoriamente";
            return resp;
        }
        catch (Exception ex)
        {
            resp.Status = HttpStatusCode.NotFound.ToString();
            resp.Message = ex.Message;
            resp.Error = "1";
            resp.Mensaje = "Proceso ejecutado Insatisfactoriamente";
            return resp;
        }
    }

    /// <summary>
    /// Persiste la respuesta del WS en la tabla <c>BCAH96DTA.UTH01CCC</c> usando
    /// <c>InsertQueryBuilder</c> con VALUES parametrizados (placeholders <c>?</c>).
    /// </summary>
    /// <remarks>
    /// Mantiene la semántica de la versión previa: inserta una fila por cada ítem de <c>Data</c>,
    /// con <c>CCC01CORR</c> incremental desde 0 y el resto de columnas mapeadas desde el DTO.
    /// </remarks>
    private GetCompaniesResponseDto MapResponse(GetCompaniesDto getCompaniesDto, GetCompaniesResponseDto getCompaniesResponseDto)
    {
        // Si la llamada no fue exitosa, no insertamos nada y devolvemos tal cual.
        if (!string.Equals(getCompaniesResponseDto.Status, "success", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(getCompaniesResponseDto.Status, "OK", StringComparison.OrdinalIgnoreCase))
        {
            return getCompaniesResponseDto;
        }

        _connection.Open();
        try
        {
            if (!_connection.IsConnected || getCompaniesResponseDto.Data is null || getCompaniesResponseDto.Data.Count == 0)
                return getCompaniesResponseDto;

            // Para facilitar el mapeo, se “normaliza” a una lista (tu código original serializaba a JSON y re-deserializaba)
            var listEnvelope = new List<GetCompaniesResponseDto> { getCompaniesResponseDto };

            // Builder INSERT parametrizado (DB2 i → placeholders ? + lista QueryResult.Parameters)
            // Columnas en el mismo orden que el INSERT original
            var builder = new InsertQueryBuilder("UTH01CCC", "BCAH96DTA", SqlDialect.Db2i)
                .WithComment("INSERT companies snapshot from GINIH /companies")
                .IntoColumns(
                    "CCC00GUID", // Guid solicitud
                    "CCC01CORR", // correlativo incremental
                    "CCC02FECH", // fecha proceso
                    "CCC03HORA", // hora proceso
                    "CCC04CAJE", // cajero
                    "CCC05BANC", // banco
                    "CCC06SUCU", // sucursal
                    "CCC07TERM", // terminal
                    "CCC08LIMI", // limit
                    "CCC09NTOK", // nextToken
                    "CCC10STAT", // status
                    "CCC11MESS", // message
                    "CCC12DTID", // data.id
                    "CCC12DTNA", // data.name
                    "CCC13DTPO", // data.payableOptions (csv o "Nulo")
                    "CCC13TIST", // timestamp
                    "CCC14MDTI", // metadata.items
                    "CCC15MDHM", // metadata.hasMore
                    "CCC16MDNT", // metadata.nextToken
                    "CCC17COVA", // code.value
                    "CCC18CONA", // code.name
                    "CCC19ERRO", // error (del response)
                    "CCC20MENS"  // mensaje (del response)
                );

            // Utilidad local para evitar nulls
            static string S(object? v, string @default = "") => v?.ToString() ?? @default;

            int correlativo = 0;
            foreach (var envelope in listEnvelope)
            {
                // Campos comunes del “sobre” (cabecera de respuesta)
                var status = S(envelope.Status);
                var message = S(envelope.Message);
                var timestamp = envelope.Timestamp.ToString();
                var mdItems = S(envelope.Metadata?.Items);
                var mdHasMore = S(envelope.Metadata?.HasMore?.ToString() ?? "NO");
                var mdNextToken = S(envelope.Metadata?.NextToken);
                var codeValue = S(envelope.Code?.Value?.ToString() ?? "124");
                var codeName = S(envelope.Code?.Name);
                var error = S(envelope.Error);
                var mensaje = S(envelope.Mensaje, "Proceso ejecutado Satisfactoriamente");

                foreach (var item in envelope.Data)
                {
                    // Campos por ítem
                    var id = S(item.Id);
                    var name = S(item.Name);
                    var payableOptions = item.PayableOptions is null || item.PayableOptions.Count == 0
                        ? "Nulo"
                        : string.Join(",", item.PayableOptions);

                    builder.Row(
                        S(getCompaniesDto.CamposObligatoriosModel.Guid),
                        correlativo.ToString(), // mantenerlo como texto (tal como hacía OleDbType.Char)
                        S(getCompaniesDto.CamposObligatoriosModel.Fecha),
                        S(getCompaniesDto.CamposObligatoriosModel.Hora),
                        S(getCompaniesDto.CamposObligatoriosModel.Cajero),
                        S(getCompaniesDto.CamposObligatoriosModel.Banco),
                        S(getCompaniesDto.CamposObligatoriosModel.Sucursal),
                        S(getCompaniesDto.CamposObligatoriosModel.Terminal),
                        S(getCompaniesDto.Limit),
                        S(getCompaniesDto.NextToken),
                        status,
                        message,
                        id,
                        name,
                        payableOptions,
                        timestamp,
                        mdItems,
                        mdHasMore,
                        mdNextToken,
                        codeValue,
                        codeName,
                        error,
                        mensaje
                    );

                    correlativo++;
                }
            }

            // Construir y ejecutar en un solo INSERT multi-VALUES
            var insert = builder.Build();
            using var cmd = _connection.GetDbCommand(insert, _contextAccessor.HttpContext!);
            _ = cmd.ExecuteNonQuery();

            return getCompaniesResponseDto;
        }
        catch (Exception ex)
        {
            getCompaniesResponseDto.Mensaje = ex.Message;
            getCompaniesResponseDto.Error = "106";
            getCompaniesResponseDto.Status = "InternalServerError";
            return getCompaniesResponseDto;
        }
        finally
        {
            _connection.Close();
        }
    }
}
