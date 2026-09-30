namespace antrl4CS.Node
{
    public class UnaryExpressionNode : AstNode
    {
        public string Op { get; set; } = string.Empty;
        public AstNode Operand { get; set; } = null!;
    }
}