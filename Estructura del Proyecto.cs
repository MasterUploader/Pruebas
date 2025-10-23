// ====== AUTORIZACIÓN Y POLÍTICAS PERSONALIZADAS ======
builder.Services.AddAuthorization(options =>
{
    // Política para validar sesión activa en AS400
    options.AddPolicy("ActiveSession", policy =>
        policy.RequireAssertion(context =>
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? context.User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userId))
                return false;

            // ⚠️ No se debe crear un ServiceProvider nuevo aquí (mala práctica)
            // Por lo tanto, simplemente devolveremos true, y la validación real
            // se hará con un AuthorizationHandler registrado abajo.
            return true;
        })
    );
});

// 🔧 Registrar un AuthorizationHandler para hacer la validación real con inyección
builder.Services.AddSingleton<IAuthorizationHandler, ActiveSessionHandler>();




using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MS_BAN_43_Embosado_Tarjetas_Debito.Services.SessionManagerService;

namespace MS_BAN_43_Embosado_Tarjetas_Debito.Authorization
{
    /// <summary>
    /// Handler que valida si la sesión del usuario sigue activa en AS400.
    /// Usa ISessionManagerService.IsSessionActiveAsync(userId).
    /// </summary>
    public class ActiveSessionHandler : AuthorizationHandler<ActiveSessionRequirement>
    {
        private readonly ISessionManagerService _sessionManager;

        public ActiveSessionHandler(ISessionManagerService sessionManager)
        {
            _sessionManager = sessionManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ActiveSessionRequirement requirement)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? context.User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(userId))
                return;

            bool isActive = await _sessionManager.IsSessionActiveAsync(userId);
            if (isActive)
                context.Succeed(requirement);
        }
    }

    /// <summary>
    /// Requisito vacío para la política "ActiveSession".
    /// </summary>
    public class ActiveSessionRequirement : IAuthorizationRequirement { }
}



builder.Services.AddControllers(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddRequirements(new ActiveSessionRequirement())
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
