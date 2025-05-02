/// <summary>
/// Guarda el archivo recibido en el disco, detectando si la ruta es de red o local.
/// Lanza errores controlados si no se puede escribir en la ruta especificada.
/// </summary>
/// <param name="archivo">Archivo subido (formulario)</param>
/// <param name="codcco">Código de agencia</param>
/// <param name="rutaBase">Ruta base (extraída de ConnectionData.json según el ambiente)</param>
/// <param name="nombreArchivo">Nombre del archivo original</param>
/// <returns>True si el archivo se guardó correctamente, false en caso contrario</returns>
public async Task<bool> GuardarArchivoEnDisco(IFormFile archivo, string codcco, string rutaBase, string nombreArchivo)
{
    try
    {
        // Asegura que la ruta base termine en backslash
        if (!rutaBase.EndsWith(Path.DirectorySeparatorChar))
            rutaBase += Path.DirectorySeparatorChar;

        // Ruta completa del archivo
        string rutaCompleta = Path.Combine(rutaBase, nombreArchivo);

        // Detectar si es ruta de red compartida (UNC) o local
        bool esRutaCompartida = rutaBase.StartsWith(@"\\");
        
        // Validación para rutas compartidas (no se deben crear)
        if (esRutaCompartida)
        {
            if (!Directory.Exists(rutaBase))
                throw new DirectoryNotFoundException($"La ruta de red '{rutaBase}' no está disponible o no existe.");
        }
        else
        {
            // Crear ruta local si no existe (solo si no es red)
            if (!Directory.Exists(rutaBase))
                Directory.CreateDirectory(rutaBase);
        }

        // Guardar el archivo físicamente en la ruta
        using var stream = new FileStream(rutaCompleta, FileMode.Create, FileAccess.Write);
        await archivo.CopyToAsync(stream);

        return true;
    }
    catch (DirectoryNotFoundException ex)
    {
        // Error específico por ruta no encontrada
        _logger?.LogError(ex, "Error de ruta al guardar el archivo en disco: {Ruta}", rutaBase);
        return false;
    }
    catch (UnauthorizedAccessException ex)
    {
        // Permisos insuficientes en la ruta
        _logger?.LogError(ex, "Sin permisos para escribir en la ruta: {Ruta}", rutaBase);
        return false;
    }
    catch (Exception ex)
    {
        // Error general
        _logger?.LogError(ex, "Error inesperado al guardar el archivo en disco");
        return false;
    }
}
