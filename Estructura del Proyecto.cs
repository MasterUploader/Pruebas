namespace RestUtilities.Connections.Helpers
{
    public sealed class ProgramCallResult
    {
        public int RowsAffected { get; internal set; }
        public IReadOnlyDictionary<string, object?> OutValues => _outValues;
        private readonly Dictionary<string, object?> _outValues = new(StringComparer.OrdinalIgnoreCase);
        internal void AddOut(string name, object? value) => _outValues[name] = value;
        public bool TryGet<T>(string key, out T? value) { ... }
        public T MapTo<T>(Action<OutputMapBuilder<T>> map) where T : new() { ... }
    }
}
