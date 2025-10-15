using Connections.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.PaymentsDtos;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.IServiceReference;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;
using Newtonsoft.Json;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.REST_UTH.Payments.Payments_Services;

/// <summary>
/// Servicio Payments que consume endpoints de GINIH y persiste respuestas
/// en AS/400 (DB2 for i) usando <c>RestUtilities.QueryBuilder</c>.
/// </summary>
/// <param name="_httpClientFactory">Factory de HttpClient configurado (perfil "GINIH").</param>
/// <param name="_connection">Conexión a base de datos (AS/400).</param>
/// <param name="_contextAccessor">Accessor de HttpContext para trazabilidad/log en la conexión.</param>
public class PaymentsServices(
    IHttpClientFactory _httpClientFactory,
    IDatabaseConnection _connection,
    IHttpContextAccessor _contextAccessor
) : IPaymentsServices
{
    // ============================================================
    // GET /payments?referenceId=...
    // ============================================================

    /// <summary>
    /// Obtiene pagos por referencia y persiste la respuesta en DB.
    /// </summary>
    public async Task<GetPaymentsResponseDto> GetPaymentAsync(GetPaymentsDto getPaymentsDto)
    {
        var response = await ConsumoWebServiceConsultaPagos(getPaymentsDto);
        return MapResponse(getPaymentsDto, response);
    }

    [HttpGet]
    private async Task<GetPaymentsResponseDto> ConsumoWebServiceConsultaPagos(GetPaymentsDto getPaymentsDto)
    {
        GetPaymentsResponseDto result = new();
        RefreshToken refreshSvc = new(_connection, _contextAccessor);

        // Host base del servicio externo
        string baseUrl = GlobalConnection.Current.Host ?? string.Empty;

        var refresh = await refreshSvc.DoRefreshToken();
        string jwt = refresh.Data.JWT;
        string reference = getPaymentsDto.Reference ?? string.Empty;

        if (!refresh.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            result.Status = HttpStatusCode.BadRequest.ToString();
            result.Message = "¡¡El JWT no se validó correctamente!!";
            return result;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient("GINIH");
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", jwt);

            using var httpResponse = await client.GetAsync($"/payments?referenceId={reference}");
            var json = await httpResponse.Content.ReadAsStringAsync();
            var deserialized = JsonConvert.DeserializeObject<GetPaymentsResponseDto>(json);

            if (deserialized is not null)
            {
                deserialized.Status = httpResponse.StatusCode.ToString();
                return deserialized;
            }

            result.Status = httpResponse.StatusCode.ToString();
            result.Message = "La consulta no devolvió nada";
            return result;
        }
        catch (Exception ex)
        {
            result.Status = HttpStatusCode.NotFound.ToString();
            result.Message = ex.Message;
            return result;
        }
    }

    // ============================================================
    // GET /payments/{id}
    // ============================================================

    /// <summary>
    /// Obtiene el detalle de pago por ID y (opcionalmente) persiste/usa la respuesta.
    /// </summary>
    public async Task<GetPaymentsIDResponseDto> GetPaymentsID(GetPaymentsIDDto getPaymentsIDDto)
    {
        var response = await ConsumoWebServiceConsultaPagosPorID(getPaymentsIDDto);
        return MapResponseID(getPaymentsIDDto, response);
    }

    [HttpGet]
    private async Task<GetPaymentsIDResponseDto> ConsumoWebServiceConsultaPagosPorID(GetPaymentsIDDto getPaymentsIDDto)
    {
        GetPaymentsIDResponseDto result = new();
        RefreshToken refreshSvc = new(_connection, _contextAccessor);

        string baseUrl = GlobalConnection.Current.Host ?? string.Empty;

        var refresh = await refreshSvc.DoRefreshToken();
        string jwt = refresh.Data.JWT;
        string id = getPaymentsIDDto.Id ?? string.Empty;

        if (!refresh.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            result.Status = HttpStatusCode.BadRequest.ToString();
            result.Message = "¡¡El JWT no se validó correctamente!!";
            return result;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient("GINIH");
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", jwt);

            // OJO: ruta correcta → /payments/{id}
            using var httpResponse = await client.GetAsync($"/payments/{id}");
            var json = await httpResponse.Content.ReadAsStringAsync();
            var deserialized = JsonConvert.DeserializeObject<GetPaymentsIDResponseDto>(json);

            if (deserialized is not null)
            {
                deserialized.Status = httpResponse.StatusCode.ToString();
                return deserialized;
            }

            result.Status = httpResponse.StatusCode.ToString();
            result.Message = "La consulta no devolvió nada";
            return result;
        }
        catch (Exception ex)
        {
            result.Status = HttpStatusCode.NotFound.ToString();
            result.Message = ex.Message;
            return result;
        }
    }

    // ============================================================
    // POST /payments
    // ============================================================

    /// <summary>
    /// Publica un pago y persiste respuesta en DB.
    /// </summary>
    public async Task<PostPaymentResponseDto> PostPayments(PostPaymentDto postPaymentDto)
    {
        var response = await ConsumoWebServicePosteaPagos(postPaymentDto);
        return MapPostResponse(postPaymentDto, response);
    }

    [HttpPost]
    private async Task<PostPaymentResponseDto> ConsumoWebServicePosteaPagos(PostPaymentDto postPaymentDto)
    {
        PostPaymentResponseDto result = new();
        RefreshToken refreshSvc = new(_connection, _contextAccessor);
        URLsExt urlHelper = new();

        string baseUrl = GlobalConnection.Current.Host ?? string.Empty;
        var refresh = await refreshSvc.DoRefreshToken();
        string jwt = refresh.Data.JWT;

        // Este método externo crea el objeto final a enviar (tu lógica existente).
        // Asumo que lo tienes implementado en tu proyecto.
        PostPaymentDtoFinal payload = GenerarPostPayment(postPaymentDto.CamposObligatoriosModel.Guid, out bool ok);

        if (!refresh.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            result.Status = HttpStatusCode.BadRequest.ToString();
            result.Error = "1";
            result.Message = "¡¡El JWT no se validó correctamente!!";
            return result;
        }

        if (!ok)
        {
            result.Status = HttpStatusCode.BadRequest.ToString();
            result.Error = "1";
            result.Message = "No se pudo generar el objeto a enviar (lectura de tabla falló).";
            return result;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient("GINIH");
            client.BaseAddress = new Uri(urlHelper.QuerySchemeEmptyFilter(baseUrl));
            client.DefaultRequestHeaders.Remove("Authorization");
            client.DefaultRequestHeaders.Add("Authorization", jwt);
            client.DefaultRequestHeaders.Remove("idempotencyKey");
            client.DefaultRequestHeaders.Add("idempotencyKey", Guid.NewGuid().ToString());

            var content = System.Text.Json.JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null, // respeta nombres exactos
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

            var httpContent = new StringContent(content, Encoding.UTF8, "application/json");

            using var httpResponse = await client.PostAsync("/payments", httpContent);
            var body = await httpResponse.Content.ReadAsStringAsync();
            var deserialized = JsonConvert.DeserializeObject<PostPaymentResponseDto>(body) ?? new();

            deserialized.Status = httpResponse.StatusCode.ToString();
            if (httpResponse.IsSuccessStatusCode)
            {
                deserialized.Error = "0";
                deserialized.Mensaje = "PROCESADO EXITOSAMENTE";
            }
            else
            {
                deserialized.Error = "1";
                deserialized.Mensaje = "PROCESO NO DEVOLVIÓ VALORES";
                deserialized.Message ??= "La consulta no devolvió valores";
            }

            return deserialized;
        }
        catch (Exception ex)
        {
            result.Status = HttpStatusCode.NotFound.ToString();
            result.Error = "1";
            result.Mensaje = "ERROR AL EJECUTAR PETICIÓN A SERVICIO EXTERNO.";
            result.Message = ex.Message;
            return result;
        }
    }

    // ============================================================
    // Persistencia con QueryBuilder
    // ============================================================

    /// <summary>
    /// Inserta la respuesta de GET /payments en <c>BCAH96DTA.UTH04APU</c>.
    /// </summary>
    private GetPaymentsResponseDto MapResponse(GetPaymentsDto dto, GetPaymentsResponseDto resp)
    {
        // Persistimos solo si la llamada fue OK
        if (!string.Equals(resp.Status, "success", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(resp.Status, "OK", StringComparison.OrdinalIgnoreCase))
            return resp;

        _connection.Open();
        try
        {
            if (!_connection.IsConnected || resp.Data is null)
                return resp;

            // Helper local para string seguro
            static string S(object? v, string def = "") => v?.ToString() ?? def;

            int correlativo = 0;

            // Build INSERT parametrizado (DB2 i: placeholders "?" + Parameters)
            var ins = new InsertQueryBuilder("UTH04APU", "BCAH96DTA", SqlDialect.Db2i)
                .WithComment("Insert snapshot from GET /payments")
                .IntoColumns(
                    "APU00GUID", "APU01CORR", "APU02FECH", "APU03HORA", "APU04CAJE", "APU05BANC", "APU06SUCU", "APU07TERM",
                    "APU08STAT", "APU09MSSG", "APU10DTID", "APU11DTNA", "APU12CUID", "APU13CUNA", "APU14COID", "APU15CONA",
                    "APU16DREF", "APU17REFE", "APU18AMVA", "APU19AMCU", "APU20SUTO", "APU19PFEE", "APU20SCHA", "APU21DICO",
                    "APU22BTAX", "APU23TOTA", "APU24CRAT", "APU25TIST", "APU26COVA", "APU27CONA", "APU28ERRO", "APU29MENS"
                )
                .Row(
                    S(dto.CamposObligatoriosModel.Guid),
                    correlativo, // numérico
                    S(dto.CamposObligatoriosModel.Fecha),
                    S(dto.CamposObligatoriosModel.Hora),
                    S(dto.CamposObligatoriosModel.Cajero),
                    S(dto.CamposObligatoriosModel.Banco),
                    S(dto.CamposObligatoriosModel.Sucursal),
                    S(dto.CamposObligatoriosModel.Terminal),

                    S(resp.Status),
                    S(resp.Message),

                    S(resp.Data.Id),
                    S(resp.Data.Name),
                    S(resp.Data.Customer?.Id),
                    S(resp.Data.Customer?.Name),
                    S(resp.Data.Company?.Id),
                    S(resp.Data.Company?.Name),

                    S(resp.Data.DocumentReference),
                    S(resp.Data.ReferenceId),

                    S(resp.Data.Amount?.Value),
                    S(resp.Data.Amount?.Currency),
                    S(resp.Data.Amount?.Breakdown?.Subtotal),
                    S(resp.Data.Amount?.Breakdown?.ProcessingFee),
                    S(resp.Data.Amount?.Breakdown?.Surcharge),
                    S(resp.Data.Amount?.Breakdown?.Discount),
                    S(resp.Data.Amount?.Breakdown?.Tax),
                    S(resp.Data.Amount?.Breakdown?.Total),

                    S(resp.Data.CreatedAt),
                    S(resp.TimeStamp),
                    S(resp.Code?.Value),
                    S(resp.Code?.Name),
                    S(resp.Error),
                    S(resp.Mensaje)
                )
                .Build();

            using var cmd = _connection.GetDbCommand(ins, _contextAccessor.HttpContext!);
            _ = cmd.ExecuteNonQuery();

            return resp;
        }
        catch (Exception ex)
        {
            resp.Mensaje = ex.Message;
            resp.Error = "106";
            resp.Status = "InternalServerError";
            resp.Code.Value = ((int)HttpStatusCode.InternalServerError).ToString();
            resp.Code.Name = HttpStatusCode.InternalServerError.ToString();
            return resp;
        }
        finally
        {
            _connection.Close();
        }
    }

    /// <summary>
    /// (Placeholder) Si necesitas persistir el GET por ID, puedes hacerlo aquí.
    /// Actualmente solo devuelve la respuesta.
    /// </summary>
    private GetPaymentsIDResponseDto MapResponseID(GetPaymentsIDDto dto, GetPaymentsIDResponseDto resp)
    {
        // Si más adelante deseas insertar a tabla, usa InsertQueryBuilder igual que arriba.
        return resp;
    }

    /// <summary>
    /// Inserta la respuesta de POST /payments en <c>BCAH96DTA.UTH04APU</c> (mismo layout),
    /// si el servicio respondió exitosamente.
    /// </summary>
    private PostPaymentResponseDto MapPostResponse(PostPaymentDto dto, PostPaymentResponseDto resp)
    {
        if (!string.Equals(resp.Status, "success", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(resp.Status, "OK", StringComparison.OrdinalIgnoreCase))
            return resp;

        _connection.Open();
        try
        {
            if (!_connection.IsConnected || resp.Data is null)
                return resp;

            static string S(object? v, string def = "") => v?.ToString() ?? def;

            int correlativo = 0;

            var ins = new InsertQueryBuilder("UTH04APU", "BCAH96DTA", SqlDialect.Db2i)
                .WithComment("Insert snapshot from POST /payments")
                .IntoColumns(
                    "APU00GUID", "APU01CORR", "APU02FECH", "APU03HORA", "APU04CAJE", "APU05BANC", "APU06SUCU", "APU07TERM",
                    "APU08STAT", "APU09MSSG", "APU10DTID", "APU11DTNA", "APU12CUID", "APU13CUNA", "APU14COID", "APU15CONA",
                    "APU16DREF", "APU17REFE", "APU18AMVA", "APU19AMCU", "APU20SUTO", "APU19PFEE", "APU20SCHA", "APU21DICO",
                    "APU22BTAX", "APU23TOTA", "APU24CRAT", "APU25TIST", "APU26COVA", "APU27CONA", "APU28ERRO", "APU29MENS"
                )
                .Row(
                    S(dto.CamposObligatoriosModel.Guid),
                    correlativo,
                    S(dto.CamposObligatoriosModel.Fecha),
                    S(dto.CamposObligatoriosModel.Hora),
                    S(dto.CamposObligatoriosModel.Cajero),
                    S(dto.CamposObligatoriosModel.Banco),
                    S(dto.CamposObligatoriosModel.Sucursal),
                    S(dto.CamposObligatoriosModel.Terminal),

                    S(resp.Status),
                    S(resp.Message),

                    S(resp.Data.Id),
                    S(resp.Data.Name),
                    S(resp.Data.Customer?.Id),
                    S(resp.Data.Customer?.Name),
                    S(resp.Data.Company?.Id),
                    S(resp.Data.Company?.Name),

                    S(resp.Data.DocumentReference),
                    S(resp.Data.ReferenceId),

                    S(resp.Data.Amount?.Value),
                    S(resp.Data.Amount?.Currency),
                    S(resp.Data.Amount?.Breakdown?.Subtotal),
                    S(resp.Data.Amount?.Breakdown?.ProcessingFee),
                    S(resp.Data.Amount?.Breakdown?.Surcharge),
                    S(resp.Data.Amount?.Breakdown?.Discount),
                    S(resp.Data.Amount?.Breakdown?.Tax),
                    S(resp.Data.Amount?.Breakdown?.Total),

                    S(resp.Data.CreatedAt),
                    S(resp.TimeStamp),
                    S(resp.Code?.Value),
                    S(resp.Code?.Name),
                    S(resp.Error),
                    S(resp.Mensaje)
                )
                .Build();

            using var cmd = _connection.GetDbCommand(ins, _contextAccessor.HttpContext!);
            _ = cmd.ExecuteNonQuery();

            return resp;
        }
        catch (Exception ex)
        {
            resp.Mensaje = ex.Message;
            resp.Error = "106";
            resp.Status = "InternalServerError";
            resp.Code.Value = ((int)HttpStatusCode.InternalServerError).ToString();
            resp.Code.Name = HttpStatusCode.InternalServerError.ToString();
            return resp;
        }
        finally
        {
            _connection.Close();
        }
    }

    // ============================================================
    // Nota:
    // - Se asume que tienes implementado GenerarPostPayment(...) que arma el payload.
    // - La clase RefreshToken usada aquí ya la migraste a QueryBuilder en tu base.
    // - El AS400ConnectionProvider expone GetDbCommand(QueryResult, HttpContext) que enlaza parámetros.
    // ============================================================

    // Firma existente en tu solución (no implementada aquí).
    private static PostPaymentDtoFinal GenerarPostPayment(string guid, out bool exitoso)
    {
        exitoso = false;
        return new PostPaymentDtoFinal();
    }
}
