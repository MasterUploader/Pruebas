private enum Naming { SqlDot, SystemSlash }
private Naming _naming = Naming.SqlDot;
private bool _wrapWithBraces = true;

public ProgramCallBuilder UseSqlNaming() { _naming = Naming.SqlDot; return this; }
public ProgramCallBuilder UseSystemNaming() { _naming = Naming.SystemSlash; return this; }
public ProgramCallBuilder WrapCallWithBraces(bool enable = true) { _wrapWithBraces = enable; return this; }

private string BuildSql()
{
    int paramCount = _paramFactories.Count + _bulkOuts.Count;
    var placeholders = paramCount == 0 ? "" : string.Join(", ", Enumerable.Repeat("?", paramCount));
    var sep = _naming == Naming.SqlDot ? "." : "/";
    var target = $"{_library}{sep}{_program}".ToUpperInvariant();
    var core = paramCount == 0 ? $"CALL {target}()" : $"CALL {target}({placeholders})";
    return _wrapWithBraces ? "{" + core + "}" : core;
}
