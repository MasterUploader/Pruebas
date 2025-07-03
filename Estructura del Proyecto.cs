using System;
using System.Linq;
using System.Xml.Linq;

class Program
{
    static void Main()
    {
        string xml = @"<?xml version='1.0' encoding='utf-8'?>
<soap:Envelope xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <soap:Body>
    <GetRPTResponse xmlns='http://www.btsincusa.com/gp/'>
      <GetRPTResult>
        <RESPONSE xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:type='RSDD_RESPONSE' xmlns='http://www.btsincusa.com/gp/'>
          <DATA>
            <DET_ACT ACTIVITY_DT='20250626' AGENT_CD='HSH' SERVICE_CD='MTR'
                     ORIG_COUNTRY_CD='USA' ORIG_CURRENCY_CD='USD'
                     DEST_COUNTRY_CD='HND' DEST_CURRENCY_CD='HNL'
                     PAYMENT_TYPE_CD='CSA' O_AGENT_CD='BTS'>
              <DETAILS>
                <DETAIL MOVEMENT_TYPE_CODE='PAYI' CONFIRMATION_NM='79901025626'
                        AGENT_ORDER_NM='79901025626' ORIGIN_AM='200.0000'
                        DESTINATION_AM='5104.6800' FEE_AM='0.0000' DISCOUNT_AM='0.0000' />
              </DETAILS>
            </DET_ACT>
          </DATA>
        </RESPONSE>
      </GetRPTResult>
    </GetRPTResponse>
  </soap:Body>
</soap:Envelope>";

        XDocument doc = XDocument.Parse(xml);
        XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
        XNamespace gp = "http://www.btsincusa.com/gp/";

        var detAct = doc.Descendants(gp + "DET_ACT").FirstOrDefault();
        var detail = detAct?.Element(gp + "DETAILS")?.Element(gp + "DETAIL");

        if (detAct != null && detail != null)
        {
            var dataToInsert = new
            {
                ActivityDate = detAct.Attribute("ACTIVITY_DT")?.Value,
                AgentCode = detAct.Attribute("AGENT_CD")?.Value,
                ServiceCode = detAct.Attribute("SERVICE_CD")?.Value,
                OrigCountry = detAct.Attribute("ORIG_COUNTRY_CD")?.Value,
                OrigCurrency = detAct.Attribute("ORIG_CURRENCY_CD")?.Value,
                DestCountry = detAct.Attribute("DEST_COUNTRY_CD")?.Value,
                DestCurrency = detAct.Attribute("DEST_CURRENCY_CD")?.Value,
                PaymentType = detAct.Attribute("PAYMENT_TYPE_CD")?.Value,
                OriginAgent = detAct.Attribute("O_AGENT_CD")?.Value,

                MovementType = detail.Attribute("MOVEMENT_TYPE_CODE")?.Value,
                Confirmation = detail.Attribute("CONFIRMATION_NM")?.Value,
                AgentOrder = detail.Attribute("AGENT_ORDER_NM")?.Value,
                OriginAmount = detail.Attribute("ORIGIN_AM")?.Value,
                DestinationAmount = detail.Attribute("DESTINATION_AM")?.Value,
                Fee = detail.Attribute("FEE_AM")?.Value,
                Discount = detail.Attribute("DISCOUNT_AM")?.Value
            };

            // Ejemplo: imprimir datos (podrías aquí hacer un INSERT a base de datos)
            Console.WriteLine($"Activity Date: {dataToInsert.ActivityDate}");
            Console.WriteLine($"Agent Code: {dataToInsert.AgentCode}");
            Console.WriteLine($"Service Code: {dataToInsert.ServiceCode}");
            Console.WriteLine($"Origin Country: {dataToInsert.OrigCountry}");
            Console.WriteLine($"Origin Amount: {dataToInsert.OriginAmount}");
            Console.WriteLine($"Destination Amount: {dataToInsert.DestinationAmount}");
            Console.WriteLine($"Confirmation: {dataToInsert.Confirmation}");
        }
    }
}
