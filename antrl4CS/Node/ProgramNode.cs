using System.Collections.Generic;

namespace antrl4CS.Node
{
    public class ProgramNode : AstNode
    {
        public List<UseNode> UseNodes { get; } = [];
        public List<ClassNode> ClassNodes { get; } = [];
    }
}