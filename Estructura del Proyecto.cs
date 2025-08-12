Me refiero a que en lugar de ser así
public ProgramCallBuilder InString(string? value, int? size = null) => In(value, DbType.String, size: size);
Sean Así que requieran el nombre
public ProgramCallBuilder InString(string name, string? value, int? size = null) => In(value, DbType.String, size: size);
