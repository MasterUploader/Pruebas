using System.ComponentModel.DataAnnotations;

namespace CAUAdministracion.Models
{
    public class UsuarioModel
    {
        [Required, MaxLength(32)]
        public string Usuario { get; set; } = string.Empty;

        /// <summary>1 = Administrador, 2 = Admin. Videos, 3 = Admin. Mensajes</summary>
        [Range(1, 3)]
        public int TipoUsuario { get; set; }

        /// <summary>A = Activo, I = Inactivo</summary>
        [Required, RegularExpression("A|I")]
        public string Estado { get; set; } = "A";
    }
}


using CAUAdministracion.Models;

namespace CAUAdministracion.Services.Usuarios
{
    public interface IUsuarioService
    {
        Task<List<UsuarioModel>> BuscarUsuariosAsync(string? q, int? tipo, string? estado);
        Task<bool> ActualizarUsuarioAsync(string usuario, string estado, int tipoUsuario);
        Task<bool> EliminarUsuarioAsync(string usuario);
        Task<int> ContarUsuariosAsync();
        Task<bool> ExisteMasDeUnUsuarioAsync();
    }
}


using System.Data;
using CAUAdministracion.Models;
using RestUtilities.Connections;
using RestUtilities.QueryBuilder;

namespace CAUAdministracion.Services.Usuarios
{
    public class UsuarioService : IUsuarioService
    {
        private readonly string _connName;

        /// <param name="connName">
        /// Nombre de conexión registrado en RestUtilities.Connections (por ejemplo "AS400").
        /// </param>
        public UsuarioService(string connName = "AS400")
        {
            _connName = connName;
        }

        public async Task<List<UsuarioModel>> BuscarUsuariosAsync(string? q, int? tipo, string? estado)
        {
            // SELECT USUARIO, TIPUSU, ESTADO FROM BCAH96DTA.USUADMIN WHERE ... ORDER BY USUARIO
            var qb = new SqlBuilder()
                .Select("USUARIO", "TIPUSU", "ESTADO")
                .From("BCAH96DTA.USUADMIN");

            // Filtros
            if (!string.IsNullOrWhiteSpace(q))
                qb.Where("UPPER(USUARIO) LIKE @q");

            if (tipo.HasValue)
                qb.Where("TIPUSU = @tipo");

            if (!string.IsNullOrWhiteSpace(estado))
                qb.Where("ESTADO = @estado");

            qb.OrderBy("USUARIO");

            using var conn = ConnectionFactory.GetOpenConnection(_connName);
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = qb.ToSql();

            // Parámetros
            if (!string.IsNullOrWhiteSpace(q))
                AddParam(cmd, "@q", $"%{q.Trim().ToUpper()}%");
            if (tipo.HasValue)
                AddParam(cmd, "@tipo", tipo.Value);
            if (!string.IsNullOrWhiteSpace(estado))
                AddParam(cmd, "@estado", estado);

            var list = new List<UsuarioModel>();
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new UsuarioModel
                {
                    Usuario     = rd["USUARIO"]?.ToString() ?? "",
                    TipoUsuario = Convert.ToInt32(rd["TIPUSU"]),
                    Estado      = rd["ESTADO"]?.ToString() ?? "A"
                });
            }
            return list;
        }

        public async Task<bool> ActualizarUsuarioAsync(string usuario, string estado, int tipoUsuario)
        {
            // UPDATE BCAH96DTA.USUADMIN SET ESTADO=@estado, TIPUSU=@tipo WHERE USUARIO=@usuario
            var qb = new SqlBuilder()
                .Update("BCAH96DTA.USUADMIN")
                .Set("ESTADO", "@estado")
                .Set("TIPUSU", "@tipo")
                .Where("USUARIO = @usuario");

            using var conn = ConnectionFactory.GetOpenConnection(_connName);
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = qb.ToSql();
            AddParam(cmd, "@estado", estado);
            AddParam(cmd, "@tipo",   tipoUsuario);
            AddParam(cmd, "@usuario", usuario);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<bool> EliminarUsuarioAsync(string usuario)
        {
            // Validación: no eliminar si es el único usuario del sistema
            if (!await ExisteMasDeUnUsuarioAsync())
                return false;

            var qb = new SqlBuilder()
                .DeleteFrom("BCAH96DTA.USUADMIN")
                .Where("USUARIO = @usuario");

            using var conn = ConnectionFactory.GetOpenConnection(_connName);
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = qb.ToSql();
            AddParam(cmd, "@usuario", usuario);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<int> ContarUsuariosAsync()
        {
            // SELECT COUNT(*) FROM BCAH96DTA.USUADMIN
            var qb = new SqlBuilder()
                .Select("COUNT(*) AS CNT")
                .From("BCAH96DTA.USUADMIN");

            using var conn = ConnectionFactory.GetOpenConnection(_connName);
            using var cmd  = conn.CreateCommand();
            cmd.CommandText = qb.ToSql();

            var scalar = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(scalar);
        }

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
    }
}

using CAUAdministracion.Models;
using CAUAdministracion.Services.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using X.PagedList;

namespace CAUAdministracion.Controllers
{
    [Authorize]
    public class UsuariosController : Controller
    {
        private readonly IUsuarioService _svc;

        public UsuariosController(IUsuarioService svc)
        {
            _svc = svc;
        }

        // Listado + filtros + paginación + modo edición por fila
        [HttpGet]
        public async Task<IActionResult> Index(int? page, string? q, int? tipo, string? estado, string? editUser)
        {
            var datos = await _svc.BuscarUsuariosAsync(q, tipo, estado);
            var pageSize = 10;
            var pageNumber = page ?? 1;

            ViewBag.Q        = q;
            ViewBag.TipoSel  = tipo?.ToString();
            ViewBag.EstadoSel= estado;
            ViewBag.EditUser = editUser;

            return View(datos.ToPagedList(pageNumber, pageSize));
        }

        // Actualiza una fila (usuario, estado y tipo)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Actualizar(string usuario, string estado, int tipoUsuario, string? q, int? tipo, string? filtroEstado, int? page)
        {
            // Si intenta inactivar y es el único, bloquear
            if (string.Equals(estado, "I", StringComparison.OrdinalIgnoreCase)
                && !await _svc.ExisteMasDeUnUsuarioAsync())
            {
                TempData["Mensaje"] = "No puede inactivar/eliminar al único usuario del sistema.";
                return RedirectToAction("Index", new { page, q, tipo, estado = filtroEstado });
            }

            var ok = await _svc.ActualizarUsuarioAsync(usuario, estado, tipoUsuario);
            TempData["Mensaje"] = ok ? "Usuario actualizado." : "No se pudo actualizar el usuario.";

            return RedirectToAction("Index", new { page, q, tipo, estado = filtroEstado });
        }

        // Elimina una fila
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Eliminar(string usuario, string? q, int? tipo, string? estado, int? page)
        {
            var ok = await _svc.EliminarUsuarioAsync(usuario);

            TempData["Mensaje"] = ok
                ? $"Usuario '{usuario}' eliminado."
                : "No se pudo eliminar. Debe existir al menos un usuario en el sistema.";

            return RedirectToAction("Index", new { page, q, tipo, estado });
        }

        // (Opcional) Navegar a “Crear”
        [HttpGet]
        public IActionResult Crear() => View();
    }
}



