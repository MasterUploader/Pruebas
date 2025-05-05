@model CAUAdministracion.Models.MensajeModel
@{
    ViewData["Title"] = "Agregar Mensaje";
    var agencias = ViewBag.Agencias as List<SelectListItem>;
}

<h2>Agregar Nuevo Mensaje</h2>

@if (!string.IsNullOrEmpty(ViewBag.Mensaje))
{
    <div class="alert alert-info alert-dismissible fade show" role="alert">
        @ViewBag.Mensaje
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

<form asp-action="Agregar" method="post">
    <div class="mb-3">
        <label for="codcco" class="form-label">Agencia</label>
        <select id="codcco" name="Codcco" class="form-select" asp-for="Codcco" required>
            <option value="">Seleccione una agencia</option>
            @foreach (var agencia in agencias)
            {
                <option value="@agencia.Value">@agencia.Text</option>
            }
        </select>
    </div>

    <div class="mb-3">
        <label for="mensaje" class="form-label">Mensaje</label>
        <textarea class="form-control" id="mensaje" name="Mensaje" rows="4" required>@Model?.Mensaje</textarea>
    </div>

    <div class="mb-3">
        <label for="estado" class="form-label">Estado</label>
        <select id="estado" name="Estado" class="form-select" asp-for="Estado" required>
            <option value="A" selected>Activo</option>
            <option value="I">Inactivo</option>
        </select>
    </div>

    <button type="submit" class="btn btn-primary">Guardar</button>
    <a asp-controller="Messages" asp-action="Index" class="btn btn-secondary ms-2">Cancelar</a>
</form>

using CAUAdministracion.Models;
using CAUAdministracion.Services.Menssages;
using Connections.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data.Common;

namespace CAUAdministracion.Services.Mensajes;

/// <summary>
/// Servicio para la gestión de mensajes (tabla MANTMSG) en AS400.
/// </summary>
public class MensajeService : IMensajeService
{
    private readonly IDatabaseConnection _as400;

    public MensajeService(IDatabaseConnection as400)
    {
        _as400 = as400;
    }

    /// <summary>
    /// Obtiene todas las agencias que tienen marquesina activada, en formato SelectListItem.
    /// </summary>
    public List<SelectListItem> ObtenerAgenciasSelectList()
    {
        var agencias = new List<SelectListItem>();
        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            command.CommandText = @"
                    SELECT CODCCO, NOMAGE 
                    FROM BCAH96DTA.RSAGE01 
                    WHERE MARQUESINA = 'SI' 
                    ORDER BY NOMAGE";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                agencias.Add(new SelectListItem
                {
                    Value = reader["CODCCO"].ToString(),
                    Text = reader["NOMAGE"].ToString()
                });
            }
        }
        catch (Exception ex)
        {
            agencias.Clear();
            agencias.Add(new SelectListItem
            {
                Value = "",
                Text = "Error: " + ex.Message
            });
        }
        finally
        {
            _as400.Close();
        }

        return agencias;
    }

    /// <summary>
    /// Lista los mensajes filtrados por código de agencia.
    /// </summary>
    public List<MensajeModel> ListarMensajes(string codcco)
    {
        var lista = new List<MensajeModel>();
        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            command.CommandText = $@"
                    SELECT CODMSG, SEQ, MENSAJE, ESTADO 
                    FROM BCAH96DTA.MANTMSG 
                    WHERE CODCCO = '{codcco}'
                    ORDER BY SEQ";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lista.Add(new MensajeModel
                {
                    Codcco = codcco,
                    CodMsg = Convert.ToInt32(reader["CODMSG"]),
                    Seq = Convert.ToInt32(reader["SEQ"]),
                    Mensaje = reader["MENSAJE"].ToString(),
                    Estado = reader["ESTADO"].ToString()
                });
            }
        }
        catch
        {
            // Error controlado, se puede loguear si se desea
        }
        finally
        {
            _as400.Close();
        }

        return lista;
    }

    /// <summary>
    /// Elimina un mensaje de la tabla MANTMSG por su ID.
    /// </summary>
    public bool EliminarMensaje(int codMsg)
    {
        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            command.CommandText = $@"
                    DELETE FROM BCAH96DTA.MANTMSG 
                    WHERE CODMSG = {codMsg}";

            return command.ExecuteNonQuery() > 0;
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

    /// <summary>
    /// Actualiza un mensaje existente.
    /// </summary>
    public bool ActualizarMensaje(MensajeModel mensaje)
    {
        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            command.CommandText = $@"
                    UPDATE BCAH96DTA.MANTMSG
                    SET SEQ = {mensaje.Seq}, 
                        MENSAJE = '{mensaje.Mensaje}', 
                        ESTADO = '{mensaje.Estado}'
                    WHERE CODMSG = {mensaje.CodMsg} 
                      AND CODCCO = '{mensaje.Codcco}'";

            return command.ExecuteNonQuery() > 0;
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

    /// <summary>
    /// Obtiene el siguiente valor de CODMSG (MAX + 1).
    /// </summary>
    public int ObtenerSiguienteId()
    {
        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            command.CommandText = "SELECT MAX(CODMSG) FROM BCAH96DTA.MANTMSG";
            var result = command.ExecuteScalar();

            return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
        }
        catch
        {
            return 1;
        }
        finally
        {
            _as400.Close();
        }
    }

    /// <summary>
    /// Obtiene la lista de mensajes registrados en la tabla MANTMSG desde el AS400.
    /// </summary>
    /// <returns>Una lista de objetos MensajeModel con los datos cargados desde la base de datos.</returns>
    public async Task<List<MensajeModel>> ObtenerMensajesAsync()
    {
        var mensajes = new List<MensajeModel>();

        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            // Consulta para obtener todos los mensajes registrados
            command.CommandText = @"
            SELECT CODCCO, CODMSG, SEQ, MENSAJE, ESTADO
            FROM BCAH96DTA.MANTMSG
            ORDER BY CODCCO, SEQ";

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                mensajes.Add(new MensajeModel
                {
                    Codcco = reader["CODCCO"]?.ToString(),
                    CodMsg = Convert.ToInt32(reader["CODMSG"]),
                    Seq = Convert.ToInt32(reader["SEQ"]),
                    Mensaje = reader["MENSAJE"]?.ToString(),
                    Estado = reader["ESTADO"]?.ToString()
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al obtener los mensajes: " + ex.Message);
            // Puedes usar un logger aquí si ya tienes uno integrado
        }
        finally
        {
            _as400.Close();
        }

        return mensajes;
    }


    /// <summary>
    /// Verifica si un mensaje tiene dependencias en otra tabla antes de eliminarlo.
    /// Esto previene eliminar mensajes que aún están en uso.
    /// </summary>
    /// <param name="codcco">Código de agencia</param>
    /// <param name="codMsg">Código del mensaje</param>
    /// <returns>True si hay dependencias encontradas, False si no hay o si ocurre un error.</returns>
    public bool TieneDependencia(string codcco, int codMsg)
    {
        try
        {
            _as400.Open();
            using var command = _as400.GetDbCommand();

            // Consulta a una tabla relacionada (ajústala si se conoce el nombre real)
            command.CommandText = $@"
            SELECT COUNT(*) 
            FROM BCAH96DTA.OTRATABLA 
            WHERE CODCCO = '{codcco}' 
              AND CODMSG = {codMsg}";

            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }
        catch
        {
            return true; // Si hay error, asumimos que sí tiene dependencia
        }
        finally
        {
            _as400.Close();
        }
    }

    /// <summary>
    /// Inserta un nuevo mensaje en la tabla MANTMSG en AS400.
    /// Genera el nuevo código automáticamente (MAX + 1) y secuencia correlativa por agencia.
    /// </summary>
    /// <param name="mensaje">Modelo con los datos del mensaje a insertar</param>
    /// <returns>True si se insertó correctamente, false si hubo un error</returns>
    public bool InsertarMensaje(MensajeModel mensaje)
    {
        try
        {
            _as400.Open();

            if (!_as400.IsConnected())
                return false;

            using var command = _as400.GetDbCommand();

            // Obtener nuevo CODMSG
            int nuevoId = GetUltimoId(command);

            // Obtener nueva secuencia por agencia
            int nuevaSecuencia = GetSecuencia(command, mensaje.Codcco);

            // Construir query SQL de inserción
            command.CommandText = $@"
            INSERT INTO BCAH96DTA.MANTMSG (CODMSG, CODCCO, SEQ, MENSAJE, ESTADO)
            VALUES ({nuevoId}, '{mensaje.Codcco}', {nuevaSecuencia}, '{mensaje.Mensaje}', '{mensaje.Estado}')";

            int filas = command.ExecuteNonQuery();
            return filas > 0;
        }
        catch
        {
            // Podrías loguear aquí el error si deseas
            return false;
        }
        finally
        {
            _as400.Close();
        }
    }



    /// <summary>
    /// Obtiene el próximo código de mensaje (CODMSG) a usar
    /// </summary>
    public int GetUltimoId(DbCommand command)
    {
        command.CommandText = "SELECT MAX(CODMSG) FROM BCAH96DTA.MANTMSG";
        var result = command.ExecuteScalar();
        return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
    }

    /// <summary>
    /// Obtiene la próxima secuencia (SEQ) para una agencia específica
    /// </summary>
    public int GetSecuencia(DbCommand command, string codcco)
    {
        command.CommandText = $"SELECT MAX(SEQ) FROM BCAH96DTA.MANTMSG WHERE CODCCO = '{codcco}'";
        var result = command.ExecuteScalar();
        return result != DBNull.Value ? Convert.ToInt32(result) + 1 : 1;
    }
}
