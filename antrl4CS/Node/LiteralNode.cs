namespace antrl4CS.Node
{
    public class LiteralNode : AstNode
    {
        public string Kind { get; set; } = string.Empty; // "BOOL","FLOAT","INT","STRING","NULL"
        public string ValueText { get; set; } = string.Empty;
    }
}