namespace antrl4CS.Node
{
    public class BinaryExpressionNode : AstNode
    {
        public string Op { get; set; } = string.Empty;
        public AstNode Left { get; set; } = null!;
        public AstNode Right { get; set; } = null!;
    }
}