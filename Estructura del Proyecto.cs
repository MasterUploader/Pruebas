/// <summary>
/// Regla de cargo (porcentaje o monto fijo) dirigida a una cuenta GL.
/// </summary>
/// <remarks>
/// - Si <see cref="Porcentaje"/> &gt; 0, se evalúa (Total * Porcentaje).
/// - Si <see cref="MontoFijo"/> &gt; 0, se suma como importe fijo.
/// - La naturaleza del cargo será la opuesta a la línea principal.
/// </remarks>
public sealed class ReglaCargo()
{
    public string Codigo { get; set; } = string.Empty;   // Ej. "INT", "COM", "IVA"
    public string CuentaGl { get; set; } = string.Empty; // Cuenta contable destino (GL)
    public decimal Porcentaje { get; set; }              // 0..1 (3% => 0.03)
    public decimal MontoFijo { get; set; }               // importe fijo
}

/// <summary>
/// Resultado de un cargo calculado (monto final y metadatos).
/// </summary>
public sealed class CargoCalculado()
{
    public string Codigo { get; set; } = string.Empty;
    public string CuentaGl { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}
