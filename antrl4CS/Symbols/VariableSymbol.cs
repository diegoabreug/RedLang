using antrl4CS.Node;

namespace antrl4CS.Symbols
{
    public class VariableSymbol : Symbol
    {
        public TypeInfo Type { get; set; } = null!;
        public bool IsField { get; set; }   // útil para saber si pertenece a clase
    }
}
