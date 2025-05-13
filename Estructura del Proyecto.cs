dcl-proc GetDecimalFromJson;
  dcl-pi *n packed(15:4);
    parentNode pointer value;
    fieldName varchar(50) const;
  end-pi;

  dcl-s result packed(15:4) inz(0);
  dcl-s val pointer;
  dcl-s strVal varchar(50);

  val = yajl_object_find(parentNode: %trim(fieldName));
  if val <> *null;
    strVal = yajl_get_string(val);
    if %trim(strVal) <> '';
      result = %dec(strVal: 15: 4);
    endif;
  endif;

  return result;
end-proc;

dcl-proc GetDateFromJson;
  dcl-pi *n date;
    parentNode pointer value;
    fieldName varchar(50) const;
  end-pi;

  dcl-s result date inz(*loval);
  dcl-s val pointer;
  dcl-s strVal char(8);

  val = yajl_object_find(parentNode: %trim(fieldName));
  if val <> *null;
    strVal = yajl_get_string(val);
    if %len(%trim(strVal)) = 8;
      result = %date(%subst(strVal:1:4) + '-' +
                     %subst(strVal:5:2) + '-' +
                     %subst(strVal:7:2): *iso);
    endif;
  endif;

  return result;
end-proc;

dcl-proc GetBooleanFromJson;
  dcl-pi *n ind;
    parentNode pointer value;
    fieldName varchar(50) const;
  end-pi;

  dcl-s val pointer;
  val = yajl_object_find(parentNode: %trim(fieldName));
  return (val <> *null and yajl_is_true(val));
end-proc;
