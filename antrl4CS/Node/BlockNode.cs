using System.Collections.Generic;

namespace antrl4CS.Node
{
    public class BlockNode : AstNode
    {
        public List<AstNode> Statements { get; } = [];
    }
}