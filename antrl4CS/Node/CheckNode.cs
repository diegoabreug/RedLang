using System.Collections.Generic;

namespace antrl4CS.Node
{
    public class CheckNode : AstNode
    {
        public AstNode Condition { get; set; } = null!;
        public List<AstNode> ThenBlock { get; } = [];
        public List<AstNode>? ElseBlock { get; set; }
    }
}