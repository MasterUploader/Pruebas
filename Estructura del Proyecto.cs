/// <summary>
/// Valida si la terminal corresponde a un e-commerce (virtual).
/// Regla: la terminal se considera virtual si el primer carácter es 'E'.
/// </summary>
/// <param name="terminal">Número o código de terminal recibido.</param>
/// <returns>True si es virtual (e-commerce), False en caso contrario.</returns>
private static bool EsTerminalVirtual(string? terminal)
{
    if (string.IsNullOrWhiteSpace(terminal))
        return false;

    // Evaluamos únicamente el primer carácter, sin importar minúscula/mayúscula
    return terminal.Trim().StartsWith("E", StringComparison.OrdinalIgnoreCase);
}
