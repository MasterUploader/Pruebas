consultarSaldoResponse = await _consultarSaldoRepository.ConsultarSaldoAsync(request);

if (consultarSaldoResponse?.Factura == null)
{
    // Reintento una sola vez
    consultarSaldoResponse = await _consultarSaldoRepository.ConsultarSaldoAsync(request);
}

// Si después del reintento sigue viniendo null, retorna error
if (consultarSaldoResponse?.Factura == null)
{
    return new ResponseModelConsulta
    {
        Header = request.Header,
        Body = consultarSaldoResponse,
        Codigo = "400",
        Mensaje = "Factura no encontrada después del reintento de consulta"
    };
}

Data = consultarSaldoResponse;
