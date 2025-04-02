// yajl_get_string: retorna un puntero a char
dcl-pr yajl_get_string pointer extproc('yajl_get_string');
  node pointer value;
end-pr;

// yajl_get_number: retorna un puntero a número (cadena que contiene el número)
dcl-pr yajl_get_number pointer extproc('yajl_get_number');
  node pointer value;
end-pr;

// Devuelve una cadena desde un nodo JSON
// Si el nodo o valor es nulo, retorna *blanks
dcl-proc SafeGetString;
  dcl-pi *n char(100);
    jsonPtr pointer value;
  end-pi;

  dcl-s tempStrPtr pointer;

  if jsonPtr <> *null;
     tempStrPtr = yajl_get_string(jsonPtr);
     if tempStrPtr <> *null;
        return %str(tempStrPtr);
     endif;
  endif;

  return *blanks;
end-proc;



// Devuelve un valor decimal desde un nodo JSON
// Si el nodo o valor es nulo, retorna cero
dcl-proc SafeGetDecimal;
  dcl-pi *n packed(15:4);
    jsonPtr pointer value;
  end-pi;

  dcl-s result packed(15:4);
  dcl-s tempNumPtr pointer;
  dcl-s tempNum char(64) based(tempNumPtr);

  result = 0;

  if jsonPtr <> *null;
     tempNumPtr = yajl_get_number(jsonPtr);
     if tempNumPtr <> *null;
        result = %dec(%str(tempNumPtr): 15: 4);
     endif;
  endif;

  return result;
end-proc;
