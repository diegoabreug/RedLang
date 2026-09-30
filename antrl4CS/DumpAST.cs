using antrl4CS.Node;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace antrl4CS
{
    public static class AstPrinter
    {
        /// <summary>
        /// </summary>
        public static void DumpAst(AstNode node, string indent = "")
        {
            if (node == null) return;

            string info = node switch
            {
                ProgramNode p => $"ProgramNode (uses={p.UseNodes.Count}, classes={p.ClassNodes.Count})",
                UseNode u => $"UseNode name={u.ClassName}",
                ClassNode c => $"ClassNode name={c.Name}",
                FunctionNode f => $"FunctionNode name={f.Name}, entry={f.IsEntry}, return={f.ReturnType.BaseName}",
                VariableDeclNode v => $"VarDecl name={v.Name}, type={v.Type?.BaseName ?? "?"}",
                BlockNode b => $"BlockNode ({b.Statements.Count} statements)",
                StatementNode s => $"StatementNode kind={s.Kind}",
                ReturnNode => "ReturnNode",
                BinaryExpressionNode b2 => $"BinaryExpr op={b2.Op}",
                UnaryExpressionNode u2 => $"UnaryExpr op={u2.Op}",
                LiteralNode => "LiteralNode",
                IdentifierNode id => $"IdentifierNode name={id.Name}",
                CallNode call => $"CallNode name={call.FunctionName}",
                ParameterNode p3 => $"ParameterNode {p3.Name}:{p3.Type.BaseName}",
                _ => node.GetType().Name
            };

            Console.WriteLine(indent + info);

            switch (node)
            {
                case ProgramNode p:
                    foreach (var u in p.UseNodes)
                        DumpAst(u, indent + "  ");
                    foreach (var c in p.ClassNodes)
                        DumpAst(c, indent + "  ");
                    break;

                case ClassNode c:
                    foreach (var m in c.Members)
                        DumpAst(m, indent + "  ");
                    break;

                case FunctionNode f:
                    foreach (var param in f.Parameters)
                        DumpAst(param, indent + "  ");
                    foreach (var stmt in f.Body)
                        DumpAst(stmt, indent + "  ");
                    break;

                case BlockNode b:
                    foreach (var stmt in b.Statements)
                        DumpAst(stmt, indent + "  ");
                    break;

                case StatementNode s when s.Payload != null:
                    DumpAst(s.Payload, indent + "  ");
                    break;

                case ReturnNode r when r.Expression != null:
                    DumpAst(r.Expression, indent + "  ");
                    break;

                case BinaryExpressionNode b2:
                    DumpAst(b2.Left, indent + "  ");
                    DumpAst(b2.Right, indent + "  ");
                    break;

                case UnaryExpressionNode u2:
                    DumpAst(u2.Operand, indent + "  ");
                    break;

                case CallNode call:
                    foreach (var arg in call.Arguments)
                        DumpAst(arg, indent + "  ");
                    break;

            }
        }
    }

}
