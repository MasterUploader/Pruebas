using Connections.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.PaymentsDtos;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.IServiceReference;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;
using Newtonsoft.Json;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using System.Data.Common;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.REST_UTH.Payments.Payments_Services;

/// <summary>
/// Servicio para consultar y registrar pagos de la API UTH usando RestUtilities.QueryBuilder para persistencia en AS/400 (DB2 i).
/// </summary>
public class PaymentsServices(
    IHttpClientFactory _httpClientFactory,
    IDatabaseConnection _connection,
    IHttpContextAccessor _contextAccessor) : IPaymentsServices
{
    /// <summary>
    /// Trae pagos por referencia (GET /payments?referenceId=...).
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
        RefreshToken refresh = new(_connection, _contextAccessor);

        var baseUrl = GlobalConnection.Current.Host;
        var refreshResponse = await refresh.DoRefreshToken();
        var jwt = refreshResponse.Data.JWT;
        var reference = getPaymentsDto.Reference;

        if (!refreshResponse.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            result.Status = HttpStatusCode.BadRequest.ToString();
            result.Message = "¡¡El JWT no se validó correctamente!!";
            return result;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient("GINIH");
            if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.IsWellFormedUriString(baseUrl, UriKind.RelativeOrAbsolute))
                client.BaseAddress = new Uri(baseUrl);

            client.DefaultRequestHeaders.Add("Authorization", jwt);

            using var response = await client.GetAsync($"{client.BaseAddress}/payments?referenceId={reference}");
            var json = await response.Content.ReadAsStringAsync();
            var deserialized = JsonConvert.DeserializeObject<GetPaymentsResponseDto>(json);

            if (deserialized is not null)
            {
                deserialized.Status = response.StatusCode.ToString();
                return deserialized;
            }

            result.Status = response.StatusCode.ToString();
            result.Message = "La consulta no devolvió datos";
            return result;
        }
        catch (Exception ex)
        {
            result.Status = HttpStatusCode.NotFound.ToString();
            result.Message = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Trae pago por Id (GET /payments/{id}).
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
        RefreshToken refresh = new(_connection, _contextAccessor);

        var baseUrl = GlobalConnection.Current.Host;
        var refreshResponse = await refresh.DoRefreshToken();
        var jwt = refreshResponse.Data.JWT;
        var id = getPaymentsIDDto.Id;

        if (!refreshResponse.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            result.Status = HttpStatusCode.BadRequest.ToString();
            result.Message = "¡¡El JWT no se validó correctamente!!";
            return result;
        }

        try
        {
            using var client = _httpClientFactory.CreateClient("GINIH");
            if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.IsWellFormedUriString(baseUrl, UriKind.RelativeOrAbsolute))
                client.BaseAddress = new Uri(baseUrl);

            client.DefaultRequestHeaders.Add("Authorization", jwt);

            using var response = await client.GetAsync($"{client.BaseAddress}/payments/{id}");
            var json = await response.Content.ReadAsStringAsync();
            var deserialized = JsonConvert.DeserializeObject<GetPaymentsIDResponseDto>(json);

            if (deserialized is not null)
            {
                deserialized.Status = response.StatusCode.ToString();
                return deserialized;
            }

            result.Status = response.StatusCode.ToString();
            result.Message = "La consulta no devolvió datos";
            return result;
        }
        catch (Exception ex)
        {
            result.Status = HttpStatusCode.NotFound.ToString();
            result.Message = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Publica un pago (POST /payments) y persiste el resultado.
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
        RefreshToken refresh = new(_connection, _contextAccessor);

        var baseUrl = GlobalConnection.Current.Host;
        var refreshResponse = await refresh.DoRefreshToken();
        var jwt = refreshResponse.Data.JWT;

        if (!refreshResponse.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            result.Status = HttpStatusCode.BadRequest.ToString();
            result.Error = "1";
            result.Mensaje = "¡¡El JWT no se validó correctamente!!";
            result.Message = "¡¡El JWT no se validó correctamente!!";
            return result;
        }

        // Si usas un generador previo del objeto:
        // var postPaymentDtoFinal = GenerarPostPayment(postPaymentDto.CamposObligatoriosModel.Guid, out bool ok);

        try
        {
            using var client = _httpClientFactory.CreateClient("GINIH");
            if (!string.IsNullOrWhiteSpace(baseUrl) && Uri.IsWellFormedUriString(baseUrl, UriKind.RelativeOrAbsolute))
                client.BaseAddress = new Uri(baseUrl);

            // Envío tal cual el objeto recibido (ajusta si usas un DTO final)
            var content = System.Text.Json.JsonSerializer.Serialize(
                postPaymentDto,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = null,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

            var data = new StringContent(content, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Add("Authorization", jwt);
            client.DefaultRequestHeaders.Add("idempotencyKey", Guid.NewGuid().ToString());

            using var response = await client.PostAsync($"{client.BaseAddress}/payments", data);
            var responseContent = await response.Content.ReadAsStringAsync();
            var deserialized = JsonConvert.DeserializeObject<PostPaymentResponseDto>(responseContent) ?? new PostPaymentResponseDto();

            deserialized.Status = response.StatusCode.ToString();
            if (response.IsSuccessStatusCode && deserialized.Data is not null)
            {
                deserialized.Error = "0";
                deserialized.Mensaje = "PROCESADO EXITOSAMENTE";
                return deserialized;
            }

            deserialized.Error = "1";
            deserialized.Mensaje ??= "PROCESO NO DEVOLVIÓ VALORES";
            deserialized.Message ??= "La consulta no devolvió valores";
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

    /// <summary>
    /// Persiste la respuesta de GetPaymentAsync en BCAH96DTA.UTH04APU usando INSERT parametrizado.
    /// </summary>
    private GetPaymentsResponseDto MapResponse(GetPaymentsDto dto, GetPaymentsResponseDto resp)
    {
        _connection.Open();
        try
        {
            if ((_connection.IsConnected) &&
                (resp.Status.Equals("success", StringComparison.OrdinalIgnoreCase) ||
                 resp.Status.Equals("OK", StringComparison.OrdinalIgnoreCase)) &&
                resp.Data is not null)
            {
                int correlativo = 0;

                var insert = new InsertQueryBuilder("UTH04APU", "BCAH96DTA", SqlDialect.Db2i)
                    .IntoColumns(
                        "APU00GUID", "APU01CORR", "APU02FECH", "APU03HORA", "APU04CAJE", "APU05BANC", "APU06SUCU", "APU07TERM",
                        "APU08STAT", "APU09MSSG", "APU10DTID", "APU11DTNA", "APU12CUID", "APU13CUNA", "APU14COID", "APU15CONA",
                        "APU16DREF", "APU17REFE", "APU18AMVA", "APU19AMCU", "APU20SUTO", "APU19PFEE", "APU20SCHA", "APU21DICO",
                        "APU22BTAX", "APU23TOTA", "APU24CRAT", "APU25TIST", "APU26COVA", "APU27CONA", "APU28ERRO", "APU29MENS"
                    )
                    .Row(
                        dto.CamposObligatoriosModel.Guid,                         // APU00GUID
                        correlativo,                                              // APU01CORR
                        dto.CamposObligatoriosModel.Fecha ?? string.Empty,       // APU02FECH
                        dto.CamposObligatoriosModel.Hora ?? string.Empty,        // APU03HORA
                        dto.CamposObligatoriosModel.Cajero ?? string.Empty,      // APU04CAJE
                        dto.CamposObligatoriosModel.Banco ?? string.Empty,       // APU05BANC
                        dto.CamposObligatoriosModel.Sucursal ?? string.Empty,    // APU06SUCU
                        dto.CamposObligatoriosModel.Terminal ?? string.Empty,    // APU07TERM
                        resp.Status ?? string.Empty,                              // APU08STAT
                        resp.Message ?? string.Empty,                             // APU09MSSG
                        resp.Data.Id ?? string.Empty,                             // APU10DTID
                        resp.Data.Name ?? string.Empty,                           // APU11DTNA
                        resp.Data.Customer?.Id ?? string.Empty,                   // APU12CUID
                        resp.Data.Customer?.Name ?? string.Empty,                 // APU13CUNA
                        resp.Data.Company?.Id ?? string.Empty,                    // APU14COID
                        resp.Data.Company?.Name ?? string.Empty,                  // APU15CONA
                        resp.Data.DocumentReference ?? string.Empty,              // APU16DREF
                        resp.Data.ReferenceId ?? string.Empty,                    // APU17REFE
                        ConvertirAEnteroGinih(resp.Data.Amount?.Value, 0),        // APU18AMVA (num)
                        resp.Data.Amount?.Currency ?? string.Empty,               // APU19AMCU
                        ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.Subtotal, 0),      // APU20SUTO
                        ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.ProcessingFee, 0), // APU19PFEE
                        ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.Surcharge, 0),     // APU20SCHA
                        ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.Discount, 0),      // APU21DICO
                        ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.Tax, 0),           // APU22BTAX
                        ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.Total, 0),         // APU23TOTA
                        resp.Data.CreatedAt ?? string.Empty,                       // APU24CRAT
                        resp.TimeStamp ?? string.Empty,                            // APU25TIST
                        resp.Code?.Value ?? string.Empty,                          // APU26COVA
                        resp.Code?.Name ?? string.Empty,                           // APU27CONA
                        resp.Error ?? string.Empty,                                // APU28ERRO
                        resp.Mensaje ?? "Proceso ejecutado satisfactoriamente"    // APU29MENS
                    )
                    .Build();

                using var cmd = _connection.GetDbCommand(insert, _contextAccessor.HttpContext!);
                _ = cmd.ExecuteNonQuery();
            }

            return resp;
        }
        catch (Exception ex)
        {
            var err = new GetPaymentsResponseDto
            {
                Mensaje = ex.Message,
                Error = "106",
                Status = "InternalServerError"
            };
            err.Code.Value = ((int)HttpStatusCode.InternalServerError).ToString();
            err.Code.Name = HttpStatusCode.InternalServerError.ToString();
            return err;
        }
        finally
        {
            _connection.Close();
        }
    }

    /// <summary>
    /// (Placeholder) Si deseas persistir algo del GET /payments/{id}, implementa aquí.
    /// </summary>
    private GetPaymentsIDResponseDto MapResponseID(GetPaymentsIDDto _dto, GetPaymentsIDResponseDto response)
    {
        // // === EJEMPLO COMENTADO (QueryBuilder): Guardar respuesta por ID ===
        // _connection.Open();
        // try
        // {
        //     if (_connection.IsConnected && response?.Data is not null)
        //     {
        //         var insert = new InsertQueryBuilder("UTH05PID", "BCAH96DTA", SqlDialect.Db2i)
        //             .IntoColumns("PID00ID", "PID01NAME", "PID02STATUS", "PID03MSG", "PID04TS")
        //             .Row(
        //                 response.Data.Id ?? string.Empty,
        //                 response.Data.Name ?? string.Empty,
        //                 response.Status ?? string.Empty,
        //                 response.Message ?? string.Empty,
        //                 response.TimeStamp ?? string.Empty
        //             )
        //             .Build();
        //
        //         using var cmd = _connection.GetDbCommand(insert, _contextAccessor.HttpContext!);
        //         _ = cmd.ExecuteNonQuery();
        //     }
        // }
        // finally { _connection.Close(); }

        return response;
    }

    /// <summary>
    /// (Opcional) Persistencia del resultado del POST /payments.
    /// Mantengo el bloque comentado, pero actualizado a RestUtilities.QueryBuilder.
    /// </summary>
    private PostPaymentResponseDto MapPostResponse(PostPaymentDto dto, PostPaymentResponseDto resp)
    {
        // // === PERSISTENCIA (COMENTADA) DEL RESULTADO DEL POST /payments ===
        // _connection.Open();
        // try
        // {
        //     if (_connection.IsConnected && resp?.Data is not null)
        //     {
        //         int correlativo = 0;
        //
        //         var insert = new InsertQueryBuilder("UTH04APU", "BCAH96DTA", SqlDialect.Db2i)
        //             .IntoColumns(
        //                 "APU00GUID", "APU01CORR", "APU02FECH", "APU03HORA", "APU04CAJE", "APU05BANC", "APU06SUCU", "APU07TERM",
        //                 "APU08STAT", "APU09MSSG", "APU10DTID", "APU11DTNA", "APU12CUID", "APU13CUNA", "APU14COID", "APU15CONA",
        //                 "APU16DREF", "APU17REFE", "APU18AMVA", "APU19AMCU", "APU20SUTO", "APU19PFEE", "APU20SCHA", "APU21DICO",
        //                 "APU22BTAX", "APU23TOTA", "APU24CRAT", "APU25TIST", "APU26COVA", "APU27CONA", "APU28ERRO", "APU29MENS"
        //             )
        //             .Row(
        //                 dto.CamposObligatoriosModel.Guid,                          // APU00GUID
        //                 correlativo,                                               // APU01CORR
        //                 dto.CamposObligatoriosModel.Fecha ?? string.Empty,        // APU02FECH
        //                 dto.CamposObligatoriosModel.Hora ?? string.Empty,         // APU03HORA
        //                 dto.CamposObligatoriosModel.Cajero ?? string.Empty,       // APU04CAJE
        //                 dto.CamposObligatoriosModel.Banco ?? string.Empty,        // APU05BANC
        //                 dto.CamposObligatoriosModel.Sucursal ?? string.Empty,     // APU06SUCU
        //                 dto.CamposObligatoriosModel.Terminal ?? string.Empty,     // APU07TERM
        //                 resp.Status ?? string.Empty,                               // APU08STAT
        //                 resp.Message ?? string.Empty,                              // APU09MSSG
        //                 resp.Data.Id ?? string.Empty,                              // APU10DTID
        //                 resp.Data.Name ?? string.Empty,                            // APU11DTNA
        //                 resp.Data.Customer?.Id ?? string.Empty,                    // APU12CUID
        //                 resp.Data.Customer?.Name ?? string.Empty,                  // APU13CUNA
        //                 resp.Data.Company?.Id ?? string.Empty,                     // APU14COID
        //                 resp.Data.Company?.Name ?? string.Empty,                   // APU15CONA
        //                 resp.Data.DocumentReference ?? string.Empty,               // APU16DREF
        //                 resp.Data.ReferenceId ?? string.Empty,                     // APU17REFE
        //                 ConvertirAEnteroGinih(resp.Data.Amount?.Value, 0),         // APU18AMVA
        //                 resp.Data.Amount?.Currency ?? string.Empty,                // APU19AMCU
        //                 ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.Subtotal, 0),      // APU20SUTO
        //                 ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.ProcessingFee, 0), // APU19PFEE
        //                 ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.Surcharge, 0),     // APU20SCHA
        //                 ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.Discount, 0),      // APU21DICO
        //                 ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.Tax, 0),           // APU22BTAX
        //                 ConvertirAEnteroGinih(resp.Data.Amount?.Breakdown?.Total, 0),         // APU23TOTA
        //                 resp.Data.CreatedAt ?? string.Empty,                        // APU24CRAT
        //                 resp.TimeStamp ?? string.Empty,                             // APU25TIST
        //                 resp.Code?.Value ?? string.Empty,                           // APU26COVA
        //                 resp.Code?.Name ?? string.Empty,                            // APU27CONA
        //                 resp.Error ?? "0",                                          // APU28ERRO
        //                 resp.Mensaje ?? "PROCESADO EXITOSAMENTE"                    // APU29MENS
        //             )
        //             .Build();
        //
        //         using var cmd = _connection.GetDbCommand(insert, _contextAccessor.HttpContext!);
        //         _ = cmd.ExecuteNonQuery();
        //
