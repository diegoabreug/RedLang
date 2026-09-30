using System.Collections.Generic;

namespace antrl4CS.Node
{
    public class LoopNode : AstNode
    {
        public AstNode Init { get; set; } = null!;
        public AstNode Condition { get; set; } = null!;
        public AstNode Action { get; set; } = null!;
        public List<AstNode> Body { get; } = [];
    }
}