using System.Collections.Generic;

namespace antrl4CS.Node
{
    public class FunctionNode : AstNode
    {
        public string Name { get; set; } = string.Empty;
        public bool IsEntry { get; set; }
        public TypeInfo ReturnType { get; set; } = new TypeInfo();
        public List<ParameterNode> Parameters { get; } = [];
        public List<AstNode> Body { get; } = [];
    }
}