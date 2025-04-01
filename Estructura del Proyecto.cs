// ==============================================
// Devuelve una cadena desde un nodo JSON
// Si el nodo o valor es nulo, retorna *blanks
// ==============================================
dcl-proc SafeGetString;
   dcl-pi *n char(100);
      jsonPtr pointer value;
   end-pi;

   // Validamos primero que el puntero no sea nulo
   if jsonPtr <> *null;

      // Luego validamos que yajl_get_string también devuelva un puntero no nulo
      if yajl_get_string(jsonPtr) <> *null;
         return %str(yajl_get_string(jsonPtr));
      else;
         return *blanks;
      endif;

   else;
      return *blanks;
   endif;

end-proc;
