namespace antrl4CS.Symbols
{
    public class ClassSymbol : Symbol
    {
        public SymbolTable ClassScope { get; set; } = null!;

        public Dictionary<string, VariableSymbol> Fields { get; } = new();

        public Dictionary<string, FunctionSymbol> Methods { get; } = new();
    }
}
