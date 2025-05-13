dcl-proc GetStringFromJson;
  dcl-pi *n char(200);
    parentNode pointer value;
    fieldName varchar(50) const;
    maxLength int(5) const;
  end-pi;

  dcl-s result char(200);
  dcl-s val pointer;

  result = *blanks;

  val = yajl_object_find(parentNode: %trim(fieldName));

  if val <> *null;
    result = yajl_get_string(val);
    if %trim(result) = '';
      result = ' ';
    endif;
  else;
    result = ' ';
  endif;

  return %subst(result:1:maxLength);
end-proc;
