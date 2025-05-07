namespace CAUAdministracion.Models;

/// <summary>
/// Modelo que representa una agencia para gestión desde AS400.
/// </summary>
public class AgenciaModel
{
    /// <summary>
    /// Código de centro de costo (identificador único).
    /// </summary>
    public int Codcco { get; set; }

    /// <summary>
    /// Nombre de la agencia.
    /// </summary>
    public string Nombre { get; set; }

    /// <summary>
    /// Zona geográfica a la que pertenece la agencia (1: CENTRO SUR, 2: NOR OCCIDENTE, 3: NOR ORIENTE).
    /// </summary>
    public int Zona { get; set; }

    /// <summary>
    /// Indica si aplica marquesina ("SI" o "NO").
    /// </summary>
    public string Marquesina { get; set; }

    /// <summary>
    /// Indica si aplica reinicio de Branch ("SI" o "NO").
    /// </summary>
    public string RstBranch { get; set; }

    /// <summary>
    /// Nombre del servidor configurado para la agencia.
    /// </summary>
    public string NombreServidor { get; set; }

    /// <summary>
    /// Dirección IP del servidor configurado para la agencia.
    /// </summary>
    public string IpServidor { get; set; }

    /// <summary>
    /// Nombre de la base de datos asociada.
    /// </summary>
    public string NombreBaseDatos { get; set; }
}




using CAUAdministracion.Models;

namespace CAUAdministracion.Services.Agencias;

/// <summary>
/// Contrato para el servicio de gestión de agencias.
/// </summary>
public interface IAgenciaService
{
    /// <summary>
    /// Lista todas las agencias registradas.
    /// </summary>
    Task<List<AgenciaModel>> ObtenerAgenciasAsync();

    /// <summary>
    /// Inserta una nueva agencia en AS400.
    /// </summary>
    bool InsertarAgencia(AgenciaModel agencia);

    /// <summary>
    /// Elimina una agencia según su código de centro de costo.
    /// </summary>
    bool EliminarAgencia(int codcco);

    /// <summary>
    /// Actualiza los datos de una agencia existente.
    /// </summary>
    bool ActualizarAgencia(AgenciaModel agencia);

    /// <summary>
    /// Verifica si un código de centro de costo ya existe en la tabla.
    /// </summary>
    bool ExisteCentroCosto(int codcco);
}


