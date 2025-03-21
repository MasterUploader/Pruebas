private string SerializeToXml<T>(T obj)
{
    try
    {
        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
        ns.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
        ns.Add("gp", "http://www.btsincusa.com/gp");

        // Forzar que el campo Type tenga el valor correcto antes de serializar
        if (obj is BaseRequest baseRequest)
        {
            baseRequest.Type = obj.GetType().Name; 
        }

        XmlSerializer serializer = new XmlSerializer(typeof(T), new Type[]
        {
            typeof(GetServiceRequest),
            typeof(GetForeignExchangeRateRequest),
            typeof(GetIdentificationsRequest),
            typeof(GetPaymentAgentsRequest),
            typeof(GetProductsRequest),
            typeof(GetWholesaleExchangeRateRequest)
        });

        using (StringWriter stringWriter = new StringWriter())
        {
            serializer.Serialize(stringWriter, obj, ns);
            return stringWriter.ToString();
        }
    }
    catch (Exception ex)
    {
        throw new Exception($"Error serializando XML: {ex.Message}");
    }
}
