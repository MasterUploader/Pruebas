// ===============================================================
// GetStringFromJson
//  - Busca un campo string dentro de un nodo JSON YAJL.
//  - Si el campo no existe o es blanco, devuelve blancos.
//  - Limita la longitud al menor valor entre maxLength y el tamaño
//    de la variable interna, para evitar RNX0100.
// ===============================================================
dcl-proc GetStringFromJson;
   // Ahora devolvemos hasta 500 posiciones
   dcl-pi *n char(500);
      parentNode pointer value;
      fieldName  varchar(50) const;
      maxLength  int(5) const;
   end-pi;

   // Internamente también 500 para soportar descripciones largas
   dcl-s result   char(500);
   dcl-s val      pointer;
   dcl-s effLen   int(10);   // longitud efectiva a usar en %subst

   result = *blanks;

   // Busca el campo dentro del objeto JSON
   val = yajl_object_find(parentNode: %trim(fieldName));

   if val <> *null;
      result = yajl_get_string(val);
      if %trim(result) = '';
         result = ' ';
      endif;
   else;
      result = ' ';
   endif;

   // Determina longitud efectiva: no puede exceder el tamaño de result
   effLen = maxLength;
   if effLen > %size(result);
      effLen = %size(result);
   endif;

   if %len(%trim(result)) > 0;
      return %subst(result: 1: effLen);
   else;
      return *blanks;
   endif;
end-proc;
