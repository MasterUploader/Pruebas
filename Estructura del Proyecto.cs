/// <summary>
/// Construye la consulta SQL como una cadena cruda (si la interfaz lo requiere).
/// En este caso, delegamos a BuildContext para centralizar la lógica.
/// </summary>
/// <returns>Consulta SQL en texto plano (opcional o vacía si no se usa directamente).</returns>
public string Build()
{
    // Este método puede ser implementado como string.Empty si no se usa directamente,
    // o puedes delegar a un traductor SQL si lo tienes.
    return string.Empty; // O lanzar excepción si no debe usarse directamente.
}
