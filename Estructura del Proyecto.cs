if (obj is BaseRequest baseRequest)
{
    baseRequest.Type = obj switch
    {
        GetServiceRequest => "GetServiceRequest",
        GetDataRequest => "GetDataRequest",
        GetProductsRequest => "GetProductsRequest",
        GetPaymentAgentsRequest => "GetPaymentAgentsRequest",
        GetWholesaleExchangeRateRequest => "GetWholesaleExchangeRateRequest",
        GetForeignExchangeRateRequest => "GetForeignExchangeRateRequest",
        GetIdentificationsRequest => "GetIdentificationsRequest",
        _ => throw new Exception("Tipo de solicitud desconocido")
    };
}
