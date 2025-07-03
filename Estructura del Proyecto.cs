<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
    <soap:Body>
        <GetRPTResponse xmlns="http://www.btsincusa.com/gp/">
            <GetRPTResult>
                <RESPONSE xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:type="RSDD_RESPONSE" xmlns="http://www.btsincusa.com/gp/">
                    <OPCODE>0000</OPCODE>
                    <PROCESS_MSG>THE OPERATION HAS BEEN COMPLETED.</PROCESS_MSG>
                    <ERROR_PARAM_FULL_NAME></ERROR_PARAM_FULL_NAME>
                    <PROCESS_DT>20250703</PROCESS_DT>
                    <PROCESS_TM>091349</PROCESS_TM>
                    <DATA>
                        <DET_ACT ACTIVITY_DT="20250626" AGENT_CD="HSH" SERVICE_CD="MTR" ORIG_COUNTRY_CD="USA" ORIG_CURRENCY_CD="USD" DEST_COUNTRY_CD="HND" DEST_CURRENCY_CD="HNL" PAYMENT_TYPE_CD="CSA" O_AGENT_CD="BTS">
                            <DETAILS>
                                <DETAIL MOVEMENT_TYPE_CODE="PAYI" CONFIRMATION_NM="79901025626" AGENT_ORDER_NM="79901025626" ORIGIN_AM="200.0000" DESTINATION_AM="5104.6800" FEE_AM="0.0000" DISCOUNT_AM="0.0000" />
                            </DETAILS>
                        </DET_ACT>
                    </DATA>
                </RESPONSE>
            </GetRPTResult>
        </GetRPTResponse>
    </soap:Body>
</soap:Envelope>
