// ================================================
// Devuelve una cadena desde un campo JSON si existe
// Si no existe o es nulo, retorna *blanks
// ================================================
dcl-proc GetStringFromJson;
  dcl-pi *n char(100); // Longitud máxima por defecto
    parentNode pointer value; // Nodo padre donde buscar
    fieldName  varchar(50) const; // Campo a buscar
    maxLength  int(5) const;      // Longitud máxima a retornar
  end-pi;

  dcl-s result char(100);
  dcl-s tempPtr pointer;

  result = *blanks;

  tempPtr = yajl_object_find(parentNode: %trim(fieldName));

  if tempPtr <> *null and yajl_get_string(tempPtr) <> *null;
    result = %subst(%str(yajl_get_string(tempPtr)): 1: maxLength);
  endif;

  return result;
end-proc;
