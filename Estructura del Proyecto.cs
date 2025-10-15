Convierte esta clase para que use RestUtilities.QueryBuilder:

using Connections.Abstractions;
using Microsoft.AspNetCore.Mvc;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.PaymentsDtos;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.IServiceReference;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;
using Newtonsoft.Json;
using System.Data.Common;
using System.Data.OleDb;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.REST_UTH.Payments.Payments_Services;

/// <summary>
/// Clase Payments Service.
/// </summary>
public class PaymentsServices(IHttpClientFactory _httpClientFactory, IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : IPaymentsServices
{

    /// <summary>
    /// Método GetPaymentsAsync, trae los pagos pendientes.
    /// </summary>
    /// <param name="getPaymentsDto">Objeto DTO.</param>
    /// <returns></returns>
    public async Task<GetPaymentsResponseDto> GetPaymentAsync(GetPaymentsDto getPaymentsDto)
    {
        GetPaymentsResponseDto response = await ConsumoWebServiceConsultaPagos(getPaymentsDto);

        return MapResponse(getPaymentsDto, response);
    }

    [HttpGet]
    private async Task<GetPaymentsResponseDto> ConsumoWebServiceConsultaPagos(GetPaymentsDto getPaymentsDto)
    {
        GetPaymentsResponseDto _getPaymentsResponseDto = new();
        RefreshToken _refreshToken = new(_connection, _contextAccessor);

        //Obtenemos las variables globales
        string _baseUrl = GlobalConnection.Current.Host;

        var refresResponse = await _refreshToken.DoRefreshToken();
        string _JWTToken = refresResponse.Data.JWT;
        var reference = getPaymentsDto.Reference;

        if (refresResponse.Status.Equals("success"))
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("GINIH");
                if (!string.IsNullOrEmpty(_baseUrl) && Uri.IsWellFormedUriString(_baseUrl, UriKind.RelativeOrAbsolute))
                {
                    client.BaseAddress = new Uri(_baseUrl);
                }
                client.DefaultRequestHeaders.Add("Authorization", _JWTToken);

                using HttpResponseMessage response = await client.GetAsync(client.BaseAddress + "/payments?referenceId=" + reference);
                var json_Respuesta = await response.Content.ReadAsStringAsync();
                var deserialized = JsonConvert.DeserializeObject<GetPaymentsResponseDto>(json_Respuesta);

                if (deserialized is not null)
                {
                    _getPaymentsResponseDto = deserialized;
                    _getPaymentsResponseDto.Status = response.StatusCode.ToString();
                    return _getPaymentsResponseDto;
                }
                _getPaymentsResponseDto.Status = response.StatusCode.ToString();
                _getPaymentsResponseDto.Message = "La Consulta no devolvio nada";
                return _getPaymentsResponseDto;
            }
            catch (Exception ex)
            {
                _getPaymentsResponseDto.Status = HttpStatusCode.NotFound.ToString();
                _getPaymentsResponseDto.Message = ex.Message;
                return _getPaymentsResponseDto;
            }
        }
        _getPaymentsResponseDto.Status = HttpStatusCode.BadRequest.ToString();
        _getPaymentsResponseDto.Message = "¡¡El JWT no se valido Correctamente!!";
        return _getPaymentsResponseDto;
    }


    /// <summary>
    /// Método GetPaymentsID
    /// </summary>
    /// <param name="getPaymentsIDDto"></param>
    /// <returns></returns>
    public async Task<GetPaymentsIDResponseDto> GetPaymentsID(GetPaymentsIDDto getPaymentsIDDto)
    {
        GetPaymentsIDResponseDto response = await ConsumoWebServiceConsultaPagosPorID(getPaymentsIDDto);

        return MapResponseID(getPaymentsIDDto, response);
    }

    [HttpGet]
    private async Task<GetPaymentsIDResponseDto> ConsumoWebServiceConsultaPagosPorID(GetPaymentsIDDto getPaymentsIDDto)
    {
        GetPaymentsIDResponseDto _getPaymentsIDResponseDto = new();
        RefreshToken _refreshToken = new(_connection, _contextAccessor);

        //Obtenemos las variables globales
        string _baseUrl = GlobalConnection.Current.Host;

        var refresResponse = await _refreshToken.DoRefreshToken();
        string _JWTToken = refresResponse.Data.JWT;
        var id = getPaymentsIDDto.Id;

        if (refresResponse.Status.Equals("success"))
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("GINIH");
                if (!string.IsNullOrEmpty(_baseUrl) && Uri.IsWellFormedUriString(_baseUrl, UriKind.RelativeOrAbsolute))
                {
                    client.BaseAddress = new Uri(_baseUrl);
                }
                client.DefaultRequestHeaders.Add("Authorization", _JWTToken);

                using HttpResponseMessage response = await client.GetAsync(client.BaseAddress + "/payments/{" + id + "}");
                var json_Respuesta = await response.Content.ReadAsStringAsync();
                var deserialized = JsonConvert.DeserializeObject<GetPaymentsIDResponseDto>(json_Respuesta);

                if (deserialized is not null)
                {
                    _getPaymentsIDResponseDto = deserialized;
                    _getPaymentsIDResponseDto.Status = response.StatusCode.ToString();
                    return _getPaymentsIDResponseDto;
                }
                _getPaymentsIDResponseDto.Status = response.StatusCode.ToString();
                _getPaymentsIDResponseDto.Message = "La Consulta no devolvio nada";
                return _getPaymentsIDResponseDto;
            }
            catch (Exception ex)
            {
                _getPaymentsIDResponseDto.Status = HttpStatusCode.NotFound.ToString();
                _getPaymentsIDResponseDto.Message = ex.Message;
                return _getPaymentsIDResponseDto;
            }
        }
        _getPaymentsIDResponseDto.Status = HttpStatusCode.BadRequest.ToString();
        _getPaymentsIDResponseDto.Message = "¡¡El JWT no se valido Correctamente!!";
        return _getPaymentsIDResponseDto;

    }

    /// <summary>
    /// Método PostPayment
    /// </summary>
    /// <param name="postPaymentDto"></param>
    /// <returns></returns>
    public async Task<PostPaymentResponseDto> PostPayments(PostPaymentDto postPaymentDto)
    {
        PostPaymentResponseDto response = await ConsumoWebServicePosteaPagos(postPaymentDto);

        return MapPostResponse(postPaymentDto, response);
    }

    [HttpPost]
    private async Task<PostPaymentResponseDto> ConsumoWebServicePosteaPagos(PostPaymentDto postPaymentDto)
    {
        PostPaymentResponseDto _postPaymentsResponseDto = new();
        PostPaymentDtoFinal postPaymentDtoFinal = new();
        CustomAttributes _custom = new();
        RefreshToken _refreshToken = new(_connection, _contextAccessor);

        URLsExt _url = new();
        //Obtenemos las variables globales
        string _baseUrl = GlobalConnection.Current.Host;
        var refresResponse = await _refreshToken.DoRefreshToken();
        string _JWTToken = refresResponse.Data.JWT;

        postPaymentDtoFinal = GenerarPostPayment(postPaymentDto.CamposObligatoriosModel.Guid, out bool exitoso);

        if (refresResponse.Status.Equals("success"))
        {
            if (exitoso)
            {
                try
                {
                    using var client = _httpClientFactory.CreateClient("GINIH");
                    if (!string.IsNullOrEmpty(_baseUrl) && Uri.IsWellFormedUriString(_baseUrl, UriKind.RelativeOrAbsolute))
                    {
                        client.BaseAddress = new Uri(_baseUrl);
                    }

                    // var content = _custom.SerializeObjectWhitAttribute(postPaymentDtoFinal);
                    var content = System.Text.Json.JsonSerializer.Serialize(postPaymentDtoFinal, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = null, // Respeta exactamente los nombres definidos
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });

                    var data = new StringContent(content, Encoding.UTF8, "application/json");

                    client.DefaultRequestHeaders.Add("Authorization", _JWTToken);
                    client.DefaultRequestHeaders.Add("idempotencyKey", Guid.NewGuid().ToString());

                    using HttpResponseMessage response = await client.PostAsync(client.BaseAddress + "/payments", data);
                    var responseContent = response.Content.ReadAsStringAsync().Result;
                    var deserialized = JsonConvert.DeserializeObject<PostPaymentResponseDto>(responseContent);

                    if (deserialized?.Data is not null && (response.StatusCode.ToString().Equals("OK") || response.StatusCode.ToString().Equals("success"))) //deserialized.Data.Count != 0 &&  response.StatusCode.ToString().Equals("OK")
                    {
                        _postPaymentsResponseDto = deserialized;
                        _postPaymentsResponseDto.Status = response.StatusCode.ToString();
                        _postPaymentsResponseDto.Error = "0";
                        _postPaymentsResponseDto.Mensaje = "PROCESADO EXITOSAMENTE";
                        return _postPaymentsResponseDto;
                    }
                    _postPaymentsResponseDto = deserialized ?? new PostPaymentResponseDto();
                    _postPaymentsResponseDto.Status = response.StatusCode.ToString();
                    _postPaymentsResponseDto.Error = "1";
                    _postPaymentsResponseDto.Mensaje = "PROCESO NO DEVOLVIO VALORES";
                    _postPaymentsResponseDto.Message = "La consulta no devolvio valores";
                    return _postPaymentsResponseDto;
                }
                catch (Exception ex)
                {
                    _postPaymentsResponseDto.Status = HttpStatusCode.NotFound.ToString();
                    _postPaymentsResponseDto.Error = "1";
                    _postPaymentsResponseDto.Mensaje = "ERROR AL EJECUTAR PETICIÓN A SERVICIO EXTERNO.";
                    _postPaymentsResponseDto.Message = ex.Message;
                    return _postPaymentsResponseDto;
                }
            }
            else
            {
                _postPaymentsResponseDto.Status = HttpStatusCode.BadRequest.ToString();
                _postPaymentsResponseDto.Error = "1";
                _postPaymentsResponseDto.Message = "No se pudo Leer la tabla CYBERDTA.CYBUTHDP, y no se genero el objeto a enviar";
                _postPaymentsResponseDto.Message = "Lectura de tabla erronea";
                return _postPaymentsResponseDto;

            }
        }
        _postPaymentsResponseDto.Status = HttpStatusCode.BadRequest.ToString();
        _postPaymentsResponseDto.Error = "1";
        _postPaymentsResponseDto.Message = "¡¡El JWT no se valido Correctamente!!";
        return _postPaymentsResponseDto;
    }



    private GetPaymentsResponseDto MapResponse(GetPaymentsDto getPaymentsDto, GetPaymentsResponseDto getPaymentsResponseDto)
    {
        _connection.Open();

        try
        {
            if ((getPaymentsResponseDto.Status == "success" || getPaymentsResponseDto.Status == "OK") && _connection.IsConnected)
            {
                int correlativo = 0;
                FieldsQuery param = new();

                string sqlQuery = "INSERT INTO BCAH96DTA.UTH04APU (APU00GUID,  APU01CORR,  APU02FECH,  APU03HORA,  APU04CAJE,  APU05BANC,  APU06SUCU,  APU07TERM,  APU08STAT,  APU09MSSG,  APU10DTID,  APU11DTNA,  APU12CUID,  APU13CUNA,  APU14COID,  APU15CONA,  APU16DREF,  APU17REFE,  APU18AMVA,  APU19AMCU,  APU20SUTO,  APU19PFEE,  APU20SCHA,  APU21DICO,  APU22BTAX,  APU23TOTA,  APU24CRAT,  APU25TIST,  APU26COVA,  APU27CONA,  APU28ERRO,  APU29MENS) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
                using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
                command.CommandText = sqlQuery;

                command.CommandType = System.Data.CommandType.Text;

                param.AddOleDbParameter(command, "APU00GUID", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Guid);
                param.AddOleDbParameter(command, "APU01CORR", OleDbType.Numeric, correlativo);
                param.AddOleDbParameter(command, "APU02FECH", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Fecha);
                param.AddOleDbParameter(command, "APU03HORA", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Hora);
                param.AddOleDbParameter(command, "APU04CAJE", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Cajero);
                param.AddOleDbParameter(command, "APU05BANC", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Banco);
                param.AddOleDbParameter(command, "APU06SUCU", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Sucursal);
                param.AddOleDbParameter(command, "APU07TERM", OleDbType.Char, getPaymentsDto.CamposObligatoriosModel.Terminal);
                param.AddOleDbParameter(command, "APU08STAT", OleDbType.Char, getPaymentsResponseDto.Status);
                param.AddOleDbParameter(command, "APU09MSSG", OleDbType.Char, getPaymentsResponseDto.Message);
                param.AddOleDbParameter(command, "APU10DTID", OleDbType.Char, getPaymentsResponseDto.Data.Id);
                param.AddOleDbParameter(command, "APU11DTNA", OleDbType.Char, getPaymentsResponseDto.Data.Name);
                param.AddOleDbParameter(command, "APU12CUID", OleDbType.Char, getPaymentsResponseDto.Data.Customer.Id);
                param.AddOleDbParameter(command, "APU13CUNA", OleDbType.Char, getPaymentsResponseDto.Data.Customer.Name);
                param.AddOleDbParameter(command, "APU14COID", OleDbType.Char, getPaymentsResponseDto.Data.Company.Id);
                param.AddOleDbParameter(command, "APU15CONA", OleDbType.Char, getPaymentsResponseDto.Data.Company.Name);
                param.AddOleDbParameter(command, "APU16DREF", OleDbType.Char, getPaymentsResponseDto.Data.DocumentReference);
                param.AddOleDbParameter(command, "APU17REFE", OleDbType.Char, getPaymentsResponseDto.Data.ReferenceId);
                param.AddOleDbParameter(command, "APU18AMVA", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Value);
                param.AddOleDbParameter(command, "APU19AMCU", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Currency);
                param.AddOleDbParameter(command, "APU20SUTO", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.Subtotal);
                param.AddOleDbParameter(command, "APU19PFEE", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.ProcessingFee);
                param.AddOleDbParameter(command, "APU20SCHA", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.Surcharge);
                param.AddOleDbParameter(command, "APU21DICO", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.Discount);
                param.AddOleDbParameter(command, "APU22BTAX", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.Tax);
                param.AddOleDbParameter(command, "APU23TOTA", OleDbType.Char, getPaymentsResponseDto.Data.Amount.Breakdown.Total);
                param.AddOleDbParameter(command, "APU24CRAT", OleDbType.Char, getPaymentsResponseDto.Data.CreatedAt);
                param.AddOleDbParameter(command, "APU25TIST", OleDbType.Char, getPaymentsResponseDto.TimeStamp);
                param.AddOleDbParameter(command, "APU26COVA", OleDbType.Char, getPaymentsResponseDto.Code.Value);
                param.AddOleDbParameter(command, "APU27CONA", OleDbType.Char, getPaymentsResponseDto.Code.Name);
                param.AddOleDbParameter(command, "APU28ERRO", OleDbType.Char, getPaymentsResponseDto.Error);
                param.AddOleDbParameter(command, "APU29MENS", OleDbType.Char, getPaymentsResponseDto.Mensaje);

                command.ExecuteNonQuery();


            }

            return getPaymentsResponseDto;

        }
        catch (Exception ex)
        {
            GetPaymentsResponseDto _getPaymentsResponseDto = new();
            _getPaymentsResponseDto.Mensaje = ex.Message;
            _getPaymentsResponseDto.Error = "106";
            _getPaymentsResponseDto.Status = "InternalServerError";
            _getPaymentsResponseDto.Code.Value = ((int)HttpStatusCode.InternalServerError).ToString();
            _getPaymentsResponseDto.Code.Name = HttpStatusCode.InternalServerError.ToString();
            return _getPaymentsResponseDto;
        }


    }
    private GetPaymentsIDResponseDto MapResponseID(GetPaymentsIDDto getPaymentsIDDto, GetPaymentsIDResponseDto getPaymentsIDResponseDto)
    {
        _connection.Open();


        return getPaymentsIDResponseDto;
    }
    private PostPaymentResponseDto MapPostResponse(PostPaymentDto postPaymentDto, PostPaymentResponseDto postPaymentResponseDto)
    {
        _connection.Open();

        try
        {
            /*   if ((postPaymentResponseDto.Status == "success" || postPaymentResponseDto.Status == "OK") && conection.Connect.CheckConfigurationState)
               {
                   _iSunitpService.AddObjLog("CLASE PAYMENTS SERVICES LLAMADO CORE", "0000000000000000000", "OBJETO ENVIADO", postPaymentDto);
                   int correlativo = 0;


                   string sqlQuery = "INSERT INTO BCAH96DTA.UTH04APU (APU00GUID,  APU01CORR,  APU02FECH,  APU03HORA,  APU04CAJE,  APU05BANC,  APU06SUCU,  APU07TERM,  APU08STAT,  APU09MSSG,  APU10DTID,  APU11DTNA,  APU12CUID,  APU13CUNA,  APU14COID,  APU15CONA,  APU16DREF,  APU17REFE,  APU18AMVA,  APU19AMCU,  APU20SUTO,  APU19PFEE,  APU20SCHA,  APU21DICO,  APU22BTAX,  APU23TOTA,  APU24CRAT,  APU25TIST,  APU26COVA,  APU27CONA,  APU28ERRO,  APU29MENS) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
                   using (OleDbCommand command = new OleDbCommand(sqlQuery, conection.Connect.OleDbConnection))
                   {
                        param.AddOleDbParameter(command,"APU00GUID", OleDbType.Char, postPaymentDto.CamposObligatoriosModel.Guid;
                        param.AddOleDbParameter(command,"APU01CORR", OleDbType.Numeric, correlativo;
                        param.AddOleDbParameter(command,"APU02FECH", OleDbType.Char, postPaymentDto.CamposObligatoriosModel.Fecha;
                        param.AddOleDbParameter(command,"APU03HORA", OleDbType.Char, postPaymentDto.CamposObligatoriosModel.Hora;
                        param.AddOleDbParameter(command,"APU04CAJE", OleDbType.Char, postPaymentDto.CamposObligatoriosModel.Cajero;
                        param.AddOleDbParameter(command,"APU05BANC", OleDbType.Char, postPaymentDto.CamposObligatoriosModel.Banco;
                        param.AddOleDbParameter(command,"APU06SUCU", OleDbType.Char, postPaymentDto.CamposObligatoriosModel.Sucursal;
                        param.AddOleDbParameter(command,"APU07TERM", OleDbType.Char, postPaymentDto.CamposObligatoriosModel.Terminal;
                        param.AddOleDbParameter(command,"APU08STAT", OleDbType.Char, postPaymentResponseDto.Status;
                        param.AddOleDbParameter(command,"APU09MSSG", OleDbType.Char, postPaymentResponseDto.Message;
                        param.AddOleDbParameter(command,"APU10DTID", OleDbType.Char, postPaymentResponseDto.Data.Id;
                        param.AddOleDbParameter(command,"APU11DTNA", OleDbType.Char, postPaymentResponseDto.Data.Name;
                        param.AddOleDbParameter(command,"APU12CUID", OleDbType.Char, postPaymentResponseDto.Data.Customer.Id;
                        param.AddOleDbParameter(command,"APU13CUNA", OleDbType.Char, postPaymentResponseDto.Data.Customer.Name;
                        param.AddOleDbParameter(command,"APU14COID", OleDbType.Char, postPaymentResponseDto.Data.Company.Id;
                        param.AddOleDbParameter(command,"APU15CONA", OleDbType.Char, postPaymentResponseDto.Data.Company.Name;
                        param.AddOleDbParameter(command,"APU16DREF", OleDbType.Char, postPaymentResponseDto.Data.DocumentReference;
                        param.AddOleDbParameter(command,"APU17REFE", OleDbType.Char, postPaymentResponseDto.Data.ReferenceId;
                        param.AddOleDbParameter(command,"APU18AMVA", OleDbType.Char, postPaymentResponseDto.Data.Amount.Value;
                        param.AddOleDbParameter(command,"APU19AMCU", OleDbType.Char, postPaymentResponseDto.Data.Amount.Currency;
                        param.AddOleDbParameter(command,"APU20SUTO", OleDbType.Char, postPaymentResponseDto.Data.Amount.Breakdown.Subtotal;
                        param.AddOleDbParameter(command,"APU19PFEE", OleDbType.Char, postPaymentResponseDto.Data.Amount.Breakdown.ProcessingFee;
                        param.AddOleDbParameter(command,"APU20SCHA", OleDbType.Char, postPaymentResponseDto.Data.Amount.Breakdown.Surcharge;
                        param.AddOleDbParameter(command,"APU21DICO", OleDbType.Char, postPaymentResponseDto.Data.Amount.Breakdown.Discount;
                        param.AddOleDbParameter(command,"APU22BTAX", OleDbType.Char, postPaymentResponseDto.Data.Amount.Breakdown.Tax;
                        param.AddOleDbParameter(command,"APU23TOTA", OleDbType.Char, postPaymentResponseDto.Data.Amount.Breakdown.Total;
                        param.AddOleDbParameter(command,"APU24CRAT", OleDbType.Char, postPaymentResponseDto.Data.CreatedAt;
                        param.AddOleDbParameter(command,"APU25TIST", OleDbType.Char, postPaymentResponseDto.Timestamp;
                        param.AddOleDbParameter(command,"APU26COVA", OleDbType.Char, postPaymentResponseDto.Code.Value;
                        param.AddOleDbParameter(command,"APU27CONA", OleDbType.Char, postPaymentResponseDto.Code.Name;
                        param.AddOleDbParameter(command,"APU28ERRO", OleDbType.Char, postPaymentResponseDto.Error;
                        param.AddOleDbParameter(command,"APU29MENS", OleDbType.Char, postPaymentResponseDto.Mensaje;

                       command.ExecuteNonQuery();
                   }

               }*/

            return postPaymentResponseDto;

        }
        catch (Exception ex)
        {
            postPaymentResponseDto.Mensaje = ex.Message;
            postPaymentResponseDto.Message = "Error al Guardar datos en tabla";
            postPaymentResponseDto.Error = "106";
            postPaymentResponseDto.Status = "InternalServerError";
            postPaymentResponseDto.Code.Value = ((int)HttpStatusCode.InternalServerError).ToString();
            postPaymentResponseDto.Code.Name = HttpStatusCode.InternalServerError.ToString();
            return postPaymentResponseDto;

        }
    }

    private PostPaymentDtoFinal GenerarPostPayment(string guid, out bool exitoso)
    {
        PostPaymentDtoFinal postPaymentDtoFinal = new();
        _connection.Open();
        exitoso = false;
        try
        {
            DateTime nowUTC = DateTime.UtcNow;
            string fechaISO8601 = nowUTC.ToString("yyyy-dd-MMTHH:mm:ss.fffZ");

            if (_connection.IsConnected)
            {

                FieldsQuery param = new();

                string sqlQuery = "SELECT * FROM CYBERDTA.CYBUTHDP WHERE HDP00GUID = ?";

                using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
                command.CommandText = sqlQuery;
                command.CommandType = System.Data.CommandType.Text;

                param.AddOleDbParameter(command, "HDP00GUID", OleDbType.Char, guid);               

                var reader = (DbDataReader)command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        //Datos

                        postPaymentDtoFinal.Amount.Value = ConvertirAEnteroGinih(reader, "HDP01MTTO"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP01MTTO"))); //Value
                        postPaymentDtoFinal.Amount.Currency = reader.GetString(reader.GetOrdinal("HDP02MONE")); //Moneda
                        postPaymentDtoFinal.Amount.Breakdown.Subtotal = ConvertirAEnteroGinih(reader, "HDP03SUTO"); // Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP03SUTO"))); //Subtotal
                        postPaymentDtoFinal.Amount.Breakdown.ProcessingFee = ConvertirAEnteroGinih(reader, "HDP04PRFE"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP04PRFE"))); //Processing Fee
                        postPaymentDtoFinal.Amount.Breakdown.Surcharge = ConvertirAEnteroGinih(reader, "HDP05MTCA"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP05MTCA"))); //Surcharge cargo
                        postPaymentDtoFinal.Amount.Breakdown.Discount = ConvertirAEnteroGinih(reader, "HDP06MTDE"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP06MTDE"))); //Discount descuento
                        postPaymentDtoFinal.Amount.Breakdown.Tax = ConvertirAEnteroGinih(reader, "HDP07MTIM"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP07MTIM"))); //Tax impuesto
                        postPaymentDtoFinal.Amount.Breakdown.Total = ConvertirAEnteroGinih(reader, "HDP08MTTO"); //Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP08MTTO"))); //Total
                        postPaymentDtoFinal.CustomerID = reader.GetString(reader.GetOrdinal("HDP09CUID"));//Customer ID                                
                        postPaymentDtoFinal.PaymentDate = fechaISO8601;//Utilitarios.ConvertirFecha(reader.GetString(reader.GetOrdinal("HDP10PADA")));//Fecha Pago
                        postPaymentDtoFinal.ReferenceId = guid; //reader.GetString(reader.GetOrdinal("HDP11REID"));//Referencia
                        postPaymentDtoFinal.PayableOption = reader.GetString(reader.GetOrdinal("HDP12PAOP"));//Opcion de Pago
                        postPaymentDtoFinal.CompanyID = reader.GetString(reader.GetOrdinal("HDP13COID"));//Codigo Compañia
                        postPaymentDtoFinal.ReceivableID = reader.GetString(reader.GetOrdinal("HDP14GEN1"));//ReceivableID

                        postPaymentDtoFinal.AdditionalData = "{\"PaymentMethod\": \"cash\" }";
                        postPaymentDtoFinal.Channel = "interbanca";

                        //  postPaymentDtoFinal.PayableOption = reader.GetString(reader.GetOrdinal("HDP15GEN2"));//Opcion de Pago

                        // postPaymentDtoFinal.PayableOption =  Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP16GEN3")));//Opcion de Pago


                        // postPaymentDtoFinal.PayableOption =  Convert.ToInt32(reader.GetValue(reader.GetOrdinal("HDP17GEN4")));//Opcion de Pago

                        exitoso = true;
                    }
                }
            }
            return postPaymentDtoFinal;
        }
        catch //(Exception ex)
        {
            exitoso = false;
            return postPaymentDtoFinal;
        }
    }

    private int ConvertirAEnteroGinih(DbDataReader reader, string campoTabla)
    {

        decimal valorDecimal = reader.GetDecimal(reader.GetOrdinal(campoTabla));

        return Convert.ToInt32(valorDecimal * 100);
    }
}
