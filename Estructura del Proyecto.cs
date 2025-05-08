dcl-s vTimestamp char(19);
vTimestamp = %char(%date():*ISO) + 'T' + %char(%time());  // Ej: 2025-05-10T15:23:45
