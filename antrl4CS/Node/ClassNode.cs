using System.Collections.Generic;

namespace antrl4CS.Node
{
    public class ClassNode : AstNode
    {
        public string Name { get; set; } = string.Empty;
        public List<AstNode> Members { get; } = [];
    }
}