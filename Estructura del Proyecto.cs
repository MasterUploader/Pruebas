dcl-proc GetStringFromJson;
  dcl-pi *n char(100);
    parentNode pointer value;
    fieldName varchar(50) const;
    maxLength int(5) const;
  end-pi;

  dcl-s result char(100);
  dcl-s tempPtr pointer;
  dcl-s tempStr char(500) based(tempPtr); // Puede ajustar tamaño

  result = *blanks;

  tempPtr = yajl_object_find(parentNode: %trim(fieldName));

  if tempPtr <> *null;
     result = %subst(%str(%addr(tempStr)): 1: maxLength);
  endif;

  return result;
end-proc;
