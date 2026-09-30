namespace antrl4CS.Node
{
    public class MemberAccessNode : AstNode
    {
        public AstNode Target { get; set; } = null!;
        public string MemberName { get; set; } = string.Empty;
        public CallNode? MethodCall { get; set; } = null; // Para guardar el CallNode completo cuando es una llamada a método
    }
}