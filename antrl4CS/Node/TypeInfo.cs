namespace antrl4CS.Node
{
    public class TypeInfo
    {
        public string BaseName { get; set; } = string.Empty;
        public bool IsArray { get; set; }
        public bool IsNullable { get; set; }
        public AstNode? ArraySizeExpression { get; set; }
    }
}