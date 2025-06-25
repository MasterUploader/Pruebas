
using Microsoft.AspNetCore.Mvc;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.AutenticacionDtos;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Repository.IRepository.Autenticacion;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.Controllers;

/// <summary>
/// Clase Login, requeridad para autenticar el usuario con Ginih.
/// </summary>
/// <param name="_loginRepository">Instancia de Clade LoginRepository</param>
[Route("[controller]")]
[ApiController]
public class LoginController (ILoginRepository _loginRepository) : ControllerBase
{

    /// <summary>
    /// Acciones relacionadas con los usuarios habilitados para realizar acciones API y en el portal
    /// Importante saber: Las operaciones realizadas por el API son realizadas con un JWT que tiene una duración de 5 minutos.
    /// La empresa proporcionará acceso al portal para tener el usuario y contraseña que le permitirá obtener un refresh-token que debe ser usado para obtener los tokens de corta duración.
    /// </summary>
    /// <returns>Retorna un Objeto PostLoginResponseDto</returns>
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login()
    {
        PostUsuarioLoginDto usuarioLoginDto = new() {
            UserName = GlobalConnection.Current.GinihUser,
            Password = GlobalConnection.Current.GinihPassword
        };

        ResponseHandler responseHandler = new();
        var postLoginResponseDto = await _loginRepository.Login(usuarioLoginDto);
        return responseHandler.HandleResponse(postLoginResponseDto, postLoginResponseDto.Status);
    }
}



using Azure;
using Connections.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyModel;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.AutenticacionDtos;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Repository.IRepository.Autenticacion;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SUNITP.LIB.ManagerProcedures.Concrete;
using SUNITP.LIB.QueryStringGenerator;
using System.Data.OleDb;
using System.Globalization;
using System.Net;
using System.Text;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.Repository;

/// <summary>
/// Clase Login Repository.
/// </summary>
public class LoginRepository (IHttpClientFactory _httpClientFactory)  : ILoginRepository
{


    /// <summary>
    /// Para obtener un refresh-token necesitas hacer una llamada al endpoint de login. La respuesta a esta llamada obtendrá un secret token que necesitarás para obtener JWT válidos para los request. 
    /// El JWT dura 5 minutos y el refresh-token tiene una duración por defecto de 2 años (Recibirás una notificación para que puedas renovarlo antes de vencer). Si ya tienes acceso al portal de integración, puedes acceder o volver a generar uno nuevo en la vista developer -> secret.
    /// </summary>
    /// <param name="usuarioLoginDto">Objeto de Transferencia Dto</param>
    /// <returns>Retorna un Objeto de Tipo PostLoginResponseDto</returns>

    [HttpPost]
    public async Task<PostLoginResponseDto> Login(PostUsuarioLoginDto usuarioLoginDto)
    {
        try
        {
            //Obtenemos las variables globales
            string _baseUrl = GlobalConnection.Current.Host;

            PostLoginResponseDto _postLoginResponseDto = new();

            using var client = _httpClientFactory.CreateClient("GINIH");
            if (!string.IsNullOrEmpty(_baseUrl) && Uri.IsWellFormedUriString(_baseUrl, UriKind.RelativeOrAbsolute))
            {
                client.BaseAddress = new Uri(_baseUrl);
            }

            var content = new StringContent(JsonConvert.SerializeObject(usuarioLoginDto), Encoding.UTF8);

            using HttpResponseMessage response = await client.PostAsync(client.BaseAddress + "/users/login", content);
            var responseContent = response.Content.ReadAsStringAsync().Result;
            var deserialized = JsonConvert.DeserializeObject<PostLoginResponseDto>(responseContent);

            if (response.IsSuccessStatusCode && deserialized is not null)
            {
                _postLoginResponseDto = deserialized;
                _postLoginResponseDto.Status = response.StatusCode.ToString();
                var saveToken = new Token(deserialized);
                var badResponse = new PostLoginResponseDto
                {
                    Status = HttpStatusCode.NotFound.ToString(),
                    Message = "No se pudo guardar datos en la tabla de token"
                };
                return saveToken.SavenTokenUTH() ? _postLoginResponseDto : badResponse;
            }
            else if (!response.IsSuccessStatusCode && deserialized is not null)
            {
                _postLoginResponseDto = deserialized;
                _postLoginResponseDto.Status = response.StatusCode.ToString();
                return _postLoginResponseDto;
            }
            _postLoginResponseDto.Status = response.StatusCode.ToString();
            _postLoginResponseDto.Message = deserialized!.Message ?? "No devolvio valores la consulta";
            return _postLoginResponseDto;
        }
        catch (Exception ex)
        {
            PostLoginResponseDto _postLoginResponseDto = new() {
                Status = HttpStatusCode.NotFound.ToString(),
                Message = ex.Message
            };

            return _postLoginResponseDto;
        }
    }
}


using Connections.Interfaces;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.AutenticacionDtos;
using SUNITP.LIB.ManagerProcedures;
using SUNITP.LIB.ManagerProcedures.Concrete;
using SUNITP.LIB.QueryStringGenerator;
using System.Data.OleDb;
using System.Globalization;
using System.Runtime.Versioning;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils;
/// <summary>
/// Clase Utilitaria Token, contiene multiples métodos requeridos para manipular los tokens de Ginih.
/// </summary>
public class Token
{
    private readonly IDatabaseConnection _connection;
    //private readonly Connector conection = new();
    private EasyMappingTool response = new();
    private readonly string? _tableName;
    private readonly string? _library;
    private readonly string _status = string.Empty;
    private readonly string _message = string.Empty;
    private readonly string _rToken = string.Empty;
    private readonly string _createdAt = string.Empty;
    private readonly string _timeStamp = string.Empty;
    private readonly string _value = string.Empty;
    private readonly string _name = string.Empty;
    private string _vence = string.Empty;
    private string _creado = string.Empty;
    private string _token = string.Empty;

    /// <summary>
    /// Constructor de la Clase Token.
    /// </summary>
    /// <param name="tokenStructure">Instancia de PostLoginResponseDto</param>
    public Token(PostLoginResponseDto tokenStructure)
    {
        var appSettings = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
        _tableName = appSettings.GetSection("ApiSettings:TableName").Value;
        _library = appSettings.GetSection("ApiSettings:Library").Value;
        _status = tokenStructure.Status;
        _message = tokenStructure.Message;
        _rToken = tokenStructure.Data.RefreshToken;
        _createdAt = tokenStructure.Data.CreatedAt;
        _timeStamp = tokenStructure.TimeStamp;
        _value = tokenStructure.Code.Value;
        _name = tokenStructure.Code.Name;
    }


    public Token(IDatabaseConnection connection)
    {
        _connection = connection;
    }

    /// <summary>
    /// Constructo de Clase Token sin parametros de ingreso, inicializa campos.
    /// </summary>
    public Token()
    {
        var appSettings = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
        _tableName = appSettings.GetSection("ApiSettings:TableName").Value;
        _library = appSettings.GetSection("ApiSettings:Library").Value;
    }

    /// <summary>
    /// Guarda el Token en la tabla en el as400
    /// </summary>
    /// <returns>Retorna un valor boleano segun sea exitoso o no el almacenamiento</returns>
    public bool SavenTokenUTH()
    {
        try
        {
            var temp = DateTime.ParseExact(_createdAt, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture).AddYears(2);
            _vence = temp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            _connection.Open();
            var oleDBCommand = _connection.GetDbCommand();
            var oleDBConnection = (OleDbConnection) oleDBCommand.Connection;

            var sqsg = new ServiceQueryStringGenerator();
            EasyCrudDataModels ecdm = new(oleDBConnection);
            var fQuery = new FieldsQuery();
            var validacion = fQuery.FieldQuery("ID", "1", OleDbType.Integer, 1, "0");

            sqsg._iQueryStringGenerator.SelectAll();
            sqsg._iQueryStringGenerator.From(_library, _tableName);
            sqsg._iQueryStringGenerator.WhereAnd(validacion, "=");
            var responseS = ecdm.SelectExecute(sqsg);

            if (responseS.Count == 0)
            {
                sqsg = new ServiceQueryStringGenerator();
                sqsg._iQueryStringGenerator.InsertIntoFrom(_library, _tableName);
                sqsg._iQueryStringGenerator.InsertValue("ID", "1", OleDbType.Integer, 1, 0);
                sqsg._iQueryStringGenerator.InsertValue("STATUS", _status, OleDbType.VarChar, 50, 0);
                sqsg._iQueryStringGenerator.InsertValue("MESSAGE", _message, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.InsertValue("RTOKEN", _rToken, OleDbType.VarChar, 2000, 0);
                sqsg._iQueryStringGenerator.InsertValue("CREATEDAT", _createdAt, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.InsertValue("TIMESTAMP", _timeStamp, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.InsertValue("VALUE", _value, OleDbType.VarChar, 3, 0);
                sqsg._iQueryStringGenerator.InsertValue("NAME", _name, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.InsertValue("VENCE", _vence, OleDbType.VarChar, 100, 0);

                //respuesta del query
                response = ecdm.InsertExecute(sqsg);
            }
            else
            {
                sqsg = new ServiceQueryStringGenerator();
                sqsg._iQueryStringGenerator.UpdateFrom(_library, _tableName);
                sqsg._iQueryStringGenerator.UpdateSet("ID", "1", OleDbType.Integer, 1, 0);
                sqsg._iQueryStringGenerator.UpdateSet("STATUS", _status, OleDbType.VarChar, 50, 0);
                sqsg._iQueryStringGenerator.UpdateSet("MESSAGE", _message, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.UpdateSet("RTOKEN", _rToken, OleDbType.VarChar, 2000, 0);
                sqsg._iQueryStringGenerator.UpdateSet("CREATEDAT", _createdAt, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.UpdateSet("TIMESTAMP", _timeStamp, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.UpdateSet("VALUE", _value, OleDbType.VarChar, 3, 0);
                sqsg._iQueryStringGenerator.UpdateSet("NAME", _name, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.UpdateSet("VENCE", _vence, OleDbType.VarChar, 100, 0);
                sqsg._iQueryStringGenerator.WhereAnd(validacion, "=");

                //respuesta del query
                response = ecdm.UpdateExecute(sqsg);
            }


            if (response.GetEasyParameter("_defaultError").value.Equals(""))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            //Temporal hasta manejar los logs
            Console.Clear();
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Metodo que obtiene el token secreto de la tabla en el as400
    /// </summary>
    /// <param name="rToken">Token registrado.</param>
    /// <returns>Retorna un string de tipo out por parametro</returns>
   
    public bool GetToken(out string rToken)
    {
        try
        {
            _connection.Open();
            var oleDBCommand = _connection.GetDbCommand();
            var oleDBConnection = (OleDbConnection)oleDBCommand.Connection;

            var sqsg = new ServiceQueryStringGenerator();
            EasyCrudDataModels ecdm = new(oleDBConnection);

            var fQuery = new FieldsQuery();
            var validacion = fQuery.FieldQuery("ID", "1", OleDbType.Integer, 1, "0");

            sqsg._iQueryStringGenerator.SelectAll();
            sqsg._iQueryStringGenerator.From(_library, _tableName);
            sqsg._iQueryStringGenerator.WhereAnd(validacion, "=");
            var responseS = ecdm.SelectExecute(sqsg);

            //Cerrar Conexión
            _connection.Close();

            if (responseS.Count == 0)
            {
                rToken = "";
                return false;
            }

            foreach (var item in responseS)
            {
                _vence = item.GetValue("VENCE");
                _creado = item.GetValue("CREATEDAT");
                _token = item.GetValue("RTOKEN");
            }

            DateTime date1 = DateTime.ParseExact(_vence, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            DateTime date2 = DateTime.ParseExact(_creado, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

            var diferenceTime = date1 - date2;

            if (diferenceTime.Days > 0)
            {
                rToken = _token;
                return true;
            }
            rToken = "";
            return false;
        }
        catch (Exception ex)
        {
            //Temporal hasta manejar los logs
            Console.Clear();
            Console.WriteLine(ex.Message);
            _connection.Close();
            rToken = "";
            return false;
        }

    }
}


using Microsoft.AspNetCore.Mvc;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Models.Dtos.RefreshDtos;
using MS_BAN_38_UTH_RECAUDACION_PAGOS.Repository.IRepository.Autenticacion;
using Newtonsoft.Json;
using System.Net;
using System.Runtime.Versioning;

namespace MS_BAN_38_UTH_RECAUDACION_PAGOS.Utils
{
    public class RefreshToken : IRefreshTokenRepository
    {
        protected GetRefreshResponseDto _refreshResponseDto;
        private readonly string? _baseUrl;

        public RefreshToken()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            _baseUrl = builder.GetSection("ApiSettings:baseUrl").Value;
            _refreshResponseDto = new GetRefreshResponseDto();
        }

        /// <summary>
        /// La duración del JWT es de 5 minutos, con el secret que tienes, necesitas refrescar el JWT para las llamadas subsecuentes. 
        /// Hacer una llamada al endpoint de refresh. La respuesta a esta llamada obtendrá un JWT válido para los siguientes 60 minutos.
        /// </summary>
        /// <returns>Retorna un Objeto de Tipo GetRefreshResponseDto</returns>
       
        [HttpGet]
        public async Task<GetRefreshResponseDto> DoRefreshToken()
        {
            try
            {
                var getToken = new Token();
                getToken.GetToken(out string _token);

                using var client = new HttpClient();
                if (!string.IsNullOrEmpty(_baseUrl) && Uri.IsWellFormedUriString(_baseUrl, UriKind.RelativeOrAbsolute))
                {
                    client.BaseAddress = new Uri(_baseUrl);
                }
                client.DefaultRequestHeaders.Add("refresh-token", _token);

                using HttpResponseMessage response = await client.GetAsync(client.BaseAddress + "/users/refresh");
                var responseContent = response.Content.ReadAsStringAsync().Result;

                var deserialized = JsonConvert.DeserializeObject<GetRefreshResponseDto>(responseContent);

                if (response.IsSuccessStatusCode && deserialized is not null)
                {
                    _refreshResponseDto = deserialized;
                    return deserialized;
                }
                else if (response.StatusCode.ToString() == "Forbidden" || (int)response.StatusCode == 403)
                {
                    _refreshResponseDto.Status = response.StatusCode.ToString();
                    _refreshResponseDto.Message = "No hay acceso desde Servidor Local a Servidor Externo.";
                    return _refreshResponseDto;
                }
                _refreshResponseDto.Status = response.StatusCode.ToString();
                _refreshResponseDto.Message = "La consulta no devolvio una Respuesta";
                return _refreshResponseDto;
            }
            catch (Exception ex)
            {
                _refreshResponseDto.Status = HttpStatusCode.NotFound.ToString();
                _refreshResponseDto.Message = ex.Message;
                return _refreshResponseDto;
            }
        }
    }
}
