namespace antrl4CS.Symbols
{
    public class SymbolTable
    {
        public Dictionary<string, Symbol> Symbols { get; } = new();
        public SymbolTable? Parent { get; }

        public SymbolTable(SymbolTable? parent = null)
        {
            Parent = parent;
        }

        public void Add(Symbol symbol)
        {
            if (!Symbols.TryAdd(symbol.Name, symbol))
                throw new Exception($"{symbol.Name} is already defined in this scope");
        }

        public Symbol? Lookup(string name) =>
            Symbols.TryGetValue(name, out var value) ? value : Parent?.Lookup(name);
    }
}
