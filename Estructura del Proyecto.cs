dcl-proc GetStringFromJson;
  dcl-pi *n char(200);
    parentNode pointer value;
    fieldName varchar(50) const;
    maxLength int(5) const;
  end-pi;

  dcl-s result char(200);
  dcl-s tempPtr pointer;
  dcl-s tempStr char(500) based(tempPtr);
  dcl-s realLength int(10);

  result = *blanks;

  tempPtr = yajl_object_find(parentNode: %trim(fieldName));

  if tempPtr <> *null;
    realLength = %len(%str(%addr(tempStr)));
    result = %subst(%str(%addr(tempStr)): 1: %min(realLength: maxLength));
    if %trim(result) = '';
      result = ' ';
    endif;
  else;
    result = ' ';
  endif;

  return result;
end-proc;
