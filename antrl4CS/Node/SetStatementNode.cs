namespace antrl4CS.Node
{
    public class SetStatementNode : AstNode
    {
        public AstNode Target { get; set; } = null!;
        public AstNode Expression { get; set; } = null!;
    }
}