Algo esta mal, revisa el codigo, toma a consideración lo siguiente la tabla USUADMIN solo tiene las columnas USUARIO PASS TIPUSU ESTADO.


    using CAUAdministracion.Helpers;
using CAUAdministracion.Models;
using CAUAdministracion.Services.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList.Extensions;

namespace CAUAdministracion.Controllers;

[Authorize, AutorizarPorTipoUsuario("1")]
public class UsuariosController : Controller
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    // ========= AGREGAR =========

    [HttpGet]
    [AutorizarPorTipoUsuario("1")]
    public IActionResult Agregar()
    {
        return View(new UsuarioCreateViewModel());
    }

    [HttpPost]
    [AutorizarPorTipoUsuario("1")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Agregar(UsuarioCreateViewModel model)
    {
        // Validaciones de negocio ANTES del IsValid, para que influyan
        if (!string.IsNullOrWhiteSpace(model.Usuario))
        {

            // --- Validaciones básicas del lado servidor ---
            if (string.IsNullOrWhiteSpace(model.Usuario))
                ModelState.AddModelError(nameof(model.Usuario), "El usuario es obligatorio.");

            if (model.TipoUsuario < 1 || model.TipoUsuario > 3)
                ModelState.AddModelError(nameof(model.TipoUsuario), "Tipo de usuario inválido.");

            if (model.Estado != "A" && model.Estado != "I")
                ModelState.AddModelError(nameof(model.Estado), "Estado inválido.");

            if (string.IsNullOrWhiteSpace(model.Clave) || string.IsNullOrWhiteSpace(model.ConfirmarClave))
                ModelState.AddModelError(nameof(model.Clave), "Debe indicar la clave y su confirmación.");

            if (!string.Equals(model.Clave, model.ConfirmarClave))
                ModelState.AddModelError(nameof(model.ConfirmarClave), "Las claves no coinciden.");

            if (await _usuarioService.ExisteUsuarioAsync(model.Usuario.Trim()))
            {
                ModelState.AddModelError(nameof(model.Usuario), "El usuario ya existe.");
            }
        }

        // Si hay cualquier error de DataAnnotations o de negocio, será inválido
        if (!ModelState.IsValid)
            return View(model);

        var usuario = new UsuarioModel
        {
            Usuario = model.Usuario.Trim(),
            TipoUsuario = model.TipoUsuario,
            Estado = model.Estado
        };

        var creado = await _usuarioService.CrearUsuarioAsync(usuario, model.Clave);
        if (creado)
        {
            TempData["Mensaje"] = "Usuario creado correctamente.";
            return RedirectToAction("Index");
        }

        ModelState.AddModelError(string.Empty, "No se pudo crear el usuario.");
        return View(model);
    }


    // Listado + filtros + paginación + modo edición por fila
    [HttpGet]
    [AutorizarPorTipoUsuario("1")]
    public async Task<IActionResult> Index(int? page, string? q, int? tipo, string? estado, string? editUser)
    {
        var datos = await _usuarioService.BuscarUsuariosAsync(q, tipo, estado);
        var pageSize = 10;
        var pageNumber = page ?? 1;

        ViewBag.Q = q;
        ViewBag.TipoSel = tipo?.ToString();
        ViewBag.EstadoSel = estado;
        ViewBag.EditUser = editUser;

        return View(datos.ToPagedList(pageNumber, pageSize));
    }

    // Actualiza una fila (usuario, estado y tipo)
    //[HttpPost]
    //[AutorizarPorTipoUsuario("1")]
    //[ValidateAntiForgeryToken]
    //public async Task<IActionResult> Actualizar(string usuario, string estado, int tipoUsuario, string? q, int? tipo, string? filtroEstado, int? page)
    //{
    //    // Si intenta inactivar y es el único, bloquear
    //    if (string.Equals(estado, "I", StringComparison.OrdinalIgnoreCase)
    //        && !await _usuarioService.ExisteMasDeUnUsuarioAsync())
    //    {
    //        TempData["Mensaje"] = "No puede inactivar/eliminar al único usuario del sistema.";
    //        return RedirectToAction("Index", new { page, q, tipo, estado = filtroEstado });
    //    }

    //    var ok = await _usuarioService.ActualizarUsuarioAsync(usuario, estado, tipoUsuario);
    //    TempData["Mensaje"] = ok ? "Usuario actualizado." : "No se pudo actualizar el usuario.";

    //    return RedirectToAction("Index", new { page, q, tipo, estado = filtroEstado });
    //}

    // GET: /Usuarios/Actualizar/123
    [HttpGet]
    public IActionResult Actualizar(string usuario)
    {
        var model = _usuarioService.ObtenerPorId(usuario);
        if (model == null) return NotFound();
        return View(model); // Views/Usuarios/Actualizar.cshtml
    }

    // POST: /Usuarios/Actualizar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Actualizar(UsuarioEditModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var ok = await _usuarioService.Actualizar(model);
        TempData["Mensaje"] = ok ? "Usuario actualizado." : "No se pudo actualizar.";
        TempData["MensajeTipo"] = ok ? "success" : "danger";
        return RedirectToAction("Index");
    }


    // Elimina una fila
    [HttpPost]
    [AutorizarPorTipoUsuario("1")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(string usuario, string? q, int? tipo, string? estado, int? page)
    {
        var ok = await _usuarioService.EliminarUsuarioAsync(usuario);

        TempData["Mensaje"] = ok ? $"Usuario '{usuario}' eliminado." : $"No se pudo eliminar el usuario '{usuario}'.";
        TempData["MensajeTipo"] = ok ? "success" : "danger";

        return RedirectToAction("Index", new { page, q, tipo, estado });
    }

    // (Opcional) Navegar a “Crear”
    [HttpGet]
    [AutorizarPorTipoUsuario("1")]
    public IActionResult Crear() => View();

}




using CAUAdministracion.Helpers;
using CAUAdministracion.Models;
using Connections.Abstractions;
using Connections.Providers.Database;
using QueryBuilder.Builders;
using System.Data;
using System.Runtime.Versioning;

namespace CAUAdministracion.Services.Usuarios;

/// <summary>
/// Clase de Servicio UsuarioService, encargada de las operaciones CRUD para usuarios.
/// </summary>
/// <param name="as400">Instancia de conexión al As400.</param>
/// <param name="httpContextAccessor">Contex de la petición HTTP, información referente a la misma.</param>
public class UsuarioService(IDatabaseConnection as400, IHttpContextAccessor httpContextAccessor) : IUsuarioService
{
    private readonly IDatabaseConnection _as400 = as400;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    public async Task<List<UsuarioModel>> BuscarUsuariosAsync(string? q, int? tipo, string? estado)
    {
        var usuario = new List<UsuarioModel>();
        try
        {
            _as400.Open();

            if (!_as400.IsConnected)
                return usuario;

            //Traemos usuario
            var query = new SelectQueryBuilder("USUADMIN", "BCAH96DTA")
                .Select("USUARIO", "TIPUSU", "ESTADO");

            // Filtros
            if (!string.IsNullOrWhiteSpace(q))
                query.WhereRaw("UPPER(USUARIO) LIKE @q");

            if (tipo.HasValue)
                query.WhereRaw("TIPUSU = @tipo");

            if (!string.IsNullOrWhiteSpace(estado))
                query.WhereRaw("ESTADO = @estado");

            query.OrderBy("USUARIO");

            var result = query.Build();
            using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);

            command.CommandText = result.Sql;

            // Parámetros
            if (!string.IsNullOrWhiteSpace(q))
                AddParam(command, "@q", $"%{q.Trim().ToUpper()}%");
            if (tipo.HasValue)
                AddParam(command, "@tipo", tipo.Value);
            if (!string.IsNullOrWhiteSpace(estado))
                AddParam(command, "@estado", estado);

            using var rd = await command.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                usuario.Add(new UsuarioModel
                {
                    Usuario = rd["USUARIO"]?.ToString() ?? "",
                    TipoUsuario = Convert.ToInt32(rd["TIPUSU"]),
                    Estado = rd["ESTADO"]?.ToString() ?? "A"
                });
            }
            return usuario;
        }
        catch
        {
            return usuario;
        }
        finally
        {
            _as400.Close();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ActualizarUsuarioAsync(string usuario, string estado, int tipoUsuario)
    {
        try
        {
            _as400.Open();

            if (!_as400.IsConnected)
                return false;

            // UPDATE BCAH96DTA.USUADMIN SET ESTADO=@estado, TIPUSU=@tipo WHERE USUARIO=@usuario
            var query = new UpdateQueryBuilder("USUADMIN", "BCAH96DTA")
                .Set("ESTADO", "@estado")
                .Set("TIPUSU", "@tipo")
                .Where("USUARIO = @usuario");

            var result = query.Build();
            using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);

            command.CommandText = result.Sql;

            AddParam(command, "@estado", estado);
            AddParam(command, "@tipo", tipoUsuario);
            AddParam(command, "@usuario", usuario);

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            _as400.Close();
        }
    }

    /// <inheritdoc />
    public async Task<bool> EliminarUsuarioAsync(string usuario)
    {
        try
        {

            if (!await ExisteMasDeUnUsuarioAsync())
                return false;

            _as400.Open();

            if (!_as400.IsConnected)
                return false;

            using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);

            var query = new DeleteQueryBuilder("USUADMIN", "BCAH96DTA")
                .Where($"USUARIO = '{usuario}'")
                .Build();

            command.CommandText = query.Sql;

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            _as400.Close();
        }
    }

    /// <inheritdoc />
    public async Task<int> ContarUsuariosAsync()
    {
        try
        {
            _as400.Open();

            if (!_as400.IsConnected)
                return 0;

            // SELECT COUNT(*) FROM BCAH96DTA.USUADMIN
            var query = new SelectQueryBuilder("USUADMIN", "BCAH96DTA")
            .Select("COUNT(*) AS CNT")
            .Build();

            using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);
            command.CommandText = query.Sql;

            var scalar = await command.ExecuteScalarAsync();
            return Convert.ToInt32(scalar);
        }
        catch
        {
            return 0;
        }
        finally
        {
            _as400.Close();
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExisteMasDeUnUsuarioAsync()
    {
        var total = await ContarUsuariosAsync();
        return total > 1;
    }

    // --------------------------------------
    // Helpers
    // --------------------------------------
    private static void AddParam(IDbCommand cmd, string name, object? value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public async Task<bool> ExisteUsuarioAsync(string usuario)
    {
        if (string.IsNullOrWhiteSpace(usuario)) return false;

        // SELECT COUNT(1) FROM BCAH96DTA.USUADMIN WHERE UPPER(USUARIO)=UPPER(@usuario)
        try
        {
            _as400.Open();

            if (!_as400.IsConnected)
                return false;

            //Construimos el Query
            var query = QueryBuilder.Core.QueryBuilder
            .From("USUADMIN", "BCAH96DTA")
            .Select("*")
            .WhereRaw($"UPPER(USUARIO) = UPPER('{usuario}')")
            .Build();

            using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);
            command.CommandText = query.Sql;

            var count = Convert.ToInt32(await command.ExecuteScalarAsync());

            if (count > 0)
            {
                return true;
            }
            return false;
        }
        catch
        {
            return true;
        }
        finally
        {
            _as400.Close();
        }
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public async Task<bool> CrearUsuarioAsync(UsuarioModel usuario, string clavePlano)
    {
        if (usuario == null) throw new ArgumentNullException(nameof(usuario));
        if (string.IsNullOrWhiteSpace(usuario.Usuario)) throw new ArgumentException("Usuario requerido.", nameof(usuario));
        if (usuario.TipoUsuario is < 1 or > 3) throw new ArgumentException("Tipo de usuario inválido.", nameof(usuario));
        if (usuario.Estado is not ("A" or "I")) throw new ArgumentException("Estado inválido.", nameof(usuario));
        if (string.IsNullOrWhiteSpace(clavePlano)) throw new ArgumentException("Clave requerida.", nameof(clavePlano));

        // ====> Cifrado con tu clase OperacionesVarias (Legacy por defecto o AES si config lo indica)
        var claveCifrada = OperacionesVarias.EncriptarCadena(clavePlano);

        try
        {
            _as400.Open();

            if (!_as400.IsConnected)
                return false;

            //Construimos el Query
            var query = new InsertQueryBuilder("USUADMIN", "BCAH96DTA")
                .Values(
                        ("USUARIO", usuario.Usuario),
                        ("PASS", claveCifrada),
                        ("TIPUSU", usuario.TipoUsuario),
                        ("ESTADO", usuario.Estado)
                        )
                .Build();

            using var command = ((AS400ConnectionProvider)_as400).GetDbCommand(query, _httpContextAccessor.HttpContext!);

            int filas = await command.ExecuteNonQueryAsync();

            return filas > 0;
        }
        catch
        {
            return false;
        }
        finally
        {
            _as400.Close();
        }
    }

    // ===================== OBTENER POR ID =====================
    public UsuarioEditModel? ObtenerPorId(string usuario)
    {
        try
        {
            _as400.Open();
            if (!_as400.IsConnected) return null;

            using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);

            // Ajusta nombres de tabla/campos a tu esquema real (USUADMIN/BCAH96DTA)
            var query = QueryBuilder.Core.QueryBuilder
                .From("USUADMIN", "BCAH96DTA")
                .Select("NOMBRE", "TIPOUSU", "CODCCO", "ESTADO")
                .Where<USUADMIN>(x => x.USUARIO == $"'{usuario}'")   // si tu entidad de modelo para Where se llama distinto, cámbiala
                .Build();

            command.CommandText = query.Sql;
            if (command.Connection?.State == System.Data.ConnectionState.Closed)
                command.Connection.Open();

            using var reader = command.ExecuteReader();
            if (!reader.Read()) return null;

            var model = new UsuarioEditModel
            {
                Id = Convert.ToInt32(reader["CODUSU"]),
                Usuario = reader["USUARIO"]?.ToString() ?? "",
                Nombre = reader["NOMBRE"]?.ToString() ?? "",
                TipoUsuario = Convert.ToInt32(reader["TIPOUSU"]),
                Codcco = reader["CODCCO"]?.ToString() ?? "",
                Estado = reader["ESTADO"]?.ToString() ?? "A"
            };

            return model;
        }
        catch
        {
            return null;
        }
        finally
        {
            _as400.Close();
        }
    }

    // ===================== ACTUALIZAR (usa tu ActualizarUsuarioAsync) =====================
    public async Task<bool> Actualizar(UsuarioEditModel model)
    {
        // Validación mínima
        if (model is null || model.Id <= 0) return false;

        // Aquí reutilizamos tu método existente. Como el tuyo no acepta modelo,
        // adaptamos los parámetros desde model. Si tu firma real difiere,
        // mapea en consecuencia:
        //
        // Ejemplo supuesto:
        // Task<bool> ActualizarUsuarioAsync(int id, string usuario, string nombre, string tipoUsuario, string codcco, string estado)
        //
        return await ActualizarUsuarioAsync(
            model.Usuario,
            model.Estado,
            model.TipoUsuario
        );
    }
}



using CAUAdministracion.Models;

namespace CAUAdministracion.Services.Usuarios;

/// <summary>
/// Interfaz de la Clase de servicio UsuarioService
/// </summary>
public interface IUsuarioService
{
    /// <summary>
    /// Método encargado de buscar usuarios para el filtro.
    /// </summary>
    /// <param name="q"></param>
    /// <param name="tipo"></param>
    /// <param name="estado"></param>
    /// <returns>Retorna una lista UsuarioModel</returns>
    Task<List<UsuarioModel>> BuscarUsuariosAsync(string? q, int? tipo, string? estado);

    /// <summary>
    /// Actualiza la información de un usuario.
    /// </summary>
    /// <param name="usuario"></param>
    /// <param name="estado"></param>
    /// <param name="tipoUsuario"></param>
    /// <returns>Retorna un valor boleano correspondiente al exito o no del proceso de actualización de usuarios.</returns>
    Task<bool> ActualizarUsuarioAsync(string usuario, string estado, int tipoUsuario);

    /// <summary>
    /// Método Que elimina un usuario del sitio web.
    /// </summary>
    /// <param name="usuario"></param>
    /// <returns>Retorna un valor boleano correspondiente al exito o no del proceso de eliminación de usuarios.</returns>
    Task<bool> EliminarUsuarioAsync(string usuario);

    /// <summary>
    /// Método que se encarga de contar si hay usuarios con ese nombre en tabla.
    /// </summary>
    /// <returns>Retorna un entero con la cantidad total, si no hay usuarios retorna 0.</returns>
    Task<int> ContarUsuariosAsync();

    /// <summary>
    /// Método que valida si existe más de un usuario en tabla.
    /// </summary>
    /// <returns></returns>
    Task<bool> ExisteMasDeUnUsuarioAsync();

    /// <summary>
    /// Verifica si ya existe un usuario (insensible a mayúsculas/minúsculas).
    /// </summary>
    /// <param name="usuario">Nombre de usuario a verificar.</param>
    /// <returns>true si existe; false en caso contrario.</returns>
    Task<bool> ExisteUsuarioAsync(string usuario);

    /// <summary>
    /// Crea un usuario nuevo en USUADMIN.
    /// </summary>
    /// <param name="usuario">Objeto con Usuario, TipoUsu y Estado.</param>
    /// <param name="clavePlano">Clave en texto plano (se cifra en el servicio).</param>
    /// <returns>true si se insertó; false si no.</returns>
    Task<bool> CrearUsuarioAsync(UsuarioModel usuario, string clavePlano);

    /// <summary>Obtiene un usuario por Id (CODUSU, por ejemplo).</summary>
    UsuarioEditModel? ObtenerPorId(string usuario);

    /// <summary>
    /// Actualiza un usuario usando el modelo de edición.
    /// Reutiliza la lógica existente de ActualizarUsuarioAsync internamente.
    /// </summary>
    Task<bool> Actualizar(UsuarioEditModel model);
}


@using X.PagedList
@using X.PagedList.Mvc.Core
@model IPagedList<object>  
@* Usa object para evitar errores de tipo si tu UsuarioModel no coincide *@

@{
    ViewData["Title"] = "Administración de Usuarios";

    // Filtros que envía el controlador en ViewBag
    var q         = ViewBag.Q as string;
    var tipoSel   = ViewBag.TipoSel?.ToString();
    var estadoSel = ViewBag.EstadoSel?.ToString();
}

<h2 class="text-danger">@ViewData["Title"]</h2>

@if (TempData["Mensaje"] != null)
{
    <div id="autoclose-alert" class="alert alert-info alert-dismissible fade show" role="alert">
        @TempData["Mensaje"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<form method="get" asp-controller="Usuarios" asp-action="Index" class="row g-3 mb-3">
    <div class="col-md-4">
        <label class="form-label">Usuario</label>
        <input type="text" name="q" value="@(q ?? "")" class="form-control" placeholder="Buscar por usuario..." />
    </div>

    <div class="col-md-3">
        <label class="form-label">Tipo</label>
        <select name="tipo" class="form-select">
            <option value="">-- Todos --</option>
            @* Sin C# en atributos; usamos if/else para “selected” *@
            @if (tipoSel == "1")
            { <option value="1" selected>Administrador</option> }
            else
            { <option value="1">Administrador</option> }

            @if (tipoSel == "2")
            { <option value="2" selected>Admin. Videos</option> }
            else
            { <option value="2">Admin. Videos</option> }

            @if (tipoSel == "3")
            { <option value="3" selected>Admin. Mensajes</option> }
            else
            { <option value="3">Admin. Mensajes</option> }
        </select>
    </div>

    <div class="col-md-3">
        <label class="form-label">Estado</label>
        <select name="estado" class="form-select">
            <option value="">-- Todos --</option>
            @if (estadoSel == "A")
            { <option value="A" selected>Activo</option> }
            else
            { <option value="A">Activo</option> }

            @if (estadoSel == "I")
            { <option value="I" selected>Inactivo</option> }
            else
            { <option value="I">Inactivo</option> }
        </select>
    </div>

    <div class="col-md-2 d-grid">
        <label class="form-label d-none d-md-block">&nbsp;</label>
        <button type="submit" class="btn btn-primary">Filtrar</button>
    </div>
</form>

<div class="mb-3">
    <a asp-controller="Usuarios" asp-action="Agregar" class="btn btn-success">Agregar nuevo usuario</a>
</div>

@if (Model != null && Model.Any())
{
    <table class="table table-bordered table-striped align-middle">
        <thead class="table-dark">
        <tr>
            <th>Usuario</th>
            <th>Tipo</th>
            <th>Estado</th>
            <th style="width:160px">Acciones</th>
        </tr>
        </thead>
        <tbody>
        @foreach (var u in Model)
        {
            // ==== Lectura segura de propiedades ====
            var t = u.GetType();

            string usuario  = t.GetProperty("Usuario")?.GetValue(u)?.ToString() ?? "";
            int    tipoInt  = 0;
            var    pTipo    = t.GetProperty("TipoUsu") ?? t.GetProperty("TipUsu") ?? t.GetProperty("TipoUsuario");
            if (pTipo != null)
            {
                var tmp = pTipo.GetValue(u);
                if (tmp != null && int.TryParse(tmp.ToString(), out var v)) tipoInt = v;
            }

            string estado   = t.GetProperty("Estado")?.GetValue(u)?.ToString() ?? "";

            string tipoTxt = tipoInt switch
            {
                1 => "Administrador",
                2 => "Admin. Videos",
                3 => "Admin. Mensajes",
                _ => $"Tipo {tipoInt}"
            };
            string estadoTxt = (estado == "A") ? "Activo" : "Inactivo";
            // ==========================================================================

            <tr>
                <td>@usuario</td>
                <td>@tipoTxt</td>
                <td>@estadoTxt</td>
                <td class="text-nowrap">
                       <a asp-controller="Usuarios" asp-action="Actualizar" asp-route-id="@usuario"  class="btn btn-sm btn-warning me-2">Editar</a>

                    <form asp-controller="Usuarios"
                          asp-action="Eliminar"
                          asp-route-usuario="@usuario"
                          method="post"
                          class="d-inline"
                          onsubmit="return confirm('¿Eliminar el usuario @usuario?');">
                        @Html.AntiForgeryToken()
                        <button type="submit" class="btn btn-sm btn-danger">Eliminar</button>
                    </form>
                </td>
            </tr>
        }
        </tbody>
    </table>

    <div class="d-flex justify-content-center">
        @Html.PagedListPager(
            Model,
            page => Url.Action("Index", new { page, q, tipo = tipoSel, estado = estadoSel }),
            new PagedListRenderOptions {
                UlElementClasses = new[] { "pagination", "justify-content-center" },
                LiElementClasses = new[] { "page-item" },
                PageClasses      = new[] { "page-link" },
                DisplayLinkToFirstPage   = PagedListDisplayMode.Always,
                DisplayLinkToLastPage    = PagedListDisplayMode.Always,
                DisplayLinkToPreviousPage= PagedListDisplayMode.Always,
                DisplayLinkToNextPage    = PagedListDisplayMode.Always,
                MaximumPageNumbersToDisplay = 7
            })
    </div>
}
else
{
    <div class="alert alert-info">No se encontraron usuarios con los criterios seleccionados.</div>
}

@section Scripts{
<script>
    // Cierra alerts en 5s
    setTimeout(function(){
        var el = document.getElementById('autoclose-alert');
        if (el) { var alert = bootstrap.Alert.getOrCreateInstance(el); alert.close(); }
    }, 5000);
</script>
    }

