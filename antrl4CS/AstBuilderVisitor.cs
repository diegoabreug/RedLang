using Antlr4.Runtime;
using antrl4CS.Node;
using System.Diagnostics.CodeAnalysis;

namespace antrl4CS
{
    public class AstBuilderVisitor : construccion_semana2ParserBaseVisitor<AstNode>
    {
        public override AstNode VisitProgram([NotNull] construccion_semana2Parser.ProgramContext context)
        {
            var prog = new ProgramNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0
            };

            // Use explicit sequences for clarity
            foreach (var use in context.otraclase())
            {
                if (Visit(use) is UseNode u) prog.UseNodes.Add(u);
            }

            foreach (var cls in context.clase_decl())
            {
                if (Visit(cls) is ClassNode c) prog.ClassNodes.Add(c);
            }

            return prog;
        }

        public override AstNode VisitOtraclase([NotNull] construccion_semana2Parser.OtraclaseContext context)
        {
            var idText = context.name()?.ID()?.GetText() ?? string.Empty;

            return new UseNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                ClassName = idText
            };
        }

        public override AstNode VisitClase_decl([NotNull] construccion_semana2Parser.Clase_declContext context)
        {
            var idText = context.name()?.ID()?.GetText() ?? string.Empty;

            var classNode = new ClassNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Name = idText
            };

            var body = context.classBody();
            if (body != null)
            {
                foreach (var member in body.classMember())
                {
                    var memberNode = Visit(member);
                    if (memberNode != null)
                    {
                        classNode.Members.Add(memberNode);
                    }
                }
            }

            return classNode;
        }

        public override AstNode VisitClassMember([NotNull] construccion_semana2Parser.ClassMemberContext context)
        {
            if (context.declare_stmt() != null)
            {
                return Visit(context.declare_stmt());
            }
            if (context.func_decl() != null)
            {
                return Visit(context.func_decl());
            }
            if (context.entry_func_decl() != null)
            {
                return Visit(context.entry_func_decl());
            }

            return new StatementNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Kind = "unknown-member"
            };
        }

        public override AstNode VisitDeclare_stmt([NotNull] construccion_semana2Parser.Declare_stmtContext context)
        {
            // DECLARE name COLON (data_type | name?) (EQUAL inicializador)? SEMI_COLON
            var varName = context.name(0)?.ID()?.GetText() ?? string.Empty;

            var decl = new VariableDeclNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Name = varName
            };

            if (context.data_type() != null)
            {
                decl.Type = ExtractDataType(context.data_type());
            }
            else
            {
                // Optional user-defined type via second 'name'
                var userTypeName = context.name(1)?.ID()?.GetText();
                if (!string.IsNullOrEmpty(userTypeName))
                {
                    decl.Type = new TypeInfo
                    {
                        BaseName = userTypeName!,
                        IsArray = false,
                        IsNullable = false,
                        ArraySizeExpression = null
                    };
                }
            }

            if (context.inicializador() != null)
            {
                decl.Initializer = Visit(context.inicializador());
            }

            return decl;
        }

        public override AstNode VisitFunc_decl([NotNull] construccion_semana2Parser.Func_declContext context)
        {
            var func = new FunctionNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Name = context.name()?.ID()?.GetText() ?? string.Empty,
                ReturnType = ExtractDataType(context.data_type())
            };

            // Parameters
            if (context.param_list() != null)
            {
                foreach (var p in context.param_list().param())
                {
                    var pName = p.name()?.ID()?.GetText() ?? string.Empty;
                    var pType = ExtractDataType(p.data_type());
                    func.Parameters.Add(new ParameterNode
                    {
                        Line = p.Start?.Line ?? 0,
                        Column = p.Start?.Column ?? 0,
                        Name = pName,
                        Type = pType
                    });
                }
            }

            // Body
            if (context.block() != null)
            {
                foreach (var stmt in context.block().sentencia())
                {
                    var stmtNode = Visit(stmt);
                    if (stmtNode != null)
                        func.Body.Add(stmtNode);
                }
            }

            return func;
        }

        public override AstNode VisitEntry_func_decl([NotNull] construccion_semana2Parser.Entry_func_declContext context)
        {
            var funcNode = (FunctionNode)Visit(context.func_decl());
            funcNode.IsEntry = true;
            return funcNode;
        }

        public override AstNode VisitSentencia([NotNull] construccion_semana2Parser.SentenciaContext context)
        {
            if (context.declare_stmt() != null)
                return Visit(context.declare_stmt());

            if (context.set_stmt() != null)
                return Visit(context.set_stmt());

            if (context.return_stmt() != null)
                return Visit(context.return_stmt());

            if (context.stmt_control() != null)
                return Visit(context.stmt_control());

            if (context.llamadaFuncion() != null)
            {
                var call = (CallNode)Visit(context.llamadaFuncion());
                return new StatementNode
                {
                    Line = context.Start?.Line ?? 0,
                    Column = context.Start?.Column ?? 0,
                    Kind = "call",
                    Payload = call
                };
            }

            if (context.accesoMiembro() != null)
            {
                var acc = Visit(context.accesoMiembro());
                return new StatementNode
                {
                    Line = context.Start?.Line ?? 0,
                    Column = context.Start?.Column ?? 0,
                    Kind = "member",
                    Payload = acc
                };
            }

            return new StatementNode { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0, Kind = "unknown" };
        }

        public override AstNode VisitSet_stmt([NotNull] construccion_semana2Parser.Set_stmtContext context)
        {
            var target = Visit(context.setObjetivo());
            var expr = Visit(context.expression());
            return new SetStatementNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Target = target,
                Expression = expr
            };
        }

        public override AstNode VisitReturn_stmt([NotNull] construccion_semana2Parser.Return_stmtContext context)
        {
            var ret = new ReturnNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0
            };
            if (context.expression() != null)
                ret.Expression = Visit(context.expression());
            return ret;
        }

        public override AstNode VisitStmt_control([NotNull] construccion_semana2Parser.Stmt_controlContext context)
        {
            if (context.check_stmt() != null) return Visit(context.check_stmt());
            if (context.loop_stmt() != null) return Visit(context.loop_stmt());
            if (context.repeat_stmt() != null) return Visit(context.repeat_stmt());
            return new StatementNode { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0, Kind = "unknown-control" };
        }

        public override AstNode VisitCheck_stmt([NotNull] construccion_semana2Parser.Check_stmtContext context)
        {
            var node = new CheckNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Condition = Visit(context.expression())
            };

            foreach (var s in context.block().sentencia())
            {
                var stmtNode = Visit(s);
                if (stmtNode != null) node.ThenBlock.Add(stmtNode);
            }

            if (context.otherwiseOpcional() != null)
            {
                node.ElseBlock = [];
                foreach (var s in context.otherwiseOpcional().block().sentencia())
                {
                    var stmtNode = Visit(s);
                    if (stmtNode != null) node.ElseBlock.Add(stmtNode);
                }
            }

            return node;
        }

        public override AstNode VisitLoop_stmt([NotNull] construccion_semana2Parser.Loop_stmtContext context)
        {
            var node = new LoopNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Init = Visit(context.loopInit()),
                Condition = Visit(context.expression()),
                Action = Visit(context.accionLoop())
            };

            foreach (var s in context.block().sentencia())
            {
                var stmtNode = Visit(s);
                if (stmtNode != null) node.Body.Add(stmtNode);
            }

            return node;
        }

        public override AstNode VisitLoopInit([NotNull] construccion_semana2Parser.LoopInitContext context)
        {
            if (context.decl_head() != null)
            {
                // DECLARE name COLON (data_type | name?) (EQUAL expression)?
                var varName = context.decl_head().name(0)?.ID()?.GetText() ?? string.Empty;
                TypeInfo? typeInfo = null;

                if (context.decl_head().data_type() != null)
                {
                    typeInfo = ExtractDataType(context.decl_head().data_type());
                }
                else
                {
                    // Optional user-defined type via second 'name'
                    var userTypeName = context.decl_head().name(1)?.ID()?.GetText();
                    if (!string.IsNullOrEmpty(userTypeName))
                    {
                        typeInfo = new TypeInfo
                        {
                            BaseName = userTypeName!,
                            IsArray = false,
                            IsNullable = false
                        };
                    }
                }

                var decl = new VariableDeclNode
                {
                    Line = context.Start?.Line ?? 0,
                    Column = context.Start?.Column ?? 0,
                    Name = varName,
                    Type = typeInfo
                };

                if (context.expression() != null)
                {
                    decl.Initializer = Visit(context.expression());
                }

                return decl;
            }

            if (context.set_stmt_no_sc() != null)
            {
                // SET setObjetivo EQUAL expression
                var target = Visit(context.set_stmt_no_sc().setObjetivo());
                var expr = Visit(context.set_stmt_no_sc().expression());
                return new SetStatementNode
                {
                    Line = context.Start?.Line ?? 0,
                    Column = context.Start?.Column ?? 0,
                    Target = target,
                    Expression = expr
                };
            }

            return new StatementNode { Line = context.Start?.Line ?? 0, Column = context.Start?.Column ?? 0, Kind = "loop-init-unknown" };
        }

        public override AstNode VisitAccionLoop([NotNull] construccion_semana2Parser.AccionLoopContext context)
        {
            var target = Visit(context.set_stmt_no_sc().setObjetivo());
            var expr = Visit(context.set_stmt_no_sc().expression());
            return new SetStatementNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Target = target,
                Expression = expr
            };
        }

        public override AstNode VisitRepeat_stmt([NotNull] construccion_semana2Parser.Repeat_stmtContext context)
        {
            var node = new RepeatNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Condition = Visit(context.expression())
            };

            foreach (var s in context.block().sentencia())
            {
                var stmtNode = Visit(s);
                if (stmtNode != null) node.Body.Add(stmtNode);
            }

            return node;
        }

        public override AstNode VisitAccesoArreglo([NotNull] construccion_semana2Parser.AccesoArregloContext context)
        {
            var target = Visit(context.name());
            var index = Visit(context.expression());
            return new ArrayAccessNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Target = target,
                Index = index
            };
        }

        public override AstNode VisitAccesoMiembro([NotNull] construccion_semana2Parser.AccesoMiembroContext context)
        {
            var target = Visit(context.name(0));
            // Two names: name DOT name
            if (context.name().Length == 2)
            {
                var memberName = context.name(1).ID().GetText();
                return new MemberAccessNode
                {
                    Line = context.Start?.Line ?? 0,
                    Column = context.Start?.Column ?? 0,
                    Target = target,
                    MemberName = memberName
                };
            }
            else
            {
                // name DOT llamadaFuncion
                var call = (CallNode)Visit(context.llamadaFuncion());
                return new MemberAccessNode
                {
                    Line = context.Start?.Line ?? 0,
                    Column = context.Start?.Column ?? 0,
                    Target = target,
                    MemberName = call.FunctionName,
                    MethodCall = call // Guardar el CallNode completo con los argumentos
                };
            }
        }

        public override AstNode VisitLlamadaFuncion([NotNull] construccion_semana2Parser.LlamadaFuncionContext context)
        {
            var call = new CallNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0
            };

            if (context.name() != null)
            {
                call.FunctionName = context.name().ID().GetText();
                call.IsBuiltIn = false;
            }
            else
            {
                // built-ins: (ASK | SHOW | LEN | FILE_OP | CONVERT_OP)
                var token = context.ASK() ?? context.SHOW() ?? context.LEN() ?? context.FILE_OP() ?? context.CONVERT_OP();
                call.FunctionName = token.GetText();
                call.IsBuiltIn = true;
            }

            if (context.listaArgumentos() != null)
            {
                foreach (var argExpr in context.listaArgumentos().expression())
                {
                    call.Arguments.Add(Visit(argExpr));
                }
            }

            return call;
        }

        public override AstNode VisitName([NotNull] construccion_semana2Parser.NameContext context)
        {
            return new IdentifierNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0,
                Name = context.ID().GetText()
            };
        }

        public override AstNode VisitArray_literal([NotNull] construccion_semana2Parser.Array_literalContext context)
        {
            var node = new ArrayLiteralNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0
            };
            if (context.listaArgumentos() != null)
            {
                foreach (var expr in context.listaArgumentos().expression())
                {
                    node.Elements.Add(Visit(expr));
                }
            }
            return node;
        }

        public override AstNode VisitLiteral([NotNull] construccion_semana2Parser.LiteralContext context)
        {
            var lit = new LiteralNode
            {
                Line = context.Start?.Line ?? 0,
                Column = context.Start?.Column ?? 0
            };

            if (context.BOOL() != null)
            {
                lit.Kind = "BOOL";
                lit.ValueText = context.BOOL().GetText();
            }
            else if (context.FLOAT() != null)
            {
                lit.Kind = "FLOAT";
                lit.ValueText = context.FLOAT().GetText();
            }
            else if (context.INT() != null)
            {
                lit.Kind = "INT";
                lit.ValueText = context.INT().GetText();
            }
            else if (context.STRING() != null)
            {
                lit.Kind = "STRING";
                lit.ValueText = context.STRING().GetText();
            }
            else if (context.NULL() != null)
            {
                lit.Kind = "NULL";
                lit.ValueText = "null";
            }
            else if (context.array_literal() != null)
            {
                return Visit(context.array_literal());
            }

            return lit;
        }

        // ----- Expression labeled alternatives -----

        public override AstNode VisitLogicalOr([NotNull] construccion_semana2Parser.LogicalOrContext context)
            => MakeBinary(context, "or", context.expression(0), context.expression(1));

        public override AstNode VisitLogicalAnd([NotNull] construccion_semana2Parser.LogicalAndContext context)
            => MakeBinary(context, "and", context.expression(0), context.expression(1));

        public override AstNode VisitLogicalNot([NotNull] construccion_semana2Parser.LogicalNotContext context)
            => MakeUnary(context, "not", context.expression());

        public override AstNode VisitRelational([NotNull] construccion_semana2Parser.RelationalContext context)
            => MakeBinary(context, context.comparador().GetText(), context.expression(0), context.expression(1));

        public override AstNode VisitAddSub([NotNull] construccion_semana2Parser.AddSubContext context)
        {
            // expression (PLUS | MINUS) expression
            var op = context.GetChild(1).GetText();
            return MakeBinary(context, op, context.expression(0), context.expression(1));
        }

        public override AstNode VisitMulDiv([NotNull] construccion_semana2Parser.MulDivContext context)
        {
            // expression (MULTIPLY | DIVIDE | MODULO) expression
            var op = context.GetChild(1).GetText();
            return MakeBinary(context, op, context.expression(0), context.expression(1));
        }

        public override AstNode VisitUnaryMinus([NotNull] construccion_semana2Parser.UnaryMinusContext context)
            => MakeUnary(context, "-", context.expression());

        public override AstNode VisitAtom([NotNull] construccion_semana2Parser.AtomContext context)
            => Visit(context.factor());

        // ----- Helpers -----

        private static BinaryExpressionNode MakeBinary(ParserRuleContext ctx, string op, construccion_semana2Parser.ExpressionContext leftCtx, construccion_semana2Parser.ExpressionContext rightCtx)
        {
            return new BinaryExpressionNode
            {
                Line = ctx.Start?.Line ?? 0,
                Column = ctx.Start?.Column ?? 0,
                Op = op,
                Left = VisitStatic(leftCtx),
                Right = VisitStatic(rightCtx)
            };
        }

        private static UnaryExpressionNode MakeUnary(ParserRuleContext ctx, string op, construccion_semana2Parser.ExpressionContext exprCtx)
        {
            return new UnaryExpressionNode
            {
                Line = ctx.Start?.Line ?? 0,
                Column = ctx.Start?.Column ?? 0,
                Op = op,
                Operand = VisitStatic(exprCtx)
            };
        }

        private static TypeInfo ExtractDataType(construccion_semana2Parser.Data_typeContext ctx)
        {
            var info = new TypeInfo();

            var baseCtx = ctx.type_base();
            if (baseCtx.TYPE_I() != null) info.BaseName = "i";
            else if (baseCtx.TYPE_F() != null) info.BaseName = "f";
            else if (baseCtx.TYPE_B() != null) info.BaseName = "b";
            else if (baseCtx.TYPE_S() != null) info.BaseName = "s";
            else if (baseCtx.name() != null) info.BaseName = baseCtx.name().ID().GetText();

            if (ctx.array_specifier() != null)
            {
                info.IsArray = true;
                info.ArraySizeExpression = VisitStatic(ctx.array_specifier().expression());
            }

            info.IsNullable = ctx.QUESTION() != null;

            return info;
        }

        private static AstNode VisitStatic(ParserRuleContext ctx)
        {
            var v = new AstBuilderVisitor();
            return v.Visit(ctx);
        }
    }
}