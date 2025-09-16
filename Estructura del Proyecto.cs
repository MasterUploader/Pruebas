using Connections.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Adquirencia.Api.Empresas.Services;

/// <summary>
/// Orquesta el flujo completo: fecha operativa → nuevo lote → asientos D/C para empresas.
/// </summary>
/// <remarks>
/// - Centraliza la operación atómica de generación de lote y asientos.
/// - Permite reemplazar el número de corte del request por el del lote generado.
/// - Reutiliza servicios previos: FechaService, LoteService, PosteoEmpresaService.
/// </remarks>
public class LoteEmpresasOrchestrator(
    IDatabaseConnection _as400,
    IHttpContextAccessor _context,
    FechaService _fechaService,         // VerFecha (TAP001 → DSCDT CYYMMDD)
    LoteService _loteService,           // VerUltLote / NuevoLote (POP801)
    PosteoEmpresaService _posteoService // D/C en libro (GLC002 u otro PF)
)
{
    /// <summary>
    /// Ejecuta el flujo: fecha operativa → crea/avanza lote → registra D/C.
    /// </summary>
    /// <param name="req">Payload del endpoint (empresas).</param>
    /// <param name="usuarioOrigen">Usuario trazador.</param>
    /// <returns>Número de lote y respuesta de posteo.</returns>
    public (int numeroLote, PosteoEmpresaResponse posteo) ProcesarConLote(PosteoEmpresaRequest req, string usuarioOrigen)
    {
        // 1) Fecha operativa (TAP001 → DSCDT CYYMMDD + YYYYMMDD para consistencia)
        var (found, dscdt, _) = _fechaService.VerFecha();
        if (!found) throw new InvalidOperationException("No se pudo obtener fecha operativa (TAP001).");

        // 2) Perfil de lote (FTTSKY): asumimos que se usa el código de comercio como perfil.
        var perfilKey = string.IsNullOrWhiteSpace(req.CodigoComercio) ? "EMPRESA" : req.CodigoComercio;

        // 3) Obtener último FTSBT para el perfil y crear siguiente (NuevoLote)
        //    Si ya tienes VerUltLote(perfil), úsalo para leer el último y pásalo a NuevoLote.
        var ultimo = _loteService.VerUltLote(perfilKey);               // lee POP801 (ORDER BY DESC FETCH 1)
        var (nuevoLote, okLote) = _loteService.NuevoLote(perfilKey, usuarioOrigen, dscdt, ultimo);
        if (!okLote) throw new InvalidOperationException("No se pudo crear el nuevo lote (POP801).");

        // 4) Usar el número de lote como “Número de corte” para los asientos
        var reqConLote = new PosteoEmpresaRequest
        {
            NumeroCuenta        = req.NumeroCuenta,
            MontoDebitado       = req.MontoDebitado,
            MontoAcreditado     = req.MontoAcreditado,
            CodigoComercio      = req.CodigoComercio,
            NombreComercio      = req.NombreComercio,
            Terminal            = req.Terminal,
            Descripción         = req.Descripción,
            NaturalezaContable  = req.NaturalezaContable,   // 'C' / 'D'
            NumeroDeCorte       = nuevoLote.ToString(),      // ← sobreescribimos con el lote generado
            IdTransaccionUnico  = req.IdTransaccionUnico,
            Estado              = req.Estado,
            DescripcionEstado   = req.DescripcionEstado
        };

        // 5) Contabilizar D/C (dos asientos balanceados) con el número de lote
        var posteo = _posteoService.Procesar(reqConLote, usuarioOrigen);

        return (nuevoLote, posteo);
    }
}
