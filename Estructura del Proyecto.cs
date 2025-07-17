ConsultarSaldoResponse consultarSaldoResponse = null;
int intentos = 0;
int maxIntentos = 2; // puedes ajustar este número si quieres más reintentos

do
{
    consultarSaldoResponse = GuardarConsultaSaldoConsultarSaldoResponse(request.Header, request.Body, codigo90); // Tu método actual

    intentos++;

    if (consultarSaldoResponse?.Factura == null)
    {
        Console.WriteLine($"Factura nula en intento {intentos}, reintentando...");
    }

} while (consultarSaldoResponse?.Factura == null && intentos < maxIntentos);

if (consultarSaldoResponse?.Factura == null)
{
    return new ResponseModelConsulta
    {
        Header = request.Header,
        Body = consultarSaldoResponse,
        Codigo = "400",
        Mensaje = "Factura no encontrada después de varios intentos"
    };
}

// Si todo bien, se asigna el resultado
Data = consultarSaldoResponse;
