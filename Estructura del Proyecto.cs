Por ejemplo en este linea de codigo en vFullFileC esta la ruta del archivo json, no el json como tal, eso afectara en algo?


          rc = HTTP_POST(%trim(pUrlPost)
                     : %addr( vFullFileC)
                     : %len(vFullFileC)
                     : %Trim(vFullFileR)
                     : HTTP_TIMEOUT
                     : HTTP_USERAGENT
                     : headers );
