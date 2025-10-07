Tengo la siguiente situación, en algunos casos cierto datos vienen llenos lo cual esta bien, ejemplo:

<ORIGIN_AM>300.0000</ORIGIN_AM>
            <DESTINATION_AM>7657.0200</DESTINATION_AM>
            <EXCH_RATE_FX>25.5234000000</EXCH_RATE_FX>
            <MARKET_REF_CURRENCY_CD>USD</MARKET_REF_CURRENCY_CD>
            <MARKET_REF_CURRENCY_FX>25.52340</MARKET_REF_CURRENCY_FX>
            <MARKET_REF_CURRENCY_AM>300.00</MARKET_REF_CURRENCY_AM>

  Pero hay otros casos donde vienen algunos vacios, eso tambien esta bien, eso es para un tipo de caso, ejemplo:

<ORIGIN_AM>300.0000</ORIGIN_AM>
            <DESTINATION_AM>7657.0200</DESTINATION_AM>
            <EXCH_RATE_FX>25.5234000000</EXCH_RATE_FX>
            <MARKET_REF_CURRENCY_CD></MARKET_REF_CURRENCY_CD>
            <MARKET_REF_CURRENCY_FX></MARKET_REF_CURRENCY_FX>
            <MARKET_REF_CURRENCY_AM></MARKET_REF_CURRENCY_AM>

  Necesito que cuando el caso venga vacio, tome los datos de los campos que si vienen:

<ORIGIN_AM>300.0000</ORIGIN_AM>
            <DESTINATION_AM>7657.0200</DESTINATION_AM>
            <EXCH_RATE_FX>25.5234000000</EXCH_RATE_FX>

  Y los agregue a los que no vienen y les aplique el redondeo sin perdidas, para que quede así:
<ORIGIN_AM>300.0000</ORIGIN_AM>
            <DESTINATION_AM>7657.0200</DESTINATION_AM>
            <EXCH_RATE_FX>25.5234000000</EXCH_RATE_FX>
            <MARKET_REF_CURRENCY_CD>USD</MARKET_REF_CURRENCY_CD>
            <MARKET_REF_CURRENCY_FX>25.52340</MARKET_REF_CURRENCY_FX>
            <MARKET_REF_CURRENCY_AM>300.00</MARKET_REF_CURRENCY_AM>
El campo <MARKET_REF_CURRENCY_CD></MARKET_REF_CURRENCY_CD>, lo pued eobtener de <ORIG_CURRENCY_CD>USD</ORIG_CURRENCY_CD>.



  Esto desde el API, ¿es posible realizarlo?

