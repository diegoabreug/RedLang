using System.Collections.Generic;

namespace antrl4CS.Node
{
    public class RepeatNode : AstNode
    {
        public AstNode Condition { get; set; } = null!;
        public List<AstNode> Body { get; } = [];
    }
}