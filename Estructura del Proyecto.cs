using System.ComponentModel.DataAnnotations;

namespace CAUAdministracion.Models
{
    /// <summary>
    /// Modelo para la edición/actualización de usuarios.
    /// Se usa en el GET/POST del endpoint Actualizar (UsuariosController)
    /// y en el servicio para mapear a los campos de la tabla USUADMIN.
    /// </summary>
    public class UsuarioEditModel
    {
        /// <summary>Identificador único del usuario (ej. CODUSU)</summary>
        [Required]
        public int Id { get; set; }

        /// <summary>Login/username del usuario (ej. USUARIO)</summary>
        [Required, StringLength(50)]
        public string Usuario { get; set; } = string.Empty;

        /// <summary>Nombre completo o nombre a mostrar (ej. NOMBRE)</summary>
        [Required, StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Tipo de usuario/rol (ej. TIPOUSU o ROL)</summary>
        [Required, StringLength(10)]
        public string TipoUsuario { get; set; } = string.Empty;

        /// <summary>Código de agencia (ej. CODCCO)</summary>
        [Required, StringLength(10)]
        public string Codcco { get; set; } = string.Empty;

        /// <summary>Estado del usuario (A/I)</summary>
        [Required, RegularExpression("A|I")]
        public string Estado { get; set; } = "A";
    }
}

using CAUAdministracion.Models;

namespace CAUAdministracion.Services.Usuarios
{
    public interface IUsuarioService
    {
        // ... (métodos existentes)

        /// <summary>Obtiene un usuario por Id (CODUSU, por ejemplo).</summary>
        UsuarioEditModel? ObtenerPorId(int id);

        /// <summary>
        /// Actualiza un usuario usando el modelo de edición.
        /// Reutiliza la lógica existente de ActualizarUsuarioAsync internamente.
        /// </summary>
        Task<bool> Actualizar(UsuarioEditModel model);
    }
}




using CAUAdministracion.Models;
using Connections.Abstractions;
using System.Data.Common;
using QueryBuilder.Builders;

namespace CAUAdministracion.Services.Usuarios
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IDatabaseConnection _as400;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UsuarioService(IDatabaseConnection as400, IHttpContextAccessor httpContextAccessor)
        {
            _as400 = as400;
            _httpContextAccessor = httpContextAccessor;
        }

        // ===================== OBTENER POR ID =====================
        public UsuarioEditModel? ObtenerPorId(int id)
        {
            try
            {
                _as400.Open();
                if (!_as400.IsConnected) return null;

                using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);

                // Ajusta nombres de tabla/campos a tu esquema real (USUADMIN/BCAH96DTA)
                var query = QueryBuilder.Core.QueryBuilder
                    .From("USUADMIN", "BCAH96DTA")
                    .Select("CODUSU", "USUARIO", "NOMBRE", "TIPOUSU", "CODCCO", "ESTADO")
                    .Where<USUADMIN>(x => x.CODUSU == id)   // si tu entidad de modelo para Where se llama distinto, cámbiala
                    .Build();

                command.CommandText = query.Sql;
                if (command.Connection?.State == System.Data.ConnectionState.Closed)
                    command.Connection.Open();

                using var reader = command.ExecuteReader();
                if (!reader.Read()) return null;

                var model = new UsuarioEditModel
                {
                    Id         = Convert.ToInt32(reader["CODUSU"]),
                    Usuario    = reader["USUARIO"]?.ToString() ?? "",
                    Nombre     = reader["NOMBRE"]?.ToString() ?? "",
                    TipoUsuario= reader["TIPOUSU"]?.ToString() ?? "",
                    Codcco     = reader["CODCCO"]?.ToString() ?? "",
                    Estado     = reader["ESTADO"]?.ToString() ?? "A"
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
                model.Id,
                model.Usuario,
                model.Nombre,
                model.TipoUsuario,
                model.Codcco,
                model.Estado
            );
        }

        // ========== EXISTENTE EN TU PROYECTO (ejemplo de firma) ==========
        // OJO: NO borres tu implementación original; sólo asegúrate que la firma
        // real coincida y esté en esta clase. Si tiene otra firma, ajusta arriba.
        private async Task<bool> ActualizarUsuarioAsync(int id, string usuario, string nombre, string tipoUsuario, string codcco, string estado)
        {
            try
            {
                _as400.Open();
                if (!_as400.IsConnected) return false;

                using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);

                // Puedes usar tu QueryBuilder.UpdateQueryBuilder si ya lo tienes.
                // Aquí lo muestro con UpdateQueryBuilder; si aún no está, arma el SQL directo.
                var update = new UpdateQueryBuilder("USUADMIN", "BCAH96DTA")
                    .Set(
                        ("USUARIO", usuario),
                        ("NOMBRE", nombre),
                        ("TIPOUSU", tipoUsuario),
                        ("CODCCO", codcco),
                        ("ESTADO", estado)
                    )
                    .Where<USUADMIN>(x => x.CODUSU == id)
                    .Build();

                command.CommandText = update.Sql;

                if (command.Connection?.State == System.Data.ConnectionState.Closed)
                    command.Connection.Open();

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
    }
}





<a asp-controller="Usuarios" asp-action="Actualizar" asp-route-id="@usuario.Id">Editar</a>


