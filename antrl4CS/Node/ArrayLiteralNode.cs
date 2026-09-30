using System.Collections.Generic;

namespace antrl4CS.Node
{
    public class ArrayLiteralNode : AstNode
    {
        public List<AstNode> Elements { get; } = [];
    }
}