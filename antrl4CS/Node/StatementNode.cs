namespace antrl4CS.Node
{
    public class StatementNode : AstNode
    {
        public string Kind { get; set; } = string.Empty;
        public AstNode? Payload { get; set; }
    }
}