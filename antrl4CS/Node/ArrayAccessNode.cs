namespace antrl4CS.Node
{
    public class ArrayAccessNode : AstNode
    {
        public AstNode Target { get; set; } = null!;
        public AstNode Index { get; set; } = null!;
    }
}