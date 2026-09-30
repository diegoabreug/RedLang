namespace antrl4CS.Node
{
    public class ParameterNode : AstNode
    {
        public string Name { get; set; } = string.Empty;
        public TypeInfo Type { get; set; } = new TypeInfo();
    }
}