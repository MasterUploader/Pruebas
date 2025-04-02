// ===========================
// Prototipos YAJL Generator
// ===========================
dcl-pr yajl_genOpen pointer extproc('yajl_genOpen');
  escape char(1) value;
end-pr;

dcl-pr yajl_genClose pointer extproc('yajl_genClose');
  gen pointer value;
end-pr;

dcl-pr yajl_beginObj pointer extproc('yajl_beginObj');
  gen pointer value;
end-pr;

dcl-pr yajl_endObj pointer extproc('yajl_endObj');
  gen pointer value;
end-pr;

dcl-pr yajl_addChar pointer extproc('yajl_addChar');
  gen pointer value;
  text pointer value;
end-pr;

dcl-pr yajl_writeBufStr pointer extproc('yajl_writeBufStr');
  gen pointer value;
end-pr;
