using antrl4CS.Node;

namespace antrl4CS.Symbols
{
    public class FunctionSymbol : Symbol
    {
        public List<VariableSymbol> Parameters { get; } = new();

        public TypeInfo ReturnType { get; set; } = null!;

        public bool IsEntry { get; set; }

        // Local scope of this function
        public SymbolTable LocalScope { get; set; } = null!;

        // If the function is inside a class
        public ClassSymbol? ParentClass { get; set; }
    }
}
