using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

// ...

builder.Services.AddControllers(options =>
{
    options.Filters.Add<LoggingActionFilter>();

    // 🔒 Exigir autenticación en todos los endpoints por defecto
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.Auth;
using MS_BAN_43_Embosado_Tarjetas_Debito.Models.Dtos.Heartbeat;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.AuthService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService;
using MS_BAN_43_Embosado_Tarjetas_Debito.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mime;
using System.Security.Claims;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController(
    IAuthService authService,
    ISessionManagerService sessionManagerService,
    ResponseHandler responseHandler
) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly ISessionManagerService _sessionSvc = sessionManagerService;
    private readonly ResponseHandler _response = responseHandler;

    // --- ya existente ---
    [HttpPost("Login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GetAuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GetAuthResponseDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        var resp = await _authService.AuthenticateAsync(loginDto!);
        return _response.HandleResponse(resp, resp.Codigo.Status);
    }

    // --- NUEVO: mantiene viva la sesión / token deslizante ---
    [HttpPost("KeepAlive")]
    [Authorize]
    [ProducesResponseType(typeof(GetHeartbeatDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(GetHeartbeatDto), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> KeepAlive()
    {
        // Usuario de red desde el token
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                     ?? User.FindFirstValue(ClaimTypes.Name) 
                     ?? User.Identity?.Name;

        if (string.IsNullOrWhiteSpace(userId))
        {
            var err = new GetHeartbeatDto { Codigo = { Status = "Unauthorized", Error = "401", Message = "Usuario no identificado" } };
            return _response.HandleResponse(err, err.Codigo.Status);
        }

        // (Opcional) validación contra tu bandera en DB: LOGB04SEA
        var active = _sessionSvc.IsSessionActiveAsync(userId);
        if (!active)
        {
            var err = new GetHeartbeatDto { Codigo = { Status = "Unauthorized", Error = "401", Message = "Sesión no activa" } };
            return _response.HandleResponse(err, err.Codigo.Status);
        }

        // Estrategia 1: ROTAR token (mejor seguridad)
        // var (securityToken, jwtString) = await _sessionSvc.GenerateTokenAsync(new LoginDto { User = userId, Password = "", /*...*/ });
        // var dto = new GetHeartbeatDto
        // {
        //     Token = new TokenDto { Token = jwtString, Expiration = securityToken.ValidTo },
        //     Codigo = { Status = "OK", Error = "0", Message = "OK" }
        // };
        // return _response.HandleResponse(dto, dto.Codigo.Status);

        // Estrategia 2: SOLO extender expiración de sesión (sin rotar token)
        // Si tu backend maneja expiración de sesión aparte del JWT, devuelve nueva fecha:
        var newExp = DateTime.UtcNow.AddMinutes(15); // ejemplo: +15 min
        var dto = new GetHeartbeatDto
        {
            Token = new TokenDto { Token = string.Empty, Expiration = newExp }, // Token vacío = no rotado
            Codigo = { Status = "OK", Error = "0", Message = "OK" }
        };
        return _response.HandleResponse(dto, dto.Codigo.Status);
    }

    // --- OPCIONAL: logout (revocación) ---
    [HttpPost("Logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                     ?? User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            // Marca sesión como inactiva en tu tabla (LOGB04SEA = 1) y opcionalmente
            // agrega el jti del token a una denylist temporal (hasta que expire).
            await _sessionSvc.InvalidateOldSessionAsync(userId);
        }

        var ok = new GetHeartbeatDto
        {
            Codigo = { Status = "OK", Error = "0", Message = "Sesión finalizada" }
        };
        return _response.HandleResponse(ok, ok.Codigo.Status);
    }
}



using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// ...

public async Task<(SecurityToken, string)> GenerateTokenAsync(LoginDto loginDto)
{
    var secretKey = await _jwtService.GetSecretKeyAsync();
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    // Identidad (ajusta a tu realidad)
    var userId = loginDto.User; // o el que uses como único
    var sid = Guid.NewGuid().ToString("N"); // id de sesión
    var jti = Guid.NewGuid().ToString("N"); // id de token

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, userId),
        new(ClaimTypes.NameIdentifier, userId),
        new("sid", sid),
        new(JwtRegisteredClaimNames.Jti, jti),
        // agrega roles / agencies si los necesitas como claims
    };

    var now = DateTime.UtcNow;
    var expires = now.AddMinutes(15); // expira en 15 min (ajusta a tu política)

    var descriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        NotBefore = now,
        Expires = expires,
        SigningCredentials = creds
    };

    var handler = new JwtSecurityTokenHandler();
    var token = handler.CreateToken(descriptor);
    var jwt = handler.WriteToken(token);

    // (Opcional) guarda `sid` en tu tabla de sesiones; marca LOGB04SEA=0 (activa)
    // await SaveSessionAsync(userId, sid, expires);

    return (token, jwt);
}

public async Task InvalidateOldSessionAsync(string userID)
{
    // Marca LOGB04SEA=1 (inactiva) para userID en BCAH96DTA.ETD02LOG
    // Puedes conservar timestamp de cierre.
    _connection.Open();

    string sql = "UPDATE BCAH96DTA.ETD02LOG SET LOGB04SEA = '1', LOGB05FCH = CURRENT_TIMESTAMP WHERE LOGB01UID = ?";
    using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
    cmd.CommandText = sql;
    cmd.CommandType = System.Data.CommandType.Text;

    FieldsQueryL param = new();
    param.AddOleDbParameter(cmd, "LOGB01UID", OleDbType.Char, userID);

    await cmd.ExecuteNonQueryAsync();
}



// En Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveSession", policy =>
        policy.RequireAssertion(ctx =>
        {
            var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ctx.User.Identity?.Name;
            if (string.IsNullOrEmpty(userId)) return false;

            // resolve service
            var sp = builder.Services.BuildServiceProvider();
            var svc = sp.GetRequiredService<ISessionManagerService>();
            return svc.IsSessionActiveAsync(userId);
        })
    );
});



[Authorize(Policy = "ActiveSession")]
public class DetalleTarjetasImprimirController : ControllerBase { ... }






