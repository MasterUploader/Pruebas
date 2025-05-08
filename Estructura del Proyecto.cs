dcl-proc MostrarJsonNodos;
  dcl-pi *n;
    nodo pointer value;
    nivel int(5) value;
  end-pi;

  dcl-s i int(5);
  dcl-s key varchar(100);
  dcl-s subnodo pointer;
  dcl-s tipo int(5);

  dcl-s totalKeys int(5);
  dcl-s indent varchar(50);

  // Indentación visual según nivel
  indent = %subst('                                                  ': 1: nivel * 2);

  tipo = yajl_get_type(nodo);

  select;
    when tipo = YAJL_TYPE_OBJ;

      totalKeys = yajl_get_keys(nodo);
      for i = 1 to totalKeys;
        key = yajl_get_key(nodo: i);
        subnodo = yajl_object_find(nodo: key);
        dsply indent + 'Obj: ' + key;
        MostrarJsonNodos(subnodo: nivel + 1);
      endfor;

    when tipo = YAJL_TYPE_ARR;

      for i = 1 to yajl_array_size(nodo);
        subnodo = yajl_array_elem(nodo: i - 1);
        dsply indent + 'Array[' + %char(i) + ']';
        MostrarJsonNodos(subnodo: nivel + 1);
      endfor;

    when tipo = YAJL_TYPE_STR;
      dsply indent + 'Valor: ' + %str(yajl_get_string(nodo));

    when tipo = YAJL_TYPE_NUM;
      dsply indent + 'Número: ' + %str(yajl_get_string(nodo));

    when tipo = YAJL_TYPE_BOOL;
      dsply indent + 'Boolean: ' + %str(yajl_get_string(nodo));

    when tipo = YAJL_TYPE_NULL;
      dsply indent + 'Null';

    other;
      dsply indent + 'Tipo desconocido';
  endsl;

end-proc;


dcl-s rootNode pointer;
dcl-s errmsg varchar(500);

rootNode = yajl_stmf_load_tree(%trim(vFullFileR): errmsg);

if rootNode = *null;
   dsply 'Error al parsear JSON: ' + errmsg;
else;
   MostrarJsonNodos(rootNode: 0);
   yajl_tree_free(rootNode);
endif;
