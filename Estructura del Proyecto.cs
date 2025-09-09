**FREE
ctl-opt dftactgrp(*no);

/**
 * Devuelve la hora actual en formato HHMMSScc (p.ej. 14020201).
 * Usa la hora del job. Las centésimas se obtienen truncando la fracción.
 */
dcl-proc GetHoraHHMMSScc export;
  dcl-pi *n char(8) end-pi;

  dcl-s ts timestamp;
  dcl-s s  char(26);   // 'YYYY-MM-DD-HH.MM.SS.mmmmmm' (ISO)
  dcl-s hh char(2);
  dcl-s mm char(2);
  dcl-s ss char(2);
  dcl-s cc char(2);    // centésimas (2 primeras de 'mmmmmm')

  ts = %timestamp();
  s  = %char(ts : *ISO);      // ISO: YYYY-MM-DD-HH.MM.SS.mmmmmm

  hh = %subst(s : 12 : 2);    // HH
  mm = %subst(s : 15 : 2);    // MM
  ss = %subst(s : 18 : 2);    // SS
  cc = %subst(s : 21 : 2);    // 2 primeras de microsegundos => centésimas

  return hh + mm + ss + cc;   // Ej.: 14020201
end-proc;

/* --- Ejemplo de uso --- */
dcl-s hora8 char(8);
hora8 = GetHoraHHMMSScc();
dsply ('Hora8=' + hora8);   // Muestra algo como 'Hora8=14020201'

*inlr = *on;
return;
