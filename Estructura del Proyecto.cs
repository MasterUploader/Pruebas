Actualiza estos métodos y controladores, para que usen RestUtilities.QueryBuilder:

using Microsoft.AspNetCore.Mvc;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.Auth;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.AuthService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.MachineInformationService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Controllers;

/// <summary>
/// Controlador AuthController, es el que contiene los Endpoints necesarios para el correcto funcionamiento de los servicios de autenticación del Sitio de embosado.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService _authService, ISessionManagerService _sessionManagerService, IMachineInfoService _machineInfoService) : ControllerBase
{
    /// <summary>
    /// Objeto DTO de Respuesta para autenticación.
    /// </summary>
    protected GetAuthResponseDto _getAuthResponseDto = new();
    private readonly ResponseHandler _responseHandler = new();


    /// <summary>
    /// Método que se encarga de realizar el proceso de logueo para los usuarios del sitio de embosado de tarjetas de Debito.
    /// </summary>
    /// <param name="loginDto"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("Login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        _getAuthResponseDto = await _authService.AuthenticateAsync(loginDto);

        return _responseHandler.HandleResponse(_getAuthResponseDto, _getAuthResponseDto.Codigo.Status);
    }

    /// <summary>
    /// Clase que se encarga de actualizar el estado de la sesión del usuario, agrega 15 minutos más al tiempo de sesión.
    /// </summary>
    /// <param name="userName"></param>
    /// <returns></returns>
    [HttpPost]
    [Route("Heartbeat")]
    public async Task<IActionResult> HeartBeat([FromBody] string userName)
    {
        HeartbeatService _heartbeatService = new(_sessionManagerService, _machineInfoService);
        // _getAuthResponseDto = await _heartbeatService.HeartbeatAsync(userName)

        // return _responseHandler.HandleResponse(_getAuthResponseDto, _getAuthResponseDto.Codigo.Status)

        return Ok();
    }
}


using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.Auth;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.AuthService;

/// <summary>
/// Interfaz IAuthService, de la clase de Servicio AuthService.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Método AuthenticateAsync, encargado del proceso de Logueo de un usuario.
    /// Válida el usuario y contraseña contra el AD, devolviendo los parametros de AD.
    /// Si los párametros coinciden con los autorizados se procede con el logueo.
    /// </summary>
    /// <param name="getAuthDto">Objeto de tipo LoginDto.</param>
    /// <returns>Retorna respuesta HTTP con objeto GetAuthResponseDto.</returns>
    Task<GetAuthResponseDto> AuthenticateAsync(LoginDto getAuthDto);
}


using API_1_TERCEROS_REMESADORAS.Utilities;
using Connections.Abstractions;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.Auth;
using MS_BAN_43_Embosado_Tarjetas_Debito.Repository.IRepository.Auth;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.MachineInformationService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;
using SUNITP.LIB.ActiveDirectoryV2;
using System.Data;
using System.Data.Common;
using System.Data.OleDb;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.AuthService;

/// <summary>
/// Clase de servicio AuthService, encargada del proceso de autenticación de usuarios para el sitio de embosado de tarjetas de debito.
/// </summary>
/// <param name="_machineInfoService">Instancia de IMachineInfoService.</param>
/// <param name="_sessionManagerService">Instancia de ISessionManagerService.</param>
/// <param name="_connection">Instancia de IDatabaseConnection.</param>
/// <param name="_contextAccessor">Instancia de IHttpContextAccessor.</param>
public class AuthService(IMachineInfoService _machineInfoService, ISessionManagerService _sessionManagerService, IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : IAuthService
{
    /// <inheritdoc />
    protected ActiveDirectoryHN _activeDirectoryHN = new();
    /// <inheritdoc />
    protected GetAuthResponseDto _getAuthResponseDto = new();

    /// <summary>
    /// Método de Autenticación asincrono.
    /// </summary>
    /// <param name="getAuthDto">Objeto Dto que se recibe para realizar el logueo.</param>
    /// <returns>Retorna un objeto GetauthResponseDto.</returns>
    public async Task<GetAuthResponseDto> AuthenticateAsync(LoginDto getAuthDto)
    {

        try
        {
            _connection.Open();

            AuthServiceRepository _authServiceRepository = new(_machineInfoService, _connection, _contextAccessor);

            bool estado = _activeDirectoryHN.GetActiveDirectory(out string domain, out string activeDirectory); //Valida si existe domino y AD en el archivo ConnectionData.json
            if (!estado) return _authServiceRepository.RegistraLogsUsuario(_getAuthResponseDto, getAuthDto.User, "No se encontro el Dominio de Red o Active Directory", "1", "vacio", "Unauthorized");
            var auth = new ServiceActiveDirectoryV2();
            var autenticateUser = auth.AutenticateUser(domain, getAuthDto.User, getAuthDto.Password, "");

            if (!autenticateUser.IAuthenticationAuthorized) return _authServiceRepository.RegistraLogsUsuario(_getAuthResponseDto, getAuthDto.User, "Credenciales Inválidas", "1", "vacio", "Unauthorized");

            bool bandera = false;
            foreach (var _ in from string role in autenticateUser.UserRoles
                              where role.Equals(activeDirectory)
                              select new { })
            {
                bandera = true;
            }

            if (!bandera) return _authServiceRepository.RegistraLogsUsuario(_getAuthResponseDto, getAuthDto.User, "Usuario no pertenece al Grupo AD", "1", "vacio", "Unauthorized");

            await Task.Delay(500);
            var token = _sessionManagerService.GenerateTokenAsync(getAuthDto);

            _getAuthResponseDto.Token.Token = token.Result.Item2;
            _getAuthResponseDto.Token.Expiration = token.Result.Item1.ValidTo;

            //Datos Usuario
            _getAuthResponseDto.ActiveDirectoryData.NombreUsuario = autenticateUser.UserName;
            _getAuthResponseDto.ActiveDirectoryData.UsuarioICBS = TraeUsuarioICBS(getAuthDto.User);

            foreach (var value in from propiedades in autenticateUser.UserProperties
                                  where propiedades.propertyName.Equals("departmentNumber")
                                  from value in propiedades.propertyValues
                                  select value)
            {
                _getAuthResponseDto.ActiveDirectoryData.AgenciaAperturaCodigo = value;
                _getAuthResponseDto.ActiveDirectoryData.AgenciaImprimeCodigo = value;
            }

            return _authServiceRepository.RegistraLogsUsuario(_getAuthResponseDto, getAuthDto.User, "Logueado Exitosamente", "0", token.Result.Item2, "success");
        }
        catch (Exception ex)
        {
            _getAuthResponseDto.Codigo.Message = ex.Message;
            _getAuthResponseDto.Codigo.Error = "400";
            _getAuthResponseDto.Codigo.Status = "BadRequest";
            _getAuthResponseDto.Codigo.TimeStamp = string.Format("{0:HH:mm:ss tt}", DateTime.Now);

            return _getAuthResponseDto;
        }
    }

    private string TraeUsuarioICBS(string usuarioRed)
    {
        FieldsQueryL param = new();
        string usuarioICBS = "";
        usuarioRed = "HN" + usuarioRed;

        string sqlQuery = "SELECT * FROM ICBSSMSSPN.SCP001 WHERE SCJBNA = ?";
        using var command = _connection.GetDbCommand(_contextAccessor.HttpContext!);
        command.CommandText = sqlQuery;
        command.CommandType = System.Data.CommandType.Text;

        param.AddOleDbParameter(command, "SCJBNA", OleDbType.Char, usuarioRed);

        using DbDataReader reader = command.ExecuteReader();

        if (reader.HasRows)
        {
            while (reader.Read())
            {
                usuarioICBS = reader.GetString(reader.GetOrdinal("SCUSER"));
            }
        }

        reader.Close();

        return usuarioICBS;
    }
}
