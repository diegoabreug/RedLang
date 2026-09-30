namespace antrl4CS.Node
{
    public class VariableDeclNode : AstNode
    {
        public string Name { get; set; } = string.Empty;
        public TypeInfo? Type { get; set; }
        public AstNode? Initializer { get; set; }
    }
}