using Connections.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.CompaniesDtos;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.IServiceReference;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;
using Newtonsoft.Json;
using System.Data.OleDb;
using System.Net;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.ServiceReference.REST_UTH.Companies.Companies_Services;

/// <summary>
/// Clase de Servicio CompaniesServices
/// </summary>
/// <param name="_httpClientFactory">Instancia de IHttpClientFactory.</param>
/// <param name="_connection">Instancia de IDatabaseConnection</param>
/// <param name="_contextAccessor">Instancia de IHttpContextAccesor</param>
public class CompaniesServices(IHttpClientFactory _httpClientFactory, IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : ICompaniesServices
{

    /// <summary>
    /// Método DoProcessAsync para Companies.
    /// </summary>
    /// <param name="getCompaniesDto">Objeto DTO.</param>
    /// <returns>Objeto GetCompaniesResponseDto.</returns>
    public async Task<GetCompaniesResponseDto> DoProcessAsync(GetCompaniesDto getCompaniesDto)
    {
        GetCompaniesResponseDto response = await ConsumoWebServiceConsultaCompañiasPorCobrar(getCompaniesDto);

        return MapResponse(getCompaniesDto, response);
    }

    [HttpGet]
    private async Task<GetCompaniesResponseDto> ConsumoWebServiceConsultaCompañiasPorCobrar(GetCompaniesDto getCompaniesDto)
    {
        GetCompaniesResponseDto _getCompaniesResponseDto = new();
        RefreshToken _refreshToken = new(_connection, _contextAccessor);
        URLsExt _url = new();

        //Obtenemos las variables globales
        string _baseUrl = GlobalConnection.Current.Host;

        var refresResponse = await _refreshToken.DoRefreshToken();

        string _JWTToken = refresResponse.Data.JWT;
        string _limit = getCompaniesDto.Limit;
        string _nextToken = getCompaniesDto.NextToken;

        var baseAddressExtra = _baseUrl + "/companies?limit=" + _limit + "&NextToken=" + _nextToken;

        if (refresResponse.Status.Equals("success"))
        {
            try
            {
                using var client = _httpClientFactory.CreateClient("GINIH");

                if (!string.IsNullOrEmpty(_baseUrl) && Uri.IsWellFormedUriString(_baseUrl, UriKind.RelativeOrAbsolute))
                {
                    client.BaseAddress = new Uri(_url.QuerySchemeEmptyFilter(baseAddressExtra));
                }
                client.DefaultRequestHeaders.Add("Authorization", _JWTToken);

                using HttpResponseMessage response = await client.GetAsync(client.BaseAddress);
                var json_Respuesta = await response.Content.ReadAsStringAsync();
                var deserialized = JsonConvert.DeserializeObject<GetCompaniesResponseDto>(json_Respuesta);

                if (deserialized?.Data.Count != 0 && deserialized != null && (response.StatusCode.ToString().Equals("OK") || response.StatusCode.ToString().Equals("success")))
                {
                    _getCompaniesResponseDto = deserialized;
                    _getCompaniesResponseDto.Status = response.StatusCode.ToString();
                    _getCompaniesResponseDto.Error = "0";
                    _getCompaniesResponseDto.Mensaje = "Proceso ejecutado Satisfactoriamente";
                    return _getCompaniesResponseDto;
                }
                _getCompaniesResponseDto = deserialized ?? new GetCompaniesResponseDto();
                _getCompaniesResponseDto.Status = response.StatusCode.ToString();
                _getCompaniesResponseDto.Message = "La consulta no devolvio valores";
                _getCompaniesResponseDto.Error = "1";
                _getCompaniesResponseDto.Mensaje = "Proceso ejecutado InSatisfactoriamente";
                return _getCompaniesResponseDto;
            }
            catch (Exception ex)
            {
                _getCompaniesResponseDto.Status = HttpStatusCode.NotFound.ToString();
                _getCompaniesResponseDto.Message = ex.Message;
                _getCompaniesResponseDto.Error = "1";
                _getCompaniesResponseDto.Mensaje = "Proceso ejecutado InSatisfactoriamente";
                return _getCompaniesResponseDto;
            }
        }
        _getCompaniesResponseDto.Status = HttpStatusCode.BadRequest.ToString();
        _getCompaniesResponseDto.Message = "¡¡El JWT no se valido Correctamente!!";
        _getCompaniesResponseDto.Error = "1";
        _getCompaniesResponseDto.Mensaje = "Proceso ejecutado InSatisfactoriamente";
        return _getCompaniesResponseDto;
    }

    private GetCompaniesResponseDto MapResponse(GetCompaniesDto getCompaniesDto, GetCompaniesResponseDto getCompaniesResponseDto)
    {
        _connection.Open();

        try
        {
            if ((getCompaniesResponseDto.Status == "success" || getCompaniesResponseDto.Status == "OK") && _connection.IsConnected)
            {

                int correlativo = 0;
                GetCompaniesResponseDto[] arrayCompanies = new[] { getCompaniesResponseDto };
                string jsonString = JsonConvert.SerializeObject(arrayCompanies);
                var listGetCompaniesResponse = JsonConvert.DeserializeObject<List<GetCompaniesResponseDto>>(jsonString)!;
                FieldsQuery param = new();

                foreach (GetCompaniesResponseDto list2 in listGetCompaniesResponse)
                {
                    foreach (var list3 in list2.Data)
                    {
                        string sqlQuery = "INSERT INTO BCAH96DTA.UTH01CCC (CCC00GUID, CCC01CORR, CCC02FECH, CCC03HORA, CCC04CAJE, CCC05BANC, CCC06SUCU, CCC07TERM, CCC08LIMI, CCC09NTOK, CCC10STAT, CCC11MESS, CCC12DTID, CCC12DTNA, CCC13DTPO, CCC13TIST, CCC14MDTI, CCC15MDHM, CCC16MDNT, CCC17COVA, CCC18CONA, CCC19ERRO, CCC20MENS) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
                        using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
                        command.CommandText = sqlQuery;

                        command.CommandType = System.Data.CommandType.Text;
                        param.AddOleDbParameter(command,"CCC00GUID", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Guid);
                        param.AddOleDbParameter(command,"CCC01CORR", OleDbType.Char, correlativo.ToString());
                        param.AddOleDbParameter(command,"CCC02FECH", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Fecha);
                        param.AddOleDbParameter(command,"CCC03HORA", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Hora);
                        param.AddOleDbParameter(command,"CCC04CAJE", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Cajero);
                        param.AddOleDbParameter(command,"CCC05BANC", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Banco);
                        param.AddOleDbParameter(command,"CCC06SUCU", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Sucursal);
                        param.AddOleDbParameter(command,"CCC07TERM", OleDbType.Char, getCompaniesDto.CamposObligatoriosModel.Terminal);
                        param.AddOleDbParameter(command,"CCC08LIMI", OleDbType.Char, getCompaniesDto.Limit);
                        param.AddOleDbParameter(command,"CCC09NTOK", OleDbType.Char, getCompaniesDto.NextToken);
                        param.AddOleDbParameter(command,"CCC10STAT", OleDbType.Char, list2.Status);
                        param.AddOleDbParameter(command,"CCC11MESS", OleDbType.Char, list2.Message);
                        param.AddOleDbParameter(command,"CCC12DTID", OleDbType.Char, list3.Id);
                        param.AddOleDbParameter(command,"CCC12DTNA", OleDbType.Char, list3.Name);
                        param.AddOleDbParameter(command,"CCC13DTPO", OleDbType.Char, (list3.PayableOptions == null) ? "Nulo" : string.Join(",", list3.PayableOptions));
                        param.AddOleDbParameter(command,"CCC13TIST", OleDbType.Char, list2.Timestamp.ToString());
                        param.AddOleDbParameter(command,"CCC14MDTI", OleDbType.Char, list2.Metadata.Items);
                        param.AddOleDbParameter(command,"CCC15MDHM", OleDbType.Char, ((list2.Metadata.HasMore.ToString() == null) ? "NO" : list2.Metadata.HasMore.ToString()));
                        param.AddOleDbParameter(command,"CCC16MDNT", OleDbType.Char, ((list2.Metadata.NextToken == null) ? "" : list2.Metadata.NextToken));
                        param.AddOleDbParameter(command,"CCC17COVA", OleDbType.Char, ((list2.Code.Value.ToString() == null) ? "124" : list2.Code.Value.ToString()));
                        param.AddOleDbParameter(command,"CCC18CONA", OleDbType.Char, ((list2.Code.Name == null) ? "" : list2.Code.Name));
                        param.AddOleDbParameter(command,"CCC19ERRO", OleDbType.Char, getCompaniesResponseDto.Error);
                        param.AddOleDbParameter(command,"CCC20MENS", OleDbType.Char, ((getCompaniesResponseDto.Mensaje == null) ? "Proceso ejecutado Satisfactoriamente" : getCompaniesResponseDto.Mensaje));

                        command.ExecuteNonQuery();

                        correlativo++;
                    }
                }
            }
            return getCompaniesResponseDto;
        }
        catch (Exception ex)
        {
            getCompaniesResponseDto.Mensaje = ex.Message;
            getCompaniesResponseDto.Error = "106";
            getCompaniesResponseDto.Status = "InternalServerError";


            return getCompaniesResponseDto;
        }
    }
}
