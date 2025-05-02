/// <summary>
/// Guarda el archivo de video directamente en la ruta de red especificada en ConnectionData.json.
/// No agrega subcarpetas ni modificaciones adicionales a la ruta.
/// </summary>
/// <param name="archivo">Archivo de video subido por el usuario</param>
/// <param name="codcco">Código de agencia (no se usa aquí, solo se conserva por contrato)</param>
/// <param name="rutaBase">Ruta completa desde ConnectionData.json (ej: \\ServidorCompartido\Marquesin\)</param>
/// <param name="nombreArchivo">Nombre del archivo a guardar</param>
/// <returns>True si se guarda correctamente, False si ocurre error</returns>
public async Task<bool> GuardarArchivoEnDisco(IFormFile archivo, string codcco, string rutaBase, string nombreArchivo)
{
    try
    {
        // Asegura que la ruta base finalice con backslash por consistencia
        if (!rutaBase.EndsWith(Path.DirectorySeparatorChar))
            rutaBase += Path.DirectorySeparatorChar;

        // Ruta completa del archivo, sin agregar carpetas adicionales
        string rutaCompleta = Path.Combine(rutaBase, nombreArchivo);

        // Crear el directorio si no existe (por seguridad)
        if (!Directory.Exists(rutaBase))
            Directory.CreateDirectory(rutaBase);

        // Escribir el archivo en disco
        using var stream = new FileStream(rutaCompleta, FileMode.Create);
        await archivo.CopyToAsync(stream);

        return true;
    }
    catch (Exception ex)
    {
        // Aquí puedes agregar logging si es necesario
        return false;
    }
}
