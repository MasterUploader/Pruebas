using System.ComponentModel.DataAnnotations;

namespace CAUAdministracion.Models
{
    /// <summary>Modelo de edición para USUADMIN.</summary>
    public class UsuarioEditModel
    {
        [Required, MaxLength(32)]
        public string Usuario { get; set; } = string.Empty;

        /// <summary>1 = Administrador, 2 = Admin. Videos, 3 = Admin. Mensajes</summary>
        [Range(1, 3)]
        public int TipoUsuario { get; set; }

        /// <summary>A = Activo, I = Inactivo</summary>
        [Required, RegularExpression("A|I")]
        public string Estado { get; set; } = "A";

        /// <summary>Contraseña nueva (opcional). Si viene vacía, no se actualiza.</summary>
        [MaxLength(128)]
        public string? PASS { get; set; }
    }
}


// GET: /Usuarios/Actualizar?usuario=alguien
[HttpGet, AutorizarPorTipoUsuario("1")]
public IActionResult Actualizar(string usuario)
{
    var model = _usuarioService.ObtenerPorId(usuario);
    if (model == null) return NotFound();
    return View(model);
}

// POST: /Usuarios/Actualizar
[HttpPost, ValidateAntiForgeryToken, AutorizarPorTipoUsuario("1")]
public async Task<IActionResult> Actualizar(UsuarioEditModel model)
{
    if (!ModelState.IsValid) return View(model);

    var ok = await _usuarioService.Actualizar(model);
    TempData["Mensaje"] = ok ? "Usuario actualizado." : "No se pudo actualizar.";
    TempData["MensajeTipo"] = ok ? "success" : "danger";
    return RedirectToAction("Index");
}


public UsuarioEditModel? ObtenerPorId(string usuario)
{
    try
    {
        _as400.Open();
        if (!_as400.IsConnected) return null;

        using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);

        var query = QueryBuilder.Core.QueryBuilder
            .From("USUADMIN", "BCAH96DTA")
            .Select("USUARIO", "TIPUSU", "ESTADO")
            .WhereRaw($"USUARIO = '{usuario}'")
            .Build();

        command.CommandText = query.Sql;
        if (command.Connection?.State == System.Data.ConnectionState.Closed)
            command.Connection.Open();

        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;

        return new UsuarioEditModel
        {
            Usuario     = reader["USUARIO"]?.ToString() ?? "",
            TipoUsuario = Convert.ToInt32(reader["TIPUSU"]),
            Estado      = reader["ESTADO"]?.ToString() ?? "A",
            PASS        = null // por seguridad, no se expone
        };
    }
    catch { return null; }
    finally { _as400.Close(); }
}


public async Task<bool> Actualizar(UsuarioEditModel model)
{
    if (model is null || string.IsNullOrWhiteSpace(model.Usuario)) return false;
    if (model.TipoUsuario is < 1 or > 3) return false;
    if (model.Estado is not ("A" or "I")) return false;

    // ¿Hay nueva clave?
    if (!string.IsNullOrWhiteSpace(model.PASS))
    {
        var passCifrada = OperacionesVarias.EncriptarCadena(model.PASS);

        try
        {
            _as400.Open();
            if (!_as400.IsConnected) return false;

            using var command = _as400.GetDbCommand(_httpContextAccessor.HttpContext!);

            var update = new UpdateQueryBuilder("USUADMIN", "BCAH96DTA")
                .Set("PASS",   $"'{passCifrada}'")
                .Set("TIPUSU", model.TipoUsuario.ToString())
                .Set("ESTADO", $"'{model.Estado}'")
                .WhereRaw($"USUARIO = '{model.Usuario}'")
                .Build();

            command.CommandText = update.Sql;
            if (command.Connection?.State == System.Data.ConnectionState.Closed)
                command.Connection.Open();

            var rows = await command.ExecuteNonQueryAsync();
            return rows > 0;
        }
        catch { return false; }
        finally { _as400.Close(); }
    }
    else
    {
        // Reutiliza tu método existente para no tocar PASS
        return await ActualizarUsuarioAsync(model.Usuario, model.Estado, model.TipoUsuario);
    }
}

