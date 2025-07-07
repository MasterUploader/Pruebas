Tengo esta respuesta XML, necesito que crees las clases con los comentarios XML y JSON, como te muestro en el ejemplo de a continuación, todos los campos manejalos como string

/// <summary>
/// Número de Confirmación.
/// </summary>
[XmlElement("CONFIRMATION_NM")]
[JsonProperty("confirmationNumber")]
public string SaleDt { get; set; } = string.Empty;


<RESPONSE xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:type="DEPOSITS_LIST" xmlns="http://www.btsincusa.com/gp/">
                    <OPCODE>1308</OPCODE>
                    <PROCESS_MSG>SDEP ACCEPTED ORDER</PROCESS_MSG>
                    <ERROR_PARAM_FULL_NAME />
                    <PROCESS_DT>20250707</PROCESS_DT>
                    <PROCESS_TM>100013</PROCESS_TM>
                    <DEPOSITS>
                        <DEPOSIT>
                            <DATA>
                                <CONFIRMATION_NM>89901012400</CONFIRMATION_NM>
                                <SALE_MOVEMENT_ID>17373378</SALE_MOVEMENT_ID>
                                <SALE_DT>20250703</SALE_DT>
                                <SALE_TM>205003</SALE_TM>
                                <SERVICE_CD>MTR</SERVICE_CD>
                                <PAYMENT_TYPE_CD>ACC</PAYMENT_TYPE_CD>
                                <ORIG_COUNTRY_CD>USA</ORIG_COUNTRY_CD>
                                <ORIG_CURRENCY_CD>USD</ORIG_CURRENCY_CD>
                                <DEST_COUNTRY_CD>HND</DEST_COUNTRY_CD>
                                <DEST_CURRENCY_CD>HNL</DEST_CURRENCY_CD>
                                <ORIGIN_AM>5400.00</ORIGIN_AM>
                                <DESTINATION_AM>140754.24</DESTINATION_AM>
                                <EXCH_RATE_FX>26.0656000000</EXCH_RATE_FX>
                                <MARKET_REF_CURRENCY_CD>USD</MARKET_REF_CURRENCY_CD>
                                <MARKET_REF_CURRENCY_FX>26.0656000000</MARKET_REF_CURRENCY_FX>
                                <MARKET_REF_CURRENCY_AM>5400.00</MARKET_REF_CURRENCY_AM>
                                <S_AGENT_CD>BTS</S_AGENT_CD>
                                <S_COUNTRY_CD>USA</S_COUNTRY_CD>
                                <S_STATE_CD>TX </S_STATE_CD>
                                <R_ACCOUNT_TYPE_CD>NOT</R_ACCOUNT_TYPE_CD>
                                <R_ACCOUNT_NM>5200342008</R_ACCOUNT_NM>
                                <R_AGENT_CD>HSH</R_AGENT_CD>
                                <SENDER>
                                    <FIRST_NAME>MIGUEL</FIRST_NAME>
                                    <MIDDLE_NAME>ANGEL</MIDDLE_NAME>
                                    <LAST_NAME>SEPULVEDA</LAST_NAME>
                                    <MOTHER_M_NAME>HENDERSON</MOTHER_M_NAME>
                                    <ADDRESS>
                                        <ADDRESS>820 N WILCOX AVE</ADDRESS>
                                        <CITY>MONTEBELLO</CITY>
                                        <STATE_CD>CA </STATE_CD>
                                        <COUNTRY_CD>USA</COUNTRY_CD>
                                        <ZIP_CODE>90640</ZIP_CODE>
                                        <PHONE>13238873090</PHONE>
                                    </ADDRESS>
                                </SENDER>
                                <RECIPIENT>
                                    <FIRST_NAME>EUGENIA</FIRST_NAME>
                                    <MIDDLE_NAME>FATIMA</MIDDLE_NAME>
                                    <LAST_NAME>GALEANO</LAST_NAME>
                                    <MOTHER_M_NAME>DIAZ</MOTHER_M_NAME>
                                    <ADDRESS>
                                        <ADDRESS>DOMICILIO CONOCIDO</ADDRESS>
                                        <CITY>CIUDAD CONOCIDA</CITY>
                                        <STATE_CD>ATL</STATE_CD>
                                        <COUNTRY_CD>HND</COUNTRY_CD>
                                        <ZIP_CODE>31001</ZIP_CODE>
                                        <PHONE>+5045244034</PHONE>
                                    </ADDRESS>
                                </RECIPIENT>
                            </DATA>
                        </DEPOSIT>
                        <DEPOSIT>
                            <DATA>
                                <CONFIRMATION_NM>89901012418</CONFIRMATION_NM>
                                <SALE_MOVEMENT_ID>17373379</SALE_MOVEMENT_ID>
                                <SALE_DT>20250703</SALE_DT>
                                <SALE_TM>205003</SALE_TM>
                                <SERVICE_CD>MTR</SERVICE_CD>
                                <PAYMENT_TYPE_CD>ACC</PAYMENT_TYPE_CD>
                                <ORIG_COUNTRY_CD>USA</ORIG_COUNTRY_CD>
                                <ORIG_CURRENCY_CD>USD</ORIG_CURRENCY_CD>
                                <DEST_COUNTRY_CD>HND</DEST_COUNTRY_CD>
                                <DEST_CURRENCY_CD>HNL</DEST_CURRENCY_CD>
                                <ORIGIN_AM>3500.00</ORIGIN_AM>
                                <DESTINATION_AM>91229.60</DESTINATION_AM>
                                <EXCH_RATE_FX>26.0656000000</EXCH_RATE_FX>
                                <MARKET_REF_CURRENCY_CD>USD</MARKET_REF_CURRENCY_CD>
                                <MARKET_REF_CURRENCY_FX>26.0656000000</MARKET_REF_CURRENCY_FX>
                                <MARKET_REF_CURRENCY_AM>3500.00</MARKET_REF_CURRENCY_AM>
                                <S_AGENT_CD>BTS</S_AGENT_CD>
                                <S_COUNTRY_CD>USA</S_COUNTRY_CD>
                                <S_STATE_CD>TX </S_STATE_CD>
                                <R_ACCOUNT_TYPE_CD>NOT</R_ACCOUNT_TYPE_CD>
                                <R_ACCOUNT_NM>3011618679</R_ACCOUNT_NM>
                                <R_AGENT_CD>HSH</R_AGENT_CD>
                                <SENDER>
                                    <FIRST_NAME>MIGUEL</FIRST_NAME>
                                    <MIDDLE_NAME>ANGEL</MIDDLE_NAME>
                                    <LAST_NAME>MONDRAGON</LAST_NAME>
                                    <MOTHER_M_NAME>BUSTILLOS</MOTHER_M_NAME>
                                    <ADDRESS>
                                        <ADDRESS>721 CASTROVILLE RD</ADDRESS>
                                        <CITY>SAN ANTONIO</CITY>
                                        <STATE_CD>TX </STATE_CD>
                                        <COUNTRY_CD>USA</COUNTRY_CD>
                                        <ZIP_CODE>78237</ZIP_CODE>
                                        <PHONE>+12104320949</PHONE>
                                    </ADDRESS>
                                </SENDER>
                                <RECIPIENT>
                                    <FIRST_NAME>FABIOLA</FIRST_NAME>
                                    <MIDDLE_NAME>PATRICIA</MIDDLE_NAME>
                                    <LAST_NAME>BONILLA</LAST_NAME>
                                    <MOTHER_M_NAME>HERNANDEZ</MOTHER_M_NAME>
                                    <ADDRESS>
                                        <ADDRESS>DOMICILIO CONOCIDO</ADDRESS>
                                        <CITY>CIUDAD CONOCIDA</CITY>
                                        <STATE_CD>ATL</STATE_CD>
                                        <COUNTRY_CD>HND</COUNTRY_CD>
                                        <ZIP_CODE>31001</ZIP_CODE>
                                        <PHONE>+5044161416</PHONE>
                                    </ADDRESS>
                                </RECIPIENT>
                            </DATA>
                        </DEPOSIT>
                    </DEPOSITS>
                </RESPONSE>
