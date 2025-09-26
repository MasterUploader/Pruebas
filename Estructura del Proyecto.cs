public class ConsultaBtsDto
{
    private string _exchangeRateFx = string.Empty;

    public string ExchangeRateFx
    {
        get => _exchangeRateFx is null ? string.Empty
               : (_exchangeRateFx.Length > 10 ? _exchangeRateFx[..10] : _exchangeRateFx);
        set => _exchangeRateFx = value?.Trim() ?? string.Empty;
    }
}
