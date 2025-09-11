using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.Auth;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.AuthService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.MachineInformationService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;
using System.Net.Mime;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Controllers
{
    /// <summary>
    /// Endpoints de autenticación para el sitio de embosado.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ISessionManagerService _sessionManagerService;
        private readonly IMachineInfoService _machineInfoService;
        private readonly ILogger<AuthController> _logger;
        private readonly ResponseHandler _responseHandler = new();

        public AuthController(
            IAuthService authService,
            ISessionManagerService sessionManagerService,
            IMachineInfoService machineInfoService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _sessionManagerService = sessionManagerService;
            _machineInfoService = machineInfoService;
            _logger = logger;
        }

        /// <summary>
        /// Realiza el proceso de logueo contra AD y registra actividad.
        /// </summary>
        [HttpPost("Login")]
        [ProducesResponseType(typeof(GetAuthResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(GetAuthResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                var dto = new GetAuthResponseDto
                {
                    Codigo =
                    {
                        Status = "BadRequest",
                        Error = "400",
                        Message = "Solicitud inválida.",
                        TimeStamp = DateTime.Now.ToString("HH:mm:ss tt")
                    }
                };
                return _responseHandler.HandleResponse(dto, dto.Codigo.Status);
            }

            try
            {
                _logger.LogInformation("Login solicitado para usuario {User}.", loginDto?.User);
                var response = await _authService.AuthenticateAsync(loginDto);
                return _responseHandler.HandleResponse(response, response.Codigo.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en Login para usuario {User}.", loginDto?.User);
                var dto = new GetAuthResponseDto
                {
                    Codigo =
                    {
                        Status = "BadRequest",
                        Error = "400",
                        Message = ex.Message,
                        TimeStamp = DateTime.Now.ToString("HH:mm:ss tt")
                    }
                };
                return _responseHandler.HandleResponse(dto, dto.Codigo.Status);
            }
        }

        /// <summary>
        /// Mantiene viva la sesión (extiende 15 minutos).
        /// </summary>
        [HttpPost("Heartbeat")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> HeartBeat([FromBody] string userName)
        {
            // Aquí puedes invocar tu servicio de sesión si lo deseas.
            // HeartbeatService hb = new(_sessionManagerService, _machineInfoService);
            // await hb.HeartbeatAsync(userName);

            return Ok();
        }
    }
}


using API_1_TERCEROS_REMESADORAS.Utilities;
using Connections.Abstractions;
using Microsoft.AspNetCore.Http;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.Auth;
using MS_BAN_43_Embosado_Tarjetas_Debito.Repository.IRepository.Auth;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.MachineInformationService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;
using QueryBuilder.Builders;
using QueryBuilder.Enums;
using QueryBuilder.Helpers;
using SUNITP.LIB.ActiveDirectoryV2;
using System.Data.Common;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Services.AuthService
{
    /// <summary>
    /// Servicio de autenticación de usuarios para embosado.
    /// </summary>
    /// <param name="_machineInfoService">Servicio de información de máquina.</param>
    /// <param name="_sessionManagerService">Servicio de sesiones/JWT.</param>
    /// <param name="_connection">Conexión a base de datos (AS400/OleDb).</param>
    /// <param name="_contextAccessor">Accessor de HttpContext.</param>
    public class AuthService(
        IMachineInfoService _machineInfoService,
        ISessionManagerService _sessionManagerService,
        IDatabaseConnection _connection,
        IHttpContextAccessor _contextAccessor) : IAuthService
    {
        protected ActiveDirectoryHN _activeDirectoryHN = new();
        protected GetAuthResponseDto _getAuthResponseDto = new();

        /// <inheritdoc />
        public async Task<GetAuthResponseDto> AuthenticateAsync(LoginDto getAuthDto)
        {
            try
            {
                _connection.Open();

                AuthServiceRepository _authServiceRepository = new(_machineInfoService, _connection, _contextAccessor);

                bool estado = _activeDirectoryHN.GetActiveDirectory(out string domain, out string activeDirectory);
                if (!estado)
                    return _authServiceRepository.RegistraLogsUsuario(_getAuthResponseDto, getAuthDto.User, "No se encontro el Dominio de Red o Active Directory", "1", "vacio", "Unauthorized");

                var auth = new ServiceActiveDirectoryV2();
                var autenticateUser = auth.AutenticateUser(domain, getAuthDto.User, getAuthDto.Password, "");
                if (!autenticateUser.IAuthenticationAuthorized)
                    return _authServiceRepository.RegistraLogsUsuario(_getAuthResponseDto, getAuthDto.User, "Credenciales Inválidas", "1", "vacio", "Unauthorized");

                bool bandera = autenticateUser.UserRoles?.Any(r => r.Equals(activeDirectory)) == true;
                if (!bandera)
                    return _authServiceRepository.RegistraLogsUsuario(_getAuthResponseDto, getAuthDto.User, "Usuario no pertenece al Grupo AD", "1", "vacio", "Unauthorized");

                // Genera token
                var token = await _sessionManagerService.GenerateTokenAsync(getAuthDto);
                _getAuthResponseDto.Token.Token = token.Item2;
                _getAuthResponseDto.Token.Expiration = token.Item1.ValidTo;

                // Datos usuario (AD)
                _getAuthResponseDto.ActiveDirectoryData.NombreUsuario = autenticateUser.UserName;
                _getAuthResponseDto.ActiveDirectoryData.UsuarioICBS = TraeUsuarioICBS(getAuthDto.User);

                // departmentNumber
                var department = autenticateUser.UserProperties?
                    .FirstOrDefault(p => p.propertyName.Equals("departmentNumber"))?
                    .propertyValues?.FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(department))
                {
                    _getAuthResponseDto.ActiveDirectoryData.AgenciaAperturaCodigo = department;
                    _getAuthResponseDto.ActiveDirectoryData.AgenciaImprimeCodigo = department;
                }

                return _authServiceRepository.RegistraLogsUsuario(_getAuthResponseDto, getAuthDto.User, "Logueado Exitosamente", "0", token.Item2, "success");
            }
            catch (Exception ex)
            {
                _getAuthResponseDto.Codigo.Message = ex.Message;
                _getAuthResponseDto.Codigo.Error = "400";
                _getAuthResponseDto.Codigo.Status = "BadRequest";
                _getAuthResponseDto.Codigo.TimeStamp = DateTime.Now.ToString("HH:mm:ss tt");
                return _getAuthResponseDto;
            }
        }

        /// <summary>
        /// Obtiene el usuario ICBS desde ICBSSMSSPN.SCP001 usando RestUtilities.QueryBuilder.
        /// </summary>
        private string TraeUsuarioICBS(string usuarioRed)
        {
            _connection.Open();
            if (!_connection.IsConnected) return string.Empty;

            // En tu lógica original: usuarioRed se normaliza a "HN" + usuarioRed
            usuarioRed = "HN" + (usuarioRed ?? string.Empty);

            // SELECT SCUSER FROM ICBSSMSSPN.SCP001 WHERE SCJBNA = :usuarioRed FETCH FIRST 1 ROW ONLY
            var qb = new SelectQueryBuilder("SCP001", "ICBSSMSSPN")
                .Select("SCUSER")
                .WhereRaw($"SCJBNA = {SqlHelper.FormatValue(usuarioRed)}")
                .Limit(1);

            var query = qb.Build();

            // Puedes usar la sobrecarga que añade parámetros automáticamente si usas placeholders:
            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = query.Sql;
            cmd.CommandType = System.Data.CommandType.Text;

            using DbDataReader reader = cmd.ExecuteReader();
            if (!reader.HasRows) return string.Empty;

            while (reader.Read())
            {
                // Devuelve el primer SCUSER encontrado
                var ordinal = reader.GetOrdinal("SCUSER");
                if (!reader.IsDBNull(ordinal))
                    return reader.GetString(ordinal);
            }

            return string.Empty;
        }
    }
}

