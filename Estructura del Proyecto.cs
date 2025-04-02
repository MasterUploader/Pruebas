// ============================================
// Procedimiento que obtiene una cadena de un campo JSON
// Si el valor no existe o es nulo, retorna blanks
// ============================================
dcl-proc GetStringFromJson;
  dcl-pi *n char(100); // Valor a retornar (máximo 100 caracteres)
    parentNode pointer value;     // Nodo padre donde buscar
    fieldName varchar(50) const;  // Nombre del campo
    maxLength int(5) const;       // Longitud máxima del campo
  end-pi;

  dcl-s result char(100);
  dcl-s tempPtr pointer;
  dcl-s tempStrPtr pointer;

  result = *blanks;

  tempPtr = yajl_object_find(parentNode: %trim(fieldName));
  tempStrPtr = yajl_get_string(tempPtr);

  if tempPtr <> *null and tempStrPtr <> *null;
     // Tomar la menor longitud entre el valor y el máximo permitido
     result = %subst(%str(tempStrPtr): 1: %min(maxLength: %len(%str(tempStrPtr))));
  endif;

  return result;
end-proc;
