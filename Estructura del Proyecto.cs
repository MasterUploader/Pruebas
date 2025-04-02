dcl-proc SafeGetDecimal;
  dcl-pi *n packed(15:4);
    jsonPtr pointer value;
  end-pi;

  dcl-s result packed(15:4);
  dcl-s tempNumPtr pointer;
  dcl-s tempNum char(64) based(tempNumPtr); // o tamaño que consideres seguro

  result = 0;

  if jsonPtr <> *null;
    tempNumPtr = yajl_get_number(jsonPtr);
    if tempNumPtr <> *null;
      result = %dec(%str(tempNumPtr): 15: 4);
    endif;
  endif;

  return result;
end-proc;


dcl-proc SafeGetString;
  dcl-pi *n char(100);
    jsonPtr pointer value;
  end-pi;

  dcl-s tempStrPtr pointer;
  dcl-s tempStr char(100) based(tempStrPtr);

  if jsonPtr <> *null;
    tempStrPtr = yajl_get_string(jsonPtr);
    if tempStrPtr <> *null;
      return %str(tempStrPtr);
    endif;
  endif;

  return *blanks;
end-proc;
