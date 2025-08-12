// Helpers OUT
public ProgramCallBuilder OutString(string name, int size, object? initialValue = null)
    => Out(name: name, dbType: DbType.String, size: size, precision: null, scale: null, initialValue: initialValue);

public ProgramCallBuilder OutChar(string name, int size, object? initialValue = null)
    => Out(name: name, dbType: DbType.AnsiStringFixedLength, size: size, precision: null, scale: null, initialValue: initialValue);

public ProgramCallBuilder OutDecimal(string name, byte precision, byte scale, object? initialValue = null)
    => Out(name: name, dbType: DbType.Decimal, size: null, precision: precision, scale: scale, initialValue: initialValue);

public ProgramCallBuilder OutInt32(string name, int? initialValue = null)
    => Out(name: name, dbType: DbType.Int32, size: null, precision: null, scale: null, initialValue: initialValue);

public ProgramCallBuilder OutDateTime(string name, DateTime? initialValue = null)
    => Out(name: name, dbType: DbType.DateTime, size: null, precision: null, scale: null, initialValue: initialValue);

// Helpers IN
public ProgramCallBuilder InString(string? value, int? size = null)
    => In(value, dbType: DbType.String, size: size);

public ProgramCallBuilder InChar(string? value, int size)
    => In(value, dbType: DbType.AnsiStringFixedLength, size: size);

public ProgramCallBuilder InDecimal(decimal? value, byte? precision = null, byte? scale = null)
    => In(value, dbType: DbType.Decimal, precision: precision, scale: scale);

public ProgramCallBuilder InInt32(int? value)
    => In(value, dbType: DbType.Int32);

public ProgramCallBuilder InDateTime(DateTime? value)
    => In(value, dbType: DbType.DateTime);
