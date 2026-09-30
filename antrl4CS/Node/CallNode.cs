using System.Collections.Generic;

namespace antrl4CS.Node
{
    public class CallNode : AstNode
    {
        public string FunctionName { get; set; } = string.Empty; // for built-ins or user-defined
        public List<AstNode> Arguments { get; } = [];
        public bool IsBuiltIn { get; set; }
    }
}