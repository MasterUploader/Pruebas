En esta linea;

   public InsertQueryBuilder IntoColumns(params string[] columns)
   { _columns.Clear(); if (columns is { Length: > 0 }) _columns.AddRange(columns); return this; }

Tengo la advertencias S2681, corrigela
